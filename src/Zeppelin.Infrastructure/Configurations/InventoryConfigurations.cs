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

        builder.Property(i => i.ReorderQuantity).HasPrecision(18, 2);
        builder.Property(i => i.PurchaseFee).HasPrecision(18, 2);
        builder.Property(i => i.SaleFee).HasPrecision(18, 2);
        builder.Property(i => i.Weight).HasPrecision(18, 3);

        builder.HasMany(i => i.Batches).WithOne(b => b.InventoryItem!)
            .HasForeignKey(b => b.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Movements).WithOne(m => m.InventoryItem!)
            .HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.ItemSuppliers).WithOne(l => l.InventoryItem!)
            .HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.HasIndex(s => s.Name);

        builder.HasMany(s => s.ItemLinks).WithOne(l => l.Supplier!)
            .HasForeignKey(l => l.SupplierId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ItemSupplierConfiguration : IEntityTypeConfiguration<ItemSupplier>
{
    public void Configure(EntityTypeBuilder<ItemSupplier> builder)
    {
        builder.Property(l => l.LastUnitCost).HasPrecision(18, 2);
        builder.HasIndex(l => new { l.InventoryItemId, l.SupplierId }).IsUnique();
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
        builder.Property(m => m.UnitCost).HasPrecision(18, 2);

        builder.HasOne(m => m.InventoryBatch).WithMany()
            .HasForeignKey(m => m.InventoryBatchId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.AppointmentType).WithMany()
            .HasForeignKey(m => m.AppointmentTypeId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Appointment).WithMany()
            .HasForeignKey(m => m.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.Supplier).WithMany()
            .HasForeignKey(m => m.SupplierId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.RecordedByUser).WithMany()
            .HasForeignKey(m => m.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
