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
        var lowStockItems = await inventoryService.GetLowStockItemsAsync();
        var avgCosts = await inventoryService.GetAverageCostsAsync(lowStockItems.Select(i => i.Id));
        var lowStock = lowStockItems.Select(i => InventoryItemsController.ToDto(i, avgCosts)).ToList();

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
