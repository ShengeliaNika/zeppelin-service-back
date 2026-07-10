using Zeppelin.Common;
using Zeppelin.Entities.Scheduling;
using Zeppelin.Enums;

namespace Zeppelin.Entities.Patients;

public class TreatmentPlanItem : IAuditable
{
    public Guid Id { get; set; }
    public Guid TreatmentPlanId { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ToothNumber { get; set; }
    public TreatmentPlanItemStatus Status { get; set; } = TreatmentPlanItemStatus.Planned;
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    // Captured now even though billing is out of scope for v1, so a future
    // billing phase has historical estimates to work from.
    public decimal? EstimatedCost { get; set; }

    // Set/cleared when Status transitions to/from Done - lets the dashboard
    // compute "estimated revenue this month" without real billing.
    public DateTime? CompletedAtUtc { get; set; }

    public int SortOrder { get; set; }
}
