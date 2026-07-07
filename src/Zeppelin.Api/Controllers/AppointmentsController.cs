using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Scheduling;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AppointmentsController(ZeppelinDbContext db, SchedulingService schedulingService, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? dentistUserId, [FromQuery] Guid? chairId)
    {
        var query = db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.DentistUser)
            .Include(a => a.Chair)
            .Include(a => a.AppointmentType)
            .Where(a => a.StartAtUtc < to && from < a.EndAtUtc)
            .AsQueryable();

        if (dentistUserId is { } dentist)
        {
            query = query.Where(a => a.DentistUserId == dentist);
        }

        if (chairId is { } chair)
        {
            query = query.Where(a => a.ChairId == chair);
        }

        var appointments = await query.OrderBy(a => a.StartAtUtc).ToListAsync();
        return Ok(appointments.Select(ToDto).ToList());
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

        return appointment is null ? NotFound() : Ok(ToDto(appointment));
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
        appointment.CancelledReason = request.Status == Domain.Enums.AppointmentStatus.Cancelled ? request.CancelledReason : null;

        await db.SaveChangesAsync();
        return await GetById(id);
    }

    private static AppointmentDto ToDto(Appointment a) => new(
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
        a.CancelledReason);
}
