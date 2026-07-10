using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeppelin.Entities.Patients;

namespace Zeppelin.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasIndex(p => new { p.LastName, p.FirstName });
        builder.HasIndex(p => p.PatientNumber).IsUnique();

        builder.HasMany(p => p.MedicalHistory).WithOne(m => m.Patient!)
            .HasForeignKey(m => m.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.ToothRecords).WithOne(t => t.Patient!)
            .HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.TreatmentPlans).WithOne(t => t.Patient!)
            .HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Attachments).WithOne(a => a.Patient!)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Appointments).WithOne(a => a.Patient!)
            .HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ToothRecordConfiguration : IEntityTypeConfiguration<ToothRecord>
{
    public void Configure(EntityTypeBuilder<ToothRecord> builder)
    {
        builder.HasIndex(t => new { t.PatientId, t.ToothNumber });
        builder.HasOne(t => t.RecordedByUser).WithMany()
            .HasForeignKey(t => t.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        builder.HasOne(t => t.CreatedByUser).WithMany()
            .HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Items).WithOne(i => i.TreatmentPlan!)
            .HasForeignKey(i => i.TreatmentPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TreatmentPlanItemConfiguration : IEntityTypeConfiguration<TreatmentPlanItem>
{
    public void Configure(EntityTypeBuilder<TreatmentPlanItem> builder)
    {
        builder.Property(i => i.EstimatedCost).HasPrecision(18, 2);
        builder.HasOne(i => i.Appointment).WithMany()
            .HasForeignKey(i => i.AppointmentId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class VisitNoteConfiguration : IEntityTypeConfiguration<VisitNote>
{
    public void Configure(EntityTypeBuilder<VisitNote> builder)
    {
        builder.HasOne(v => v.Appointment).WithMany()
            .HasForeignKey(v => v.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(v => v.Patient).WithMany()
            .HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.AuthoredByUser).WithMany()
            .HasForeignKey(v => v.AuthoredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasOne(a => a.Appointment).WithMany()
            .HasForeignKey(a => a.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.UploadedByUser).WithMany()
            .HasForeignKey(a => a.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
