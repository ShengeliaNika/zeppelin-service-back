using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/inventory-items/{itemId:guid}/suppliers")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class ItemSuppliersController(ZeppelinDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ItemSupplierLinkDto>>> GetAll(Guid itemId)
    {
        var links = await db.ItemSuppliers
            .Include(l => l.Supplier)
            .Where(l => l.InventoryItemId == itemId)
            .ToListAsync();

        return Ok(links.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ItemSupplierLinkDto>> Link(Guid itemId, LinkItemSupplierRequest request)
    {
        var itemExists = await db.InventoryItems.AnyAsync(i => i.Id == itemId);
        if (!itemExists)
        {
            return NotFound();
        }

        if (await db.ItemSuppliers.AnyAsync(l => l.InventoryItemId == itemId && l.SupplierId == request.SupplierId))
        {
            return Conflict("This item is already linked to that supplier.");
        }

        if (request.IsPreferred)
        {
            await ClearPreferredAsync(itemId);
        }

        var link = new ItemSupplier
        {
            Id = Guid.NewGuid(),
            InventoryItemId = itemId,
            SupplierId = request.SupplierId,
            LastUnitCost = request.LastUnitCost,
            SupplierSku = request.SupplierSku,
            IsPreferred = request.IsPreferred,
        };

        db.ItemSuppliers.Add(link);
        await db.SaveChangesAsync();
        await db.Entry(link).Reference(l => l.Supplier).LoadAsync();

        return Ok(ToDto(link));
    }

    [HttpPut("{linkId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ItemSupplierLinkDto>> Update(Guid itemId, Guid linkId, UpdateItemSupplierLinkRequest request)
    {
        var link = await db.ItemSuppliers.Include(l => l.Supplier).FirstOrDefaultAsync(l => l.Id == linkId && l.InventoryItemId == itemId);
        if (link is null)
        {
            return NotFound();
        }

        if (request.IsPreferred && !link.IsPreferred)
        {
            await ClearPreferredAsync(itemId);
        }

        link.LastUnitCost = request.LastUnitCost;
        link.SupplierSku = request.SupplierSku;
        link.IsPreferred = request.IsPreferred;

        await db.SaveChangesAsync();
        return Ok(ToDto(link));
    }

    [HttpDelete("{linkId:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Unlink(Guid itemId, Guid linkId)
    {
        var link = await db.ItemSuppliers.FirstOrDefaultAsync(l => l.Id == linkId && l.InventoryItemId == itemId);
        if (link is null)
        {
            return NotFound();
        }

        db.ItemSuppliers.Remove(link);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ClearPreferredAsync(Guid itemId)
    {
        var existing = await db.ItemSuppliers.Where(l => l.InventoryItemId == itemId && l.IsPreferred).ToListAsync();
        foreach (var l in existing)
        {
            l.IsPreferred = false;
        }
    }

    private static ItemSupplierLinkDto ToDto(ItemSupplier link) => new(
        link.Id, link.SupplierId, link.Supplier!.Name, link.LastUnitCost, link.SupplierSku, link.IsPreferred);
}
