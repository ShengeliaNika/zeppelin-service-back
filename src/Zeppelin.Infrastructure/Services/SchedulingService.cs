using Microsoft.EntityFrameworkCore;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Infrastructure.Services;

public class SchedulingConflictException(string message) : Exception(message);

public class SchedulingService(ZeppelinDbContext db)
{
    private static readonly AppointmentStatus[] InactiveStatuses = [AppointmentStatus.Cancelled, AppointmentStatus.NoShow];

    // Throws SchedulingConflictException if the requested dentist or chair is
    // already booked in the given window. excludeAppointmentId is used when
    // rescheduling an existing appointment so it doesn't conflict with itself.
    public async Task EnsureNoConflictAsync(
        Guid dentistUserId,
        Guid? chairId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        Guid? excludeAppointmentId = null)
    {
        var overlapping = db.Appointments.Where(a =>
            !InactiveStatuses.Contains(a.Status) &&
            a.StartAtUtc < endAtUtc &&
            startAtUtc < a.EndAtUtc);

        if (excludeAppointmentId is { } excludeId)
        {
            overlapping = overlapping.Where(a => a.Id != excludeId);
        }

        var dentistConflict = await overlapping.AnyAsync(a => a.DentistUserId == dentistUserId);
        if (dentistConflict)
        {
            throw new SchedulingConflictException("The selected dentist already has an appointment in this time range.");
        }

        if (chairId is { } chair)
        {
            var chairConflict = await overlapping.AnyAsync(a => a.ChairId == chair);
            if (chairConflict)
            {
                throw new SchedulingConflictException("The selected chair is already booked in this time range.");
            }
        }
    }
}
