namespace Zeppelin.Domain.Entities.Scheduling;

public class AppointmentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultDurationMinutes { get; set; } = 30;
    public string? Color { get; set; }

    // Nullable - only checkup-style types (e.g. 6-month cleaning) generate
    // recall reminders. Null means "no recall tracking for this type."
    public int? RecallIntervalMonths { get; set; }

    public bool IsActive { get; set; } = true;
}
