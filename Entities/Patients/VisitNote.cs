using Zeppelin.Common;
using Zeppelin.Entities.Identity;
using Zeppelin.Entities.Scheduling;

namespace Zeppelin.Entities.Patients;

public class VisitNote : IAuditable
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid AuthoredByUserId { get; set; }
    public ApplicationUser? AuthoredByUser { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public string? ProceduresPerformed { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
