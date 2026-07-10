using Microsoft.EntityFrameworkCore;
using Zeppelin.Entities.Scheduling;
using Zeppelin.Enums;

namespace Zeppelin.Services;

public class RecallReminderService(ZeppelinDbContext db)
{
    // Computed on demand rather than via a background job (no scheduler
    // infra in v1) - idempotent, so calling this repeatedly just fills in
    // any newly-due reminders without duplicating existing ones.
    public async Task<List<RecallReminder>> GetDueRemindersAsync()
    {
        await GenerateDueRemindersAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.RecallReminders
            .Include(r => r.Patient)
            .Include(r => r.AppointmentType)
            .Where(r => r.Status == RecallReminderStatus.Pending && r.DueDate <= today)
            .OrderBy(r => r.DueDate)
            .ToListAsync();
    }

    public async Task DismissAsync(Guid id)
    {
        var reminder = await db.RecallReminders.FirstOrDefaultAsync(r => r.Id == id);
        if (reminder is not null)
        {
            reminder.Status = RecallReminderStatus.Dismissed;
            await db.SaveChangesAsync();
        }
    }

    private async Task GenerateDueRemindersAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recallTypes = await db.AppointmentTypes
            .Where(t => t.IsActive && t.RecallIntervalMonths != null)
            .ToListAsync();

        var nowUtc = DateTime.UtcNow;

        foreach (var type in recallTypes)
        {
            // EndAtUtc > now shouldn't happen for a genuinely Completed visit,
            // but excluding it guards against exactly that kind of bad data
            // skewing "last completed" into the future and pushing the recall
            // due date out indefinitely.
            var lastCompletedByPatient = await db.Appointments
                .Where(a => a.AppointmentTypeId == type.Id && a.Status == AppointmentStatus.Completed && a.EndAtUtc <= nowUtc)
                .GroupBy(a => a.PatientId)
                .Select(g => new { PatientId = g.Key, LastCompletedUtc = g.Max(a => a.EndAtUtc) })
                .ToListAsync();

            foreach (var entry in lastCompletedByPatient)
            {
                var dueDate = DateOnly.FromDateTime(entry.LastCompletedUtc.AddMonths(type.RecallIntervalMonths!.Value));
                if (dueDate > today)
                {
                    continue;
                }

                var alreadyExists = await db.RecallReminders.AnyAsync(r =>
                    r.PatientId == entry.PatientId && r.AppointmentTypeId == type.Id && r.DueDate == dueDate);

                if (!alreadyExists)
                {
                    db.RecallReminders.Add(new RecallReminder
                    {
                        Id = Guid.NewGuid(),
                        PatientId = entry.PatientId,
                        AppointmentTypeId = type.Id,
                        DueDate = dueDate,
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
