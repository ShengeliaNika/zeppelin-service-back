using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeppelin.Entities.Scheduling;

namespace Zeppelin.Configurations;

public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
{
    public void Configure(EntityTypeBuilder<WorkingHours> builder)
    {
        builder.HasIndex(w => w.DayOfWeek).IsUnique();
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasOne(a => a.Patient).WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.DentistUser).WithMany()
            .HasForeignKey(a => a.DentistUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.CreatedByUser).WithMany()
            .HasForeignKey(a => a.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Chair).WithMany()
            .HasForeignKey(a => a.ChairId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AppointmentType).WithMany()
            .HasForeignKey(a => a.AppointmentTypeId).OnDelete(DeleteBehavior.Restrict);

        // Backs the conflict-detection query in SchedulingService.
        builder.HasIndex(a => new { a.DentistUserId, a.StartAtUtc });
        builder.HasIndex(a => new { a.ChairId, a.StartAtUtc });
    }
}

public class RecallReminderConfiguration : IEntityTypeConfiguration<RecallReminder>
{
    public void Configure(EntityTypeBuilder<RecallReminder> builder)
    {
        builder.HasOne(r => r.Patient).WithMany()
            .HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.AppointmentType).WithMany()
            .HasForeignKey(r => r.AppointmentTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
