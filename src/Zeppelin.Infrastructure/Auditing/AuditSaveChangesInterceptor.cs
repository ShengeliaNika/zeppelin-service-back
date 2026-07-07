using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Audit;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Infrastructure.Auditing;

// Watches every SaveChanges call for IAuditable entities and writes an
// AuditLogEntry row automatically, so clinically/legally sensitive writes
// (patients, dental chart, treatment plans, visit notes, attachments) are
// never missed because someone forgot to call a logging method by hand.
public class AuditSaveChangesInterceptor(ICurrentUserAccessor currentUserAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AppendAuditEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AppendAuditEntries(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void AppendAuditEntries(DbContext context)
    {
        var userId = currentUserAccessor.UserId;
        if (userId is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated,
            };

            var changedFields = BuildChangedFields(entry, action);

            context.Add(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                EntityName = entry.Entity.GetType().Name,
                EntityId = ((IAuditable)entry.Entity).Id,
                Action = action,
                ChangedFieldsJson = changedFields,
                TimestampUtc = DateTime.UtcNow,
            });
        }
    }

    private static string BuildChangedFields(EntityEntry entry, AuditAction action)
    {
        if (action == AuditAction.Created)
        {
            var values = entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p]);
            return JsonSerializer.Serialize(values);
        }

        if (action == AuditAction.Deleted)
        {
            return JsonSerializer.Serialize(new { Deleted = true });
        }

        var changed = entry.Properties
            .Where(p => p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => new { Old = p.OriginalValue, New = p.CurrentValue });

        return JsonSerializer.Serialize(changed);
    }
}
