namespace Zeppelin.Domain.Enums;

public enum MedicalHistoryType
{
    Allergy,
    Medication,
    Condition,
}

public enum ToothStatus
{
    Healthy,
    Decayed,
    Filled,
    Crowned,
    RootCanal,
    Missing,
    Implant,
    Extracted,
}

public enum TreatmentPlanStatus
{
    Draft,
    Active,
    Completed,
    Cancelled,
}

public enum TreatmentPlanItemStatus
{
    Planned,
    InProgress,
    Done,
    Cancelled,
}

public enum AttachmentType
{
    Xray,
    Photo,
    ConsentForm,
    Other,
}

public enum InventoryCategory
{
    Consumable,
    Anesthetic,
    Ppe,
    Instrument,
    Other,
}

public enum StockMovementType
{
    Restock,
    UsageDeduction,
    Waste,
    Adjustment,
}

public enum AppointmentStatus
{
    Scheduled,
    Confirmed,
    CheckedIn,
    Completed,
    NoShow,
    Cancelled,
}

public enum RecallReminderStatus
{
    Pending,
    Scheduled,
    Dismissed,
}

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
}
