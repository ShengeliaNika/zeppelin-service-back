using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zeppelin.Api.Dtos.Auth;
using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auth;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    ZeppelinDbContext db,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // lockoutOnFailure tracks failed attempts on the user (AccessFailedCount)
        // and locks the account out for a period once MaxFailedAccessAttempts is
        // hit, rather than a bare password check that never remembers failures.
        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Unauthorized(new { message = "Account locked due to repeated failed login attempts. Try again later." });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(await IssueTokensAsync(user, roles));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (existing is null || !existing.IsActive || existing.User is null || !existing.User.IsActive)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        existing.RevokedAtUtc = DateTime.UtcNow;

        var roles = await userManager.GetRolesAsync(existing.User);
        var response = await IssueTokensAsync(existing.User, roles);
        existing.ReplacedByTokenHash = tokenService.HashRefreshToken(response.RefreshToken);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return NoContent();
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, IList<string> roles)
    {
        var access = tokenService.GenerateAccessToken(user, roles);
        var refreshRaw = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshRaw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
        });
        await db.SaveChangesAsync();

        return new AuthResponse(
            access.AccessToken,
            access.ExpiresAtUtc,
            refreshRaw,
            new UserSummaryDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles.ToList()));
    }
}
