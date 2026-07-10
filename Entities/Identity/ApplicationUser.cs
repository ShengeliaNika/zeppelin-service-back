using Microsoft.AspNetCore.Identity;
using Zeppelin.Enums;

namespace Zeppelin.Entities.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? StaffTitle { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Self-registered accounts start Pending and can't log in until an admin
    // approves them and assigns roles; admin-created accounts are Approved immediately.
    public UserApprovalStatus ApprovalStatus { get; set; } = UserApprovalStatus.Approved;
    public DateTime? ApprovalDecidedAtUtc { get; set; }
    public Guid? ApprovalDecidedByUserId { get; set; }
}
