using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Team;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Team;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;

namespace Zeppelin.Api.Controllers;

// Lightweight internal coordination tool - any staff member can assign a task
// to any other staff member. Not tied to patients/appointments; deliberately
// simple (no priority, no sub-tasks, no comments) for v1.
[ApiController]
[Route("api/team-tasks")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class TeamTasksController(ZeppelinDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamTaskDto>>> GetAll(
        [FromQuery] bool mine = false, [FromQuery] TeamTaskStatus? status = null)
    {
        var query = db.TeamTasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .AsQueryable();

        if (mine)
        {
            query = query.Where(t => t.AssignedToUserId == currentUser.UserId!.Value);
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        var tasks = await query
            .OrderBy(t => t.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        return Ok(tasks.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<TeamTaskDto>> Create(CreateTeamTaskRequest request)
    {
        var task = new TeamTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = currentUser.UserId!.Value,
            DueDate = request.DueDate,
        };

        db.TeamTasks.Add(task);
        await db.SaveChangesAsync();

        await db.Entry(task).Reference(t => t.AssignedToUser).LoadAsync();
        await db.Entry(task).Reference(t => t.AssignedByUser).LoadAsync();

        return Ok(ToDto(task));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<TeamTaskDto>> UpdateStatus(Guid id, UpdateTeamTaskStatusRequest request)
    {
        var task = await db.TeamTasks
            .Include(t => t.AssignedToUser)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
        {
            return NotFound();
        }

        task.Status = request.Status;
        task.CompletedAtUtc = request.Status == TeamTaskStatus.Done ? DateTime.UtcNow : null;

        await db.SaveChangesAsync();
        return Ok(ToDto(task));
    }

    private static TeamTaskDto ToDto(TeamTask t) => new(
        t.Id,
        t.Title,
        t.Description,
        t.AssignedToUserId,
        t.AssignedToUser is null ? string.Empty : $"{t.AssignedToUser.FirstName} {t.AssignedToUser.LastName}",
        t.AssignedByUserId,
        t.AssignedByUser is null ? string.Empty : $"{t.AssignedByUser.FirstName} {t.AssignedByUser.LastName}",
        t.Status,
        t.DueDate,
        t.CreatedAtUtc,
        t.CompletedAtUtc);
}
