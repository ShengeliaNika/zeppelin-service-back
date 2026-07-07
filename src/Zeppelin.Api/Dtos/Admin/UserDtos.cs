namespace Zeppelin.Api.Dtos.Admin;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? StaffTitle,
    bool IsActive,
    IReadOnlyList<string> Roles);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? StaffTitle,
    IReadOnlyList<string> Roles);

public record UpdateUserRequest(string FirstName, string LastName, string? StaffTitle, bool IsActive);

public record SetUserRolesRequest(IReadOnlyList<string> Roles);
