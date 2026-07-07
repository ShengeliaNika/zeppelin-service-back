using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeppelin.Api.Dtos.Admin;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AlertsController(InventoryService inventoryService, RecallReminderService recallReminderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CombinedAlertsDto>> Get([FromQuery] int expiringWithinDays = 30)
    {
        var lowStock = (await inventoryService.GetLowStockItemsAsync())
            .Select(i => new InventoryItemDto(
                i.Id, i.Name, i.Category, i.Unit, i.SupplierName, i.SupplierContact, i.CurrentStock, i.ParLevel, i.IsActive,
                i.Batches.Where(b => b.QuantityRemaining > 0)
                    .Select(b => new InventoryBatchDto(b.Id, b.LotNumber, b.ExpiryDate, b.QuantityRemaining))
                    .ToList()))
            .ToList();

        var expiringSoon = (await inventoryService.GetExpiringSoonBatchesAsync(expiringWithinDays))
            .Select(b => new ExpiringBatchDto(b.InventoryItemId, b.InventoryItem!.Name, b.Id, b.LotNumber, b.ExpiryDate!.Value, b.QuantityRemaining))
            .ToList();

        var recallDue = (await recallReminderService.GetDueRemindersAsync())
            .Select(r => new RecallReminderDto(r.Id, r.PatientId, $"{r.Patient!.FirstName} {r.Patient.LastName}", r.AppointmentType!.Name, r.DueDate))
            .ToList();

        return Ok(new CombinedAlertsDto(lowStock, expiringSoon, recallDue));
    }

    [HttpPut("recall/{id:guid}/dismiss")]
    public async Task<IActionResult> DismissRecall(Guid id)
    {
        await recallReminderService.DismissAsync(id);
        return NoContent();
    }
}
