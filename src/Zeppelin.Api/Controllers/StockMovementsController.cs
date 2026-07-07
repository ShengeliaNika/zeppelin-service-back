using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

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
                currentUser.UserId!.Value);

            await db.Entry(movement).Reference(m => m.RecordedByUser).LoadAsync();
            return Ok(ToDto(movement));
        }
        catch (InventoryItemNotFoundException)
        {
            return NotFound();
        }
    }

    private static StockMovementDto ToDto(Domain.Entities.Inventory.StockMovement m) => new(
        m.Id,
        m.Type,
        m.Quantity,
        m.InventoryBatchId,
        m.AppointmentTypeId,
        m.Notes,
        m.RecordedByUser is null ? string.Empty : $"{m.RecordedByUser.FirstName} {m.RecordedByUser.LastName}",
        m.RecordedAtUtc);
}
