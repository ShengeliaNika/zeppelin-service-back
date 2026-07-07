using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Audit;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? ChangedFieldsJson { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
