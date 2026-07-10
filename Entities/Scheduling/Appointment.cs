using Zeppelin.Common;
using Zeppelin.Entities.Identity;
using Zeppelin.Entities.Patients;
using Zeppelin.Enums;

namespace Zeppelin.Entities.Scheduling;

public class Appointment : IAuditable
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid DentistUserId { get; set; }
    public ApplicationUser? DentistUser { get; set; }
    public Guid? ChairId { get; set; }
    public Chair? Chair { get; set; }
    public Guid AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CancelledReason { get; set; }
}
