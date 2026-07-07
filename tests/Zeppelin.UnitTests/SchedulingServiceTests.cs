using Microsoft.EntityFrameworkCore;
using Xunit;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.UnitTests;

public class SchedulingServiceTests
{
    private static ZeppelinDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeppelinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeppelinDbContext(options);
    }

    private static Appointment MakeAppointment(Guid dentistId, Guid? chairId, DateTime start, DateTime end, AppointmentStatus status = AppointmentStatus.Scheduled) => new()
    {
        Id = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        DentistUserId = dentistId,
        ChairId = chairId,
        AppointmentTypeId = Guid.NewGuid(),
        StartAtUtc = start,
        EndAtUtc = end,
        Status = status,
        CreatedByUserId = Guid.NewGuid(),
    };

    [Fact]
    public async Task ThrowsWhenSameDentistOverlaps()
    {
        await using var db = CreateContext();
        var dentistId = Guid.NewGuid();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        db.Appointments.Add(MakeAppointment(dentistId, null, day, day.AddMinutes(30)));
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);

        await Assert.ThrowsAsync<SchedulingConflictException>(() =>
            service.EnsureNoConflictAsync(dentistId, null, day.AddMinutes(15), day.AddMinutes(45)));
    }

    [Fact]
    public async Task AllowsDifferentDentistSameTime()
    {
        await using var db = CreateContext();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        db.Appointments.Add(MakeAppointment(Guid.NewGuid(), null, day, day.AddMinutes(30)));
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);

        await service.EnsureNoConflictAsync(Guid.NewGuid(), null, day, day.AddMinutes(30));
        // No exception thrown = success.
    }

    [Fact]
    public async Task IgnoresCancelledAppointmentsWhenCheckingConflicts()
    {
        await using var db = CreateContext();
        var dentistId = Guid.NewGuid();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        db.Appointments.Add(MakeAppointment(dentistId, null, day, day.AddMinutes(30), AppointmentStatus.Cancelled));
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);

        await service.EnsureNoConflictAsync(dentistId, null, day, day.AddMinutes(30));
    }

    [Fact]
    public async Task ThrowsWhenSameChairOverlapsForDifferentDentist()
    {
        await using var db = CreateContext();
        var chairId = Guid.NewGuid();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        db.Appointments.Add(MakeAppointment(Guid.NewGuid(), chairId, day, day.AddMinutes(30)));
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);

        await Assert.ThrowsAsync<SchedulingConflictException>(() =>
            service.EnsureNoConflictAsync(Guid.NewGuid(), chairId, day.AddMinutes(10), day.AddMinutes(40)));
    }

    [Fact]
    public async Task ExcludesOwnAppointmentWhenRescheduling()
    {
        await using var db = CreateContext();
        var dentistId = Guid.NewGuid();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        var appointment = MakeAppointment(dentistId, null, day, day.AddMinutes(30));
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);

        // Rescheduling the same appointment to a slightly shifted time should
        // not conflict with itself.
        await service.EnsureNoConflictAsync(
            dentistId, null, day.AddMinutes(10), day.AddMinutes(40), excludeAppointmentId: appointment.Id);
    }

    [Theory]
    [InlineData(-30, 0, false)] // ends exactly when the existing appointment starts - no overlap
    [InlineData(30, 60, false)] // starts exactly when the existing appointment ends - no overlap
    [InlineData(29, 31, true)]  // overlaps by a minute
    public async Task BoundaryOverlapCases(int startOffsetMinutes, int endOffsetMinutes, bool expectConflict)
    {
        await using var db = CreateContext();
        var dentistId = Guid.NewGuid();
        var day = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);
        db.Appointments.Add(MakeAppointment(dentistId, null, day, day.AddMinutes(30)));
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);
        var start = day.AddMinutes(startOffsetMinutes);
        var end = day.AddMinutes(endOffsetMinutes);

        if (expectConflict)
        {
            await Assert.ThrowsAsync<SchedulingConflictException>(() => service.EnsureNoConflictAsync(dentistId, null, start, end));
        }
        else
        {
            await service.EnsureNoConflictAsync(dentistId, null, start, end);
        }
    }
}
