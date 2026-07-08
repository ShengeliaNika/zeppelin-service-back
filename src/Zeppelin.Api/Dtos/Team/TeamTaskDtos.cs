using Zeppelin.Domain.Enums;

namespace Zeppelin.Api.Dtos.Team;

public record TeamTaskDto(
    Guid Id,
    string Title,
    string? Description,
    Guid AssignedToUserId,
    string AssignedToName,
    Guid AssignedByUserId,
    string AssignedByName,
    TeamTaskStatus Status,
    DateOnly? DueDate,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public record CreateTeamTaskRequest(string Title, string? Description, Guid AssignedToUserId, DateOnly? DueDate);

public record UpdateTeamTaskStatusRequest(TeamTaskStatus Status);
