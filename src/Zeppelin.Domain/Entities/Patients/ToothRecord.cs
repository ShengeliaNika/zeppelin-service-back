using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Patients;

// ToothNumber uses the Universal Numbering System (1-32).
public class ToothRecord : IAuditable
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int ToothNumber { get; set; }
    public ToothStatus Status { get; set; }
    public string? Surface { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid RecordedByUserId { get; set; }
    public ApplicationUser? RecordedByUser { get; set; }
}
