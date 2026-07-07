using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Admin;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class DashboardController(ZeppelinDbContext db, InventoryService inventoryService, RecallReminderService recallReminderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);
        var weekEndUtc = todayUtc.AddDays(7);

        var todaysAppointments = await db.Appointments.CountAsync(a => a.StartAtUtc >= todayUtc && a.StartAtUtc < tomorrowUtc);
        var weekAppointments = await db.Appointments.CountAsync(a => a.StartAtUtc >= todayUtc && a.StartAtUtc < weekEndUtc);
        var activePatients = await db.Patients.CountAsync(p => p.IsActive);
        var lowStockCount = (await inventoryService.GetLowStockItemsAsync()).Count;
        var expiringSoonCount = (await inventoryService.GetExpiringSoonBatchesAsync(30)).Count;
        var recallDueCount = (await recallReminderService.GetDueRemindersAsync()).Count;

        return Ok(new DashboardSummaryDto(
            todaysAppointments, weekAppointments, activePatients, lowStockCount, expiringSoonCount, recallDueCount));
    }
}
