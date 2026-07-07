using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Scheduling;

public class RecallReminder
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public RecallReminderStatus Status { get; set; } = RecallReminderStatus.Pending;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
