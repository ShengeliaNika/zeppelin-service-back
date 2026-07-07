namespace Zeppelin.Api.Dtos.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record UserSummaryDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles);

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    UserSummaryDto User);
