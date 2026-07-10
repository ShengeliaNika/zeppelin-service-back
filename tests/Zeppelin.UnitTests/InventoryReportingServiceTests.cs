using Microsoft.EntityFrameworkCore;
using Xunit;
using Zeppelin.Entities.Inventory;
using Zeppelin.Enums;
using Zeppelin;
using Zeppelin.Services;

namespace Zeppelin.UnitTests;

public class InventoryReportingServiceTests
{
    private static ZeppelinDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeppelinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeppelinDbContext(options);
    }

    private static InventoryItem MakeItem(string name, decimal currentStock, decimal parLevel) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Category = InventoryCategory.Consumable,
        Unit = "box",
        CurrentStock = currentStock,
        ParLevel = parLevel,
        IsActive = true,
    };

    [Fact]
    public async Task PurchaseListGroupsLowStockItemUnderNoSupplierWhenUnlinked()
    {
        await using var db = CreateContext();
        var item = MakeItem("Gloves", currentStock: 2, parLevel: 10);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var reporting = new InventoryReportingService(db, new InventoryService(db));
        var groups = await reporting.GetPurchaseListAsync(expiringWithinDays: 30);

        var group = Assert.Single(groups);
        Assert.Null(group.SupplierId);
        Assert.Equal("No supplier assigned", group.SupplierName);
        var line = Assert.Single(group.Lines);
        Assert.Equal(PurchaseListReason.LowStock, line.Reason);
        Assert.Equal(8, line.SuggestedQuantity);
    }

    [Fact]
    public async Task PurchaseListGroupsByPreferredSupplierAndMarksBothReason()
    {
        await using var db = CreateContext();
        var item = MakeItem("Anesthetic Cartridges", currentStock: 1, parLevel: 20);
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Acme Dental Supply" };
        item.ItemSuppliers.Add(new ItemSupplier { Id = Guid.NewGuid(), InventoryItemId = item.Id, SupplierId = supplier.Id, IsPreferred = true });
        var expiringBatch = new InventoryBatch
        {
            Id = Guid.NewGuid(),
            InventoryItemId = item.Id,
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            QuantityRemaining = 1,
        };
        item.Batches.Add(expiringBatch);
        db.InventoryItems.Add(item);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var reporting = new InventoryReportingService(db, new InventoryService(db));
        var groups = await reporting.GetPurchaseListAsync(expiringWithinDays: 30);

        var group = Assert.Single(groups);
        Assert.Equal(supplier.Id, group.SupplierId);
        var line = Assert.Single(group.Lines);
        Assert.Equal(PurchaseListReason.Both, line.Reason);
    }

    [Fact]
    public async Task UsageCostReportReturnsEmptyWhenNoMovementsInRange()
    {
        await using var db = CreateContext();
        var reporting = new InventoryReportingService(db, new InventoryService(db));

        var report = await reporting.GetUsageCostReportAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), DateOnly.FromDateTime(DateTime.UtcNow), category: null);

        Assert.Equal(0, report.TotalUsageCost);
        Assert.Empty(report.TopUsedItems);
    }

    [Fact]
    public async Task UsageCostReportComputesCostFromLastKnownSupplierCostAndWastePercentage()
    {
        await using var db = CreateContext();
        var item = MakeItem("Composite Resin", currentStock: 10, parLevel: 5);
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Acme Dental Supply" };
        item.ItemSuppliers.Add(new ItemSupplier { Id = Guid.NewGuid(), InventoryItemId = item.Id, SupplierId = supplier.Id, LastUnitCost = 10m });
        db.InventoryItems.Add(item);
        db.Suppliers.Add(supplier);
        db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            InventoryItemId = item.Id,
            Type = StockMovementType.UsageDeduction,
            Quantity = 3,
            RecordedByUserId = Guid.NewGuid(),
            RecordedAtUtc = DateTime.UtcNow,
        });
        db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            InventoryItemId = item.Id,
            Type = StockMovementType.Waste,
            Quantity = 1,
            RecordedByUserId = Guid.NewGuid(),
            RecordedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var reporting = new InventoryReportingService(db, new InventoryService(db));
        var report = await reporting.GetUsageCostReportAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), category: null);

        Assert.Equal(30m, report.TotalUsageCost);
        Assert.Equal(10m, report.TotalWasteCost);
        var wasteStat = Assert.Single(report.WasteStats);
        Assert.Equal(25.0m, wasteStat.WastePercentage);
    }
}
