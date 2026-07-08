using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Domain.Entities.Audit;
using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Domain.Entities.Team;

namespace Zeppelin.Infrastructure;

public class ZeppelinDbContext(DbContextOptions<ZeppelinDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalHistoryEntry> MedicalHistoryEntries => Set<MedicalHistoryEntry>();
    public DbSet<ToothRecord> ToothRecords => Set<ToothRecord>();
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();
    public DbSet<VisitNote> VisitNotes => Set<VisitNote>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemSupplier> ItemSuppliers => Set<ItemSupplier>();

    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<Chair> Chairs => Set<Chair>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<RecallReminder> RecallReminders => Set<RecallReminder>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<TeamTask> TeamTasks => Set<TeamTask>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ZeppelinDbContext).Assembly);
    }
}
