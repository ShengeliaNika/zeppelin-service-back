namespace Zeppelin.Dtos.Admin;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? StaffTitle,
    bool IsActive,
    IReadOnlyList<string> Roles,
    string ApprovalStatus,
    DateTime? ApprovalDecidedAtUtc);

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? StaffTitle,
    IReadOnlyList<string> Roles);

public record UpdateUserRequest(string FirstName, string LastName, string? StaffTitle, bool IsActive);

public record SetUserRolesRequest(IReadOnlyList<string> Roles);

public record ApproveUserRequest(IReadOnlyList<string> Roles);
