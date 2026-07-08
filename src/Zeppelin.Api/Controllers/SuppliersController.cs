using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class SuppliersController(ZeppelinDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierDto>>> GetAll([FromQuery] string? search = null)
    {
        var query = db.Suppliers.Include(s => s.ItemLinks).ThenInclude(l => l.InventoryItem).Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var suppliers = await query.OrderBy(s => s.Name).ToListAsync();
        return Ok(suppliers.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id)
    {
        var supplier = await db.Suppliers.Include(s => s.ItemLinks).ThenInclude(l => l.InventoryItem).FirstOrDefaultAsync(s => s.Id == id);
        return supplier is null ? NotFound() : Ok(ToDto(supplier));
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<SupplierDto>> Create(CreateSupplierRequest request)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactName = request.ContactName,
            Phone = request.Phone,
            Email = request.Email,
            Notes = request.Notes,
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, ToDto(supplier));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, UpdateSupplierRequest request)
    {
        var supplier = await db.Suppliers.Include(s => s.ItemLinks).ThenInclude(l => l.InventoryItem).FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.Name = request.Name;
        supplier.ContactName = request.ContactName;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Notes = request.Notes;
        supplier.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return Ok(ToDto(supplier));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null)
        {
            return NotFound();
        }

        supplier.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static SupplierDto ToDto(Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.ContactName,
        supplier.Phone,
        supplier.Email,
        supplier.Notes,
        supplier.IsActive,
        supplier.ItemLinks
            .Select(l => new SupplierItemLinkDto(l.Id, l.InventoryItemId, l.InventoryItem!.Name, l.LastUnitCost, l.SupplierSku, l.IsPreferred))
            .ToList());
}
