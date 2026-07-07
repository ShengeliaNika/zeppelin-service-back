using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Scheduling;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/chairs")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class ChairsController(ZeppelinDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ChairDto>>> GetAll()
    {
        var chairs = await db.Chairs
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new ChairDto(c.Id, c.Name, c.IsActive))
            .ToListAsync();

        return Ok(chairs);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<ChairDto>> Create(CreateChairRequest request)
    {
        var chair = new Chair { Id = Guid.NewGuid(), Name = request.Name };
        db.Chairs.Add(chair);
        await db.SaveChangesAsync();
        return Ok(new ChairDto(chair.Id, chair.Name, chair.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var chair = await db.Chairs.FirstOrDefaultAsync(c => c.Id == id);
        if (chair is null)
        {
            return NotFound();
        }

        chair.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
