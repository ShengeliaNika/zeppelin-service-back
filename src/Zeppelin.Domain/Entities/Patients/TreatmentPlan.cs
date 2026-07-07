using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Patients;

public class TreatmentPlan : IAuditable
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TreatmentPlanItem> Items { get; set; } = [];
}
