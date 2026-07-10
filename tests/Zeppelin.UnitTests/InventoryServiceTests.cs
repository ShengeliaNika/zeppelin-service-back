using Microsoft.EntityFrameworkCore;
using Xunit;
using Zeppelin.Entities.Inventory;
using Zeppelin.Enums;
using Zeppelin;
using Zeppelin.Services;

namespace Zeppelin.UnitTests;

public class InventoryServiceTests
{
    private static ZeppelinDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeppelinDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeppelinDbContext(options);
    }

    private static InventoryItem MakeItem(decimal currentStock, decimal parLevel) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Composite Resin",
        Category = InventoryCategory.Consumable,
        Unit = "box",
        CurrentStock = currentStock,
        ParLevel = parLevel,
    };

    [Fact]
    public async Task RestockIncreasesCurrentStock()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 5, parLevel: 10);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(item.Id, StockMovementType.Restock, 20, null, null, null, null, null, Guid.NewGuid());

        Assert.Equal(25, item.CurrentStock);
    }

    [Fact]
    public async Task RestockWithLotNumberCreatesBatch()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 0, parLevel: 10);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));
        await service.RecordMovementAsync(item.Id, StockMovementType.Restock, 15, "LOT-1", expiry, null, null, null, Guid.NewGuid());

        var batch = Assert.Single(db.InventoryBatches.Local);
        Assert.Equal("LOT-1", batch.LotNumber);
        Assert.Equal(15, batch.QuantityRemaining);
    }

    [Fact]
    public async Task UsageDeductionDecreasesCurrentStockAndDoesNotGoNegative()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 5, parLevel: 10);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(item.Id, StockMovementType.UsageDeduction, 8, null, null, null, null, null, Guid.NewGuid());

        Assert.Equal(0, item.CurrentStock);
    }

    [Fact]
    public async Task UsageDeductionDrawsFromEarliestExpiringBatchFirst()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 30, parLevel: 10);
        var soonBatch = new InventoryBatch { Id = Guid.NewGuid(), InventoryItemId = item.Id, ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), QuantityRemaining = 10 };
        var laterBatch = new InventoryBatch { Id = Guid.NewGuid(), InventoryItemId = item.Id, ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)), QuantityRemaining = 20 };
        item.Batches.Add(soonBatch);
        item.Batches.Add(laterBatch);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(item.Id, StockMovementType.UsageDeduction, 4, null, null, null, null, null, Guid.NewGuid());

        Assert.Equal(6, soonBatch.QuantityRemaining);
        Assert.Equal(20, laterBatch.QuantityRemaining);
    }

    [Fact]
    public async Task AdjustmentSetsCurrentStockDirectly()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 50, parLevel: 10);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(item.Id, StockMovementType.Adjustment, 12, null, null, null, null, "Physical count correction", Guid.NewGuid());

        Assert.Equal(12, item.CurrentStock);
    }

    [Fact]
    public async Task ThrowsWhenInventoryItemDoesNotExist()
    {
        await using var db = CreateContext();
        var service = new InventoryService(db);

        await Assert.ThrowsAsync<InventoryItemNotFoundException>(() =>
            service.RecordMovementAsync(Guid.NewGuid(), StockMovementType.Restock, 1, null, null, null, null, null, Guid.NewGuid()));
    }

    [Fact]
    public async Task RestockWithSupplierAndCostCreatesItemSupplierLink()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 0, parLevel: 10);
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Acme Dental Supply" };
        db.InventoryItems.Add(item);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(
            item.Id, StockMovementType.Restock, 10, null, null, null, null, null, Guid.NewGuid(),
            supplierId: supplier.Id, unitCost: 4.5m);

        var link = Assert.Single(db.ItemSuppliers.Local);
        Assert.Equal(supplier.Id, link.SupplierId);
        Assert.Equal(4.5m, link.LastUnitCost);
    }

    [Fact]
    public async Task RestockWithSupplierUpdatesExistingLinkInsteadOfDuplicating()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 0, parLevel: 10);
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Acme Dental Supply" };
        db.InventoryItems.Add(item);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        await service.RecordMovementAsync(item.Id, StockMovementType.Restock, 10, null, null, null, null, null, Guid.NewGuid(), supplier.Id, 4.5m);
        await service.RecordMovementAsync(item.Id, StockMovementType.Restock, 10, null, null, null, null, null, Guid.NewGuid(), supplier.Id, 5.0m);

        var link = Assert.Single(db.ItemSuppliers.Local);
        Assert.Equal(5.0m, link.LastUnitCost);
    }

    [Fact]
    public async Task GetAllActiveBatchesAsyncReturnsOnlyBatchesWithRemainingStockOrderedByExpiry()
    {
        await using var db = CreateContext();
        var item = MakeItem(currentStock: 30, parLevel: 10);
        var soon = new InventoryBatch { Id = Guid.NewGuid(), InventoryItemId = item.Id, ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), QuantityRemaining = 5 };
        var later = new InventoryBatch { Id = Guid.NewGuid(), InventoryItemId = item.Id, ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(50)), QuantityRemaining = 5 };
        var depleted = new InventoryBatch { Id = Guid.NewGuid(), InventoryItemId = item.Id, ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), QuantityRemaining = 0 };
        item.Batches.AddRange([soon, later, depleted]);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        var service = new InventoryService(db);
        var batches = await service.GetAllActiveBatchesAsync();

        Assert.Equal(2, batches.Count);
        Assert.Equal(soon.Id, batches[0].Id);
        Assert.Equal(later.Id, batches[1].Id);
    }
}
