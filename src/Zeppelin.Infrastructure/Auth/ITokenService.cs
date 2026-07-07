using Zeppelin.Domain.Entities.Identity;

namespace Zeppelin.Infrastructure.Auth;

public record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(ApplicationUser user, IList<string> roles);

    // Returns the raw refresh token (given to the client) - callers are
    // responsible for persisting its hash, never the raw value.
    string GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}
