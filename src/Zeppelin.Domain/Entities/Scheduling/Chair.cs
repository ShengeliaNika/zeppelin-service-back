namespace Zeppelin.Domain.Entities.Scheduling;

public class Chair
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
