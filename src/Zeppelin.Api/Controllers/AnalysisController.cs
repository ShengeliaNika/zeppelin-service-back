using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Analysis;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

// Trend/insight views layered on existing data - no new data collection,
// same "estimate, not real billing" boundary as the Dashboard's revenue tile.
[ApiController]
[Route("api/analysis")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AnalysisController(ZeppelinDbContext db) : ControllerBase
{
    [HttpGet("appointments-trend")]
    public async Task<ActionResult<AppointmentsTrendDto>> GetAppointmentsTrend([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var appointments = await db.Appointments
            .Where(a => a.StartAtUtc >= fromUtc && a.StartAtUtc <= toUtc)
            .Select(a => new { a.StartAtUtc, a.Status })
            .ToListAsync();

        var daily = appointments
            .GroupBy(a => DateOnly.FromDateTime(a.StartAtUtc))
            .Select(g => new DailyAppointmentStatsDto(
                g.Key,
                g.Count(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.CheckedIn),
                g.Count(a => a.Status == AppointmentStatus.Completed),
                g.Count(a => a.Status == AppointmentStatus.NoShow),
                g.Count(a => a.Status == AppointmentStatus.Cancelled)))
            .OrderBy(d => d.Date)
            .ToList();

        var completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow);
        var cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled);
        var resolved = completed + noShow + cancelled;

        var completionRate = resolved == 0 ? 0 : Math.Round((decimal)completed / resolved * 100, 1);
        var noShowRate = resolved == 0 ? 0 : Math.Round((decimal)noShow / resolved * 100, 1);

        return Ok(new AppointmentsTrendDto(from, to, daily, completionRate, noShowRate));
    }

    [HttpGet("patient-growth")]
    public async Task<ActionResult<PatientGrowthDto>> GetPatientGrowth([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var createdDates = await db.Patients
            .Where(p => p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc <= toUtc)
            .Select(p => p.CreatedAtUtc)
            .ToListAsync();

        var weekly = createdDates
            .GroupBy(WeekStart)
            .Select(g => new WeeklyPatientGrowthDto(g.Key, g.Count()))
            .OrderBy(w => w.WeekStart)
            .ToList();

        return Ok(new PatientGrowthDto(from, to, createdDates.Count, weekly));
    }

    [HttpGet("revenue-trend")]
    public async Task<ActionResult<RevenueTrendDto>> GetRevenueTrend([FromQuery] int months = 6)
    {
        months = Math.Clamp(months, 1, 24);
        var todayUtc = DateTime.UtcNow.Date;
        var rangeStartUtc = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

        var completedItems = await db.TreatmentPlanItems
            .Where(i => i.CompletedAtUtc != null && i.CompletedAtUtc >= rangeStartUtc && i.EstimatedCost != null)
            .Select(i => new { i.CompletedAtUtc, i.EstimatedCost })
            .ToListAsync();

        var byMonth = completedItems
            .GroupBy(i => (i.CompletedAtUtc!.Value.Year, i.CompletedAtUtc.Value.Month))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.EstimatedCost!.Value));

        var monthly = new List<MonthlyRevenueDto>();
        for (var i = 0; i < months; i++)
        {
            var month = rangeStartUtc.AddMonths(i);
            monthly.Add(new MonthlyRevenueDto(month.Year, month.Month, byMonth.GetValueOrDefault((month.Year, month.Month), 0)));
        }

        return Ok(new RevenueTrendDto(monthly));
    }

    private static DateOnly WeekStart(DateTime dt)
    {
        var date = DateOnly.FromDateTime(dt);
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
