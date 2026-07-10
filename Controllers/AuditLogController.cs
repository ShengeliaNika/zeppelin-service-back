using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Dtos.Admin;
using Zeppelin.Dtos.Common;
using Zeppelin.Common;
using Zeppelin;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = Policies.AdminOnly)]
public class AuditLogController(ZeppelinDbContext db) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogEntryDto>>> GetAll(
        [FromQuery] string? entityName, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        take = Math.Clamp(take, 1, MaxPageSize);
        skip = Math.Max(0, skip);

        var query = db.AuditLogEntries.Include(e => e.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(e => e.EntityName == entityName);
        }

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderByDescending(e => e.TimestampUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var result = entries.Select(e => new AuditLogEntryDto(
            e.Id,
            e.User is null ? string.Empty : $"{e.User.FirstName} {e.User.LastName}",
            e.EntityName,
            e.EntityId,
            e.Action,
            e.ChangedFieldsJson,
            e.TimestampUtc)).ToList();

        return Ok(new PagedResultDto<AuditLogEntryDto>(result, totalCount, skip, take));
    }
}
