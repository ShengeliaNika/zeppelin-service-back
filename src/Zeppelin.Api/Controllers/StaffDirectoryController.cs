using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zeppelin.Api.Dtos.Scheduling;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Identity;

namespace Zeppelin.Api.Controllers;

// Read-only staff directory for populating dentist/staff pickers (e.g. when
// booking an appointment) without exposing the full user-management surface
// of UsersController to non-Admin roles.
[ApiController]
[Route("api/staff")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class StaffDirectoryController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffDirectoryEntryDto>>> GetAll()
    {
        var users = userManager.Users.Where(u => u.IsActive).OrderBy(u => u.LastName).ToList();
        var result = new List<StaffDirectoryEntryDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new StaffDirectoryEntryDto(user.Id, user.FirstName, user.LastName, roles.ToList()));
        }

        return Ok(result);
    }
}
