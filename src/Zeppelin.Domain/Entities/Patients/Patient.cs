using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Scheduling;

namespace Zeppelin.Domain.Entities.Patients;

public class Patient : IAuditable
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public string? InsuranceGroupNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<MedicalHistoryEntry> MedicalHistory { get; set; } = [];
    public List<ToothRecord> ToothRecords { get; set; } = [];
    public List<TreatmentPlan> TreatmentPlans { get; set; } = [];
    public List<Attachment> Attachments { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}
