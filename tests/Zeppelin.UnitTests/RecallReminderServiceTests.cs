using Microsoft.EntityFrameworkCore;
using Xunit;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.UnitTests;

public class RecallReminderServiceTests
{
    private static ZeppelinDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeppelinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeppelinDbContext(options);
    }

    private static AppointmentType MakeCheckupType(int recallMonths) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Checkup",
        DefaultDurationMinutes = 30,
        RecallIntervalMonths = recallMonths,
    };

    // GetDueRemindersAsync Includes Patient/AppointmentType, and PatientId is
    // a required (non-nullable) relationship - EF Core's Include behaves
    // like an inner join for required references, so a reminder whose
    // PatientId doesn't match a real Patient row would silently be filtered
    // out of the result. A real Patient row is needed for these tests to
    // exercise the actual query path.
    private static Patient MakePatient() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "Patient",
        DateOfBirth = new DateOnly(1990, 1, 1),
    };

    [Fact]
    public async Task GeneratesReminderWhenRecallIntervalHasElapsed()
    {
        await using var db = CreateContext();
        var type = MakeCheckupType(6);
        var patient = MakePatient();
        var completedAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(-7),
            EndAtUtc = DateTime.UtcNow.AddMonths(-7).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };

        db.AppointmentTypes.Add(type);
        db.Patients.Add(patient);
        db.Appointments.Add(completedAppointment);
        await db.SaveChangesAsync();

        var service = new RecallReminderService(db);
        var due = await service.GetDueRemindersAsync();

        var reminder = Assert.Single(due);
        Assert.Equal(patient.Id, reminder.PatientId);
        Assert.Equal(RecallReminderStatus.Pending, reminder.Status);
    }

    [Fact]
    public async Task DoesNotGenerateReminderWhenRecallIntervalHasNotElapsed()
    {
        await using var db = CreateContext();
        var type = MakeCheckupType(6);
        var patient = MakePatient();
        var completedAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(-2),
            EndAtUtc = DateTime.UtcNow.AddMonths(-2).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };

        db.AppointmentTypes.Add(type);
        db.Patients.Add(patient);
        db.Appointments.Add(completedAppointment);
        await db.SaveChangesAsync();

        var service = new RecallReminderService(db);
        var due = await service.GetDueRemindersAsync();

        Assert.Empty(due);
    }

    [Fact]
    public async Task IgnoresFutureDatedCompletedAppointmentWhenComputingLastVisit()
    {
        // Regression test: a Completed appointment dated in the future (bad
        // data - can't happen in real usage, but seen from test fixtures)
        // must not be treated as the patient's "last visit," which would
        // push the recall due date out indefinitely and mask a genuinely
        // due reminder from an earlier real visit.
        await using var db = CreateContext();
        var type = MakeCheckupType(6);
        var patient = MakePatient();
        var genuinePastVisit = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(-7),
            EndAtUtc = DateTime.UtcNow.AddMonths(-7).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };
        var futureDatedCompleted = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(1),
            EndAtUtc = DateTime.UtcNow.AddMonths(1).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };

        db.AppointmentTypes.Add(type);
        db.Patients.Add(patient);
        db.Appointments.AddRange(genuinePastVisit, futureDatedCompleted);
        await db.SaveChangesAsync();

        var service = new RecallReminderService(db);
        var due = await service.GetDueRemindersAsync();

        var reminder = Assert.Single(due);
        Assert.Equal(patient.Id, reminder.PatientId);
    }

    [Fact]
    public async Task IsIdempotentAcrossMultipleCalls()
    {
        await using var db = CreateContext();
        var type = MakeCheckupType(6);
        var patient = MakePatient();
        var completedAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(-7),
            EndAtUtc = DateTime.UtcNow.AddMonths(-7).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };

        db.AppointmentTypes.Add(type);
        db.Patients.Add(patient);
        db.Appointments.Add(completedAppointment);
        await db.SaveChangesAsync();

        var service = new RecallReminderService(db);
        await service.GetDueRemindersAsync();
        var secondCall = await service.GetDueRemindersAsync();

        Assert.Single(secondCall);
    }

    [Fact]
    public async Task DismissedReminderIsExcludedFromDueList()
    {
        await using var db = CreateContext();
        var type = MakeCheckupType(6);
        var patient = MakePatient();
        var completedAppointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            DentistUserId = Guid.NewGuid(),
            AppointmentTypeId = type.Id,
            StartAtUtc = DateTime.UtcNow.AddMonths(-7),
            EndAtUtc = DateTime.UtcNow.AddMonths(-7).AddMinutes(30),
            Status = AppointmentStatus.Completed,
            CreatedByUserId = Guid.NewGuid(),
        };

        db.AppointmentTypes.Add(type);
        db.Patients.Add(patient);
        db.Appointments.Add(completedAppointment);
        await db.SaveChangesAsync();

        var service = new RecallReminderService(db);
        var due = await service.GetDueRemindersAsync();
        await service.DismissAsync(due.Single().Id);

        var afterDismiss = await service.GetDueRemindersAsync();
        Assert.Empty(afterDismiss);
    }
}
