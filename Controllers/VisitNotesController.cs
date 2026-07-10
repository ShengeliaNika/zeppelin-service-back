using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Dtos.Clinical;
using Zeppelin.Common;
using Zeppelin.Entities.Patients;
using Zeppelin;
using Zeppelin.Auditing;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/appointments/{appointmentId:guid}/visit-notes")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class VisitNotesController(ZeppelinDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VisitNoteDto>>> GetAll(Guid appointmentId)
    {
        var notes = await db.VisitNotes
            .Include(n => n.AuthoredByUser)
            .Where(n => n.AppointmentId == appointmentId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();

        return Ok(notes.Select(ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<VisitNoteDto>> Create(Guid appointmentId, CreateVisitNoteRequest request)
    {
        var appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
        {
            return NotFound();
        }

        var note = new VisitNote
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            AuthoredByUserId = currentUser.UserId!.Value,
            NoteText = request.NoteText,
            ProceduresPerformed = request.ProceduresPerformed,
        };

        db.VisitNotes.Add(note);
        await db.SaveChangesAsync();
        await db.Entry(note).Reference(n => n.AuthoredByUser).LoadAsync();

        return Ok(ToDto(note));
    }

    internal static VisitNoteDto ToDto(VisitNote note) => new(
        note.Id,
        note.AppointmentId,
        note.PatientId,
        note.AuthoredByUser is null ? string.Empty : $"{note.AuthoredByUser.FirstName} {note.AuthoredByUser.LastName}",
        note.NoteText,
        note.ProceduresPerformed,
        note.CreatedAtUtc);
}
