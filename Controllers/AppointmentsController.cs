using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Dtos.Inventory;
using Zeppelin.Dtos.Scheduling;
using Zeppelin.Common;
using Zeppelin.Entities.Scheduling;
using Zeppelin;
using Zeppelin.Auditing;
using Zeppelin.Services;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AppointmentsController(ZeppelinDbContext db, SchedulingService schedulingService, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? dentistUserId, [FromQuery] Guid? chairId,
        [FromQuery] Guid? patientId, [FromQuery] Enums.AppointmentStatus? status)
    {
        var query = db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DentistUser)
            .Include(a => a.Chair)
            .Include(a => a.AppointmentType)
            .AsQueryable();

        if (from is not null && to is not null)
        {
            query = query.Where(a => a.StartAtUtc < to && from < a.EndAtUtc);
        }

        if (dentistUserId is { } dentist)
        {
            query = query.Where(a => a.DentistUserId == dentist);
        }

        if (chairId is { } chair)
        {
            query = query.Where(a => a.ChairId == chair);
        }

        if (patientId is { } patient)
        {
            query = query.Where(a => a.PatientId == patient);
        }

        if (status is { } s)
        {
            query = query.Where(a => a.Status == s);
        }

        var appointments = await query.OrderBy(a => a.StartAtUtc).ToListAsync();

        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var loggedIds = (await db.StockMovements
                .Where(m => m.AppointmentId != null && appointmentIds.Contains(m.AppointmentId.Value))
                .Select(m => m.AppointmentId!.Value)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        return Ok(appointments.Select(a => ToDto(a, loggedIds.Contains(a.Id))).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id)
    {
        var appointment = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DentistUser)
            .Include(a => a.Chair)
            .Include(a => a.AppointmentType)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null)
        {
            return NotFound();
        }

        var hasLoggedSupplies = await db.StockMovements.AnyAsync(m => m.AppointmentId == id);
        return Ok(ToDto(appointment, hasLoggedSupplies));
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request)
    {
        try
        {
            await schedulingService.EnsureNoConflictAsync(request.DentistUserId, request.ChairId, request.StartAtUtc, request.EndAtUtc);
        }
        catch (SchedulingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DentistUserId = request.DentistUserId,
            ChairId = request.ChairId,
            AppointmentTypeId = request.AppointmentTypeId,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            Notes = request.Notes,
            CreatedByUserId = currentUser.UserId!.Value,
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return await GetById(appointment.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(Guid id, RescheduleAppointmentRequest request)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        try
        {
            await schedulingService.EnsureNoConflictAsync(
                request.DentistUserId, request.ChairId, request.StartAtUtc, request.EndAtUtc, excludeAppointmentId: id);
        }
        catch (SchedulingConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }

        appointment.DentistUserId = request.DentistUserId;
        appointment.ChairId = request.ChairId;
        appointment.AppointmentTypeId = request.AppointmentTypeId;
        appointment.StartAtUtc = request.StartAtUtc;
        appointment.EndAtUtc = request.EndAtUtc;
        appointment.Notes = request.Notes;

        await db.SaveChangesAsync();
        return await GetById(id);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(Guid id, UpdateAppointmentStatusRequest request)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        appointment.Status = request.Status;
        appointment.CancelledReason = request.Status == Enums.AppointmentStatus.Cancelled ? request.CancelledReason : null;

        await db.SaveChangesAsync();
        return await GetById(id);
    }

    // Supplies a doctor logged as used during this visit (UsageDeduction/Waste
    // stock movements tagged with this AppointmentId) - lets "what did we use
    // for this session" be reviewed from the appointment itself.
    [HttpGet("{id:guid}/stock-movements")]
    public async Task<ActionResult<IReadOnlyList<AppointmentSupplyUsageDto>>> GetSupplyUsage(Guid id)
    {
        var movements = await db.StockMovements
            .Include(m => m.InventoryItem)
            .Include(m => m.RecordedByUser)
            .Where(m => m.AppointmentId == id)
            .OrderByDescending(m => m.RecordedAtUtc)
            .ToListAsync();

        return Ok(movements.Select(m => new AppointmentSupplyUsageDto(
            m.Id,
            m.InventoryItemId,
            m.InventoryItem!.Name,
            m.InventoryItem.Unit,
            m.Type,
            m.Quantity,
            m.RecordedByUser is null ? string.Empty : $"{m.RecordedByUser.FirstName} {m.RecordedByUser.LastName}",
            m.RecordedAtUtc)).ToList());
    }

    internal static AppointmentDto ToDto(Appointment a, bool hasLoggedSupplies = false) => new(
        a.Id,
        a.PatientId,
        $"{a.Patient!.FirstName} {a.Patient.LastName}",
        a.DentistUserId,
        $"{a.DentistUser!.FirstName} {a.DentistUser.LastName}",
        a.ChairId,
        a.Chair?.Name,
        a.AppointmentTypeId,
        a.AppointmentType!.Name,
        a.StartAtUtc,
        a.EndAtUtc,
        a.Status,
        a.Notes,
        a.CancelledReason,
        hasLoggedSupplies);
}
