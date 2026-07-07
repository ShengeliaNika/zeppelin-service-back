using Zeppelin.Domain.Common;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Patients;

public class MedicalHistoryEntry : IAuditable
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public MedicalHistoryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime NotedAtUtc { get; set; } = DateTime.UtcNow;
}
