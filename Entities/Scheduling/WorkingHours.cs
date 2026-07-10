namespace Zeppelin.Entities.Scheduling;

// One row per weekday, since dental clinics commonly have shorter Fri/Sat hours.
public class WorkingHours
{
    public Guid Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
}
