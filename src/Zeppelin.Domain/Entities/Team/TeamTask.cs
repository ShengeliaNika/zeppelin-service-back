using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Team;

public class TeamTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public Guid AssignedByUserId { get; set; }
    public ApplicationUser? AssignedByUser { get; set; }

    public TeamTaskStatus Status { get; set; } = TeamTaskStatus.Open;
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
