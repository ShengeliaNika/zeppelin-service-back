using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Zeppelin.Dtos.Admin;
using Zeppelin.Common;
using Zeppelin.Entities.Identity;
using Zeppelin.Enums;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.AdminOnly)]
public class UsersController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll()
    {
        var users = userManager.Users.OrderBy(u => u.LastName).ToList();
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(await ToDtoAsync(user));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? NotFound() : Ok(await ToDtoAsync(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            StaffTitle = request.StaffTitle,
            EmailConfirmed = true,
            // Admin is creating the account directly, so it's approved on arrival -
            // no separate approval step needed for staff an admin already vetted.
            ApprovalStatus = UserApprovalStatus.Approved,
            ApprovalDecidedAtUtc = DateTime.UtcNow,
            ApprovalDecidedByUserId = CurrentUserId(),
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRolesAsync(user, request.Roles);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, await ToDtoAsync(user));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<UserDto>> Approve(Guid id, ApproveUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.ApprovalStatus = UserApprovalStatus.Approved;
        user.ApprovalDecidedAtUtc = DateTime.UtcNow;
        user.ApprovalDecidedByUserId = CurrentUserId();

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return ValidationProblem(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRolesAsync(user, request.Roles);

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<ActionResult<UserDto>> Decline(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.ApprovalStatus = UserApprovalStatus.Declined;
        user.ApprovalDecidedAtUtc = DateTime.UtcNow;
        user.ApprovalDecidedByUserId = CurrentUserId();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ValidationProblem(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.StaffTitle = request.StaffTitle;
        user.IsActive = request.IsActive;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return ValidationProblem(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UserDto>> SetRoles(Guid id, SetUserRolesRequest request)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRolesAsync(user, request.Roles);

        return Ok(await ToDtoAsync(user));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.StaffTitle,
            user.IsActive,
            roles.ToList(),
            user.ApprovalStatus.ToString(),
            user.ApprovalDecidedAtUtc);
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
