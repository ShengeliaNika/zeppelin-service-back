using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Common;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/inventory-items")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class InventoryItemsController(ZeppelinDbContext db, InventoryService inventoryService) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<InventoryItemDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 25)
    {
        take = Math.Clamp(take, 1, MaxPageSize);
        skip = Math.Max(0, skip);

        var query = db.InventoryItems.Include(i => i.Batches).Where(i => i.IsActive).OrderBy(i => i.Name);

        var totalCount = await query.CountAsync();
        var items = await query.Skip(skip).Take(take).ToListAsync();

        return Ok(new PagedResultDto<InventoryItemDto>(items.Select(ToDto).ToList(), totalCount, skip, take));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id)
    {
        var item = await db.InventoryItems.Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == id);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<InventoryAlertsDto>> GetAlerts([FromQuery] int expiringWithinDays = 30)
    {
        var lowStock = (await inventoryService.GetLowStockItemsAsync()).Select(ToDto).ToList();
        var expiringSoon = (await inventoryService.GetExpiringSoonBatchesAsync(expiringWithinDays))
            .Select(b => new ExpiringBatchDto(b.InventoryItemId, b.InventoryItem!.Name, b.Id, b.LotNumber, b.ExpiryDate!.Value, b.QuantityRemaining))
            .ToList();

        return Ok(new InventoryAlertsDto(lowStock, expiringSoon));
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<InventoryItemDto>> Create(CreateInventoryItemRequest request)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Category = request.Category,
            Unit = request.Unit,
            SupplierName = request.SupplierName,
            SupplierContact = request.SupplierContact,
            ParLevel = request.ParLevel,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToDto(item));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, UpdateInventoryItemRequest request)
    {
        var item = await db.InventoryItems.Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Name = request.Name;
        item.Category = request.Category;
        item.Unit = request.Unit;
        item.SupplierName = request.SupplierName;
        item.SupplierContact = request.SupplierContact;
        item.ParLevel = request.ParLevel;
        item.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    private static InventoryItemDto ToDto(InventoryItem item) => new(
        item.Id,
        item.Name,
        item.Category,
        item.Unit,
        item.SupplierName,
        item.SupplierContact,
        item.CurrentStock,
        item.ParLevel,
        item.IsActive,
        item.Batches
            .Where(b => b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
            .Select(b => new InventoryBatchDto(b.Id, b.LotNumber, b.ExpiryDate, b.QuantityRemaining))
            .ToList());
}
