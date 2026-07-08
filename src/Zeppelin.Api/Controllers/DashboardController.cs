using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Admin;
using Zeppelin.Api.Dtos.Scheduling;
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
        var sevenDaysAgoUtc = todayUtc.AddDays(-7);
        var monthStartUtc = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var todaysAppointments = await db.Appointments.CountAsync(a => a.StartAtUtc >= todayUtc && a.StartAtUtc < tomorrowUtc);
        var weekAppointments = await db.Appointments.CountAsync(a => a.StartAtUtc >= todayUtc && a.StartAtUtc < weekEndUtc);
        var activePatients = await db.Patients.CountAsync(p => p.IsActive);
        var recallDueCount = (await recallReminderService.GetDueRemindersAsync()).Count;
        var inventorySummary = await inventoryService.GetSummaryAsync();

        var estimatedRevenueThisMonth = await db.TreatmentPlanItems
            .Where(i => i.CompletedAtUtc != null && i.CompletedAtUtc >= monthStartUtc && i.EstimatedCost != null)
            .SumAsync(i => i.EstimatedCost!.Value);

        var completedLast7Days = await db.Appointments.CountAsync(
            a => a.StartAtUtc >= sevenDaysAgoUtc && a.StartAtUtc < todayUtc && a.Status == AppointmentStatus.Completed);
        var noShowLast7Days = await db.Appointments.CountAsync(
            a => a.StartAtUtc >= sevenDaysAgoUtc && a.StartAtUtc < todayUtc && a.Status == AppointmentStatus.NoShow);
        var cancelledLast7Days = await db.Appointments.CountAsync(
            a => a.StartAtUtc >= sevenDaysAgoUtc && a.StartAtUtc < todayUtc && a.Status == AppointmentStatus.Cancelled);

        return Ok(new DashboardSummaryDto(
            todaysAppointments,
            weekAppointments,
            activePatients,
            inventorySummary.LowStockCount,
            inventorySummary.ExpiringSoonCount,
            recallDueCount,
            estimatedRevenueThisMonth,
            inventorySummary.TotalValuation,
            completedLast7Days,
            noShowLast7Days,
            cancelledLast7Days));
    }

    [HttpGet("todays-schedule")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetTodaysSchedule()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var appointments = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DentistUser)
            .Include(a => a.Chair)
            .Include(a => a.AppointmentType)
            .Where(a => a.StartAtUtc >= todayUtc && a.StartAtUtc < tomorrowUtc)
            .OrderBy(a => a.StartAtUtc)
            .ToListAsync();

        return Ok(appointments.Select(AppointmentsController.ToDto).ToList());
    }
}
