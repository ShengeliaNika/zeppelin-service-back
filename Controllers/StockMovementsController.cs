using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Dtos.Inventory;
using Zeppelin.Common;
using Zeppelin;
using Zeppelin.Auditing;
using Zeppelin.Services;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/inventory-items/{itemId:guid}/stock-movements")]
[Authorize(Policy = Policies.ClinicalStaff)]
public class StockMovementsController(ZeppelinDbContext db, InventoryService inventoryService, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> GetAll(Guid itemId)
    {
        var movements = await db.StockMovements
            .Include(m => m.RecordedByUser)
            .Include(m => m.Supplier)
            .Where(m => m.InventoryItemId == itemId)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();

        return Ok(movements.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<StockMovementDto>> Create(Guid itemId, CreateStockMovementRequest request)
    {
        try
        {
            var movement = await inventoryService.RecordMovementAsync(
                itemId,
                request.Type,
                request.Quantity,
                request.LotNumber,
                request.ExpiryDate,
                request.AppointmentTypeId,
                request.AppointmentId,
                request.Notes,
                currentUser.UserId!.Value,
                request.SupplierId,
                request.UnitCost);

            await db.Entry(movement).Reference(m => m.RecordedByUser).LoadAsync();
            if (movement.SupplierId is not null)
            {
                await db.Entry(movement).Reference(m => m.Supplier).LoadAsync();
            }
            return Ok(ToDto(movement));
        }
        catch (InventoryItemNotFoundException)
        {
            return NotFound();
        }
    }

    // Undoes a mistaken "supplies used" entry - restores the stock it deducted
    // rather than just deleting the row. Scoped to UsageDeduction/Waste only
    // (see InventoryService.ReverseUsageMovementAsync); Restock/Adjustment
    // movements return 409, they're not part of this doctor-facing flow.
    [HttpDelete("{movementId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId, Guid movementId)
    {
        try
        {
            await inventoryService.ReverseUsageMovementAsync(itemId, movementId);
            return NoContent();
        }
        catch (StockMovementNotFoundException)
        {
            return NotFound();
        }
        catch (StockMovementNotReversibleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static StockMovementDto ToDto(Entities.Inventory.StockMovement m) => new(
        m.Id,
        m.Type,
        m.Quantity,
        m.InventoryBatchId,
        m.AppointmentTypeId,
        m.SupplierId,
        m.Supplier?.Name,
        m.UnitCost,
        m.Notes,
        m.RecordedByUser is null ? string.Empty : $"{m.RecordedByUser.FirstName} {m.RecordedByUser.LastName}",
        m.RecordedAtUtc);
}
