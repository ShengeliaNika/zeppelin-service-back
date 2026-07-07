using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeppelin.Domain.Entities.Inventory;

namespace Zeppelin.Infrastructure.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.Property(i => i.CurrentStock).HasPrecision(18, 2);
        builder.Property(i => i.ParLevel).HasPrecision(18, 2);
        builder.HasIndex(i => i.Name);

        builder.HasMany(i => i.Batches).WithOne(b => b.InventoryItem!)
            .HasForeignKey(b => b.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Movements).WithOne(m => m.InventoryItem!)
            .HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.Property(b => b.QuantityRemaining).HasPrecision(18, 2);
        builder.HasIndex(b => b.ExpiryDate);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(m => m.Quantity).HasPrecision(18, 2);

        builder.HasOne(m => m.InventoryBatch).WithMany()
            .HasForeignKey(m => m.InventoryBatchId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.AppointmentType).WithMany()
            .HasForeignKey(m => m.AppointmentTypeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Appointment).WithMany()
            .HasForeignKey(m => m.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.RecordedByUser).WithMany()
            .HasForeignKey(m => m.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
