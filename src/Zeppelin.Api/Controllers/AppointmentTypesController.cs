using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Scheduling;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/appointment-types")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AppointmentTypesController(ZeppelinDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentTypeDto>>> GetAll()
    {
        var types = await db.AppointmentTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new AppointmentTypeDto(t.Id, t.Name, t.DefaultDurationMinutes, t.Color, t.RecallIntervalMonths, t.IsActive))
            .ToListAsync();

        return Ok(types);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<AppointmentTypeDto>> Create(CreateAppointmentTypeRequest request)
    {
        var type = new AppointmentType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
            Color = request.Color,
            RecallIntervalMonths = request.RecallIntervalMonths,
        };

        db.AppointmentTypes.Add(type);
        await db.SaveChangesAsync();

        return Ok(new AppointmentTypeDto(type.Id, type.Name, type.DefaultDurationMinutes, type.Color, type.RecallIntervalMonths, type.IsActive));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var type = await db.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            return NotFound();
        }

        type.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
