using Microsoft.EntityFrameworkCore;
using Zeppelin.Entities.Inventory;
using Zeppelin.Enums;

namespace Zeppelin.Services;

public class InventoryItemNotFoundException(Guid id) : Exception($"Inventory item {id} was not found.");
public class StockMovementNotFoundException(Guid id) : Exception($"Stock movement {id} was not found.");
public class StockMovementNotReversibleException(StockMovementType type)
    : Exception($"Stock movements of type {type} can't be undone this way - only UsageDeduction/Waste entries can.");

public class InventoryService(ZeppelinDbContext db)
{
    // Restock adds to CurrentStock (and opens a new batch if a lot/expiry was
    // given). UsageDeduction/Waste subtract, drawing down the
    // earliest-expiring batch with remaining stock first (simple FIFO,
    // single-batch - v1 doesn't split a deduction across batches). Adjustment
    // sets CurrentStock to the given value directly (a correction to the
    // counted total, not a delta) since a signed delta would be ambiguous
    // given Quantity is always stored positive.
    public async Task<StockMovement> RecordMovementAsync(
        Guid inventoryItemId,
        StockMovementType type,
        decimal quantity,
        string? lotNumber,
        DateOnly? expiryDate,
        Guid? appointmentTypeId,
        Guid? appointmentId,
        string? notes,
        Guid recordedByUserId,
        Guid? supplierId = null,
        decimal? unitCost = null)
    {
        var item = await db.InventoryItems
            .Include(i => i.Batches)
            .Include(i => i.ItemSuppliers)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId)
            ?? throw new InventoryItemNotFoundException(inventoryItemId);

        Guid? batchId = null;

        switch (type)
        {
            case StockMovementType.Restock:
                item.CurrentStock += quantity;
                if (!string.IsNullOrWhiteSpace(lotNumber) || expiryDate is not null)
                {
                    var batch = new InventoryBatch
                    {
                        Id = Guid.NewGuid(),
                        InventoryItemId = item.Id,
                        LotNumber = lotNumber,
                        ExpiryDate = expiryDate,
                        QuantityRemaining = quantity,
                    };
                    db.InventoryBatches.Add(batch);
                    batchId = batch.Id;
                }
                if (supplierId is not null)
                {
                    var link = item.ItemSuppliers.FirstOrDefault(l => l.SupplierId == supplierId);
                    if (link is null)
                    {
                        link = new ItemSupplier { Id = Guid.NewGuid(), InventoryItemId = item.Id, SupplierId = supplierId.Value };
                        db.ItemSuppliers.Add(link);
                        item.ItemSuppliers.Add(link);
                    }
                    if (unitCost is not null)
                    {
                        link.LastUnitCost = unitCost;
                    }
                }
                break;

            case StockMovementType.UsageDeduction:
            case StockMovementType.Waste:
                item.CurrentStock = Math.Max(0, item.CurrentStock - quantity);
                var deductFrom = item.Batches
                    .Where(b => b.QuantityRemaining > 0)
                    .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
                    .FirstOrDefault();
                if (deductFrom is not null)
                {
                    deductFrom.QuantityRemaining = Math.Max(0, deductFrom.QuantityRemaining - quantity);
                    batchId = deductFrom.Id;
                }
                break;

            case StockMovementType.Adjustment:
                item.CurrentStock = quantity;
                break;
        }

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            InventoryBatchId = batchId,
            Type = type,
            Quantity = quantity,
            AppointmentTypeId = appointmentTypeId,
            AppointmentId = appointmentId,
            SupplierId = type == StockMovementType.Restock ? supplierId : null,
            UnitCost = type == StockMovementType.Restock ? unitCost : null,
            RecordedByUserId = recordedByUserId,
            Notes = notes,
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        return movement;
    }

    // Undoes a mistaken "supplies used" log entry (e.g. wrong item/quantity):
    // restores CurrentStock and, if the deduction was drawn from a specific
    // batch, restores that exact batch's QuantityRemaining too - then removes
    // the movement row. Scoped to UsageDeduction/Waste only, since Restock and
    // Adjustment have different (and here, out of scope) reversal semantics -
    // this exists for the doctor-facing "log supplies used" flow, not as a
    // general-purpose stock-movement undo.
    public async Task ReverseUsageMovementAsync(Guid itemId, Guid movementId)
    {
        var movement = await db.StockMovements
            .Include(m => m.InventoryItem)
            .Include(m => m.InventoryBatch)
            .FirstOrDefaultAsync(m => m.Id == movementId && m.InventoryItemId == itemId)
            ?? throw new StockMovementNotFoundException(movementId);

        if (movement.Type != StockMovementType.UsageDeduction && movement.Type != StockMovementType.Waste)
        {
            throw new StockMovementNotReversibleException(movement.Type);
        }

        movement.InventoryItem!.CurrentStock += movement.Quantity;
        if (movement.InventoryBatch is not null)
        {
            movement.InventoryBatch.QuantityRemaining += movement.Quantity;
        }

        db.StockMovements.Remove(movement);
        await db.SaveChangesAsync();
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await db.InventoryItems
            .Include(i => i.Batches)
            .Include(i => i.ItemSuppliers).ThenInclude(l => l.Supplier)
            .Where(i => i.IsActive && i.CurrentStock <= i.ParLevel)
            .ToListAsync();
    }

    public async Task<List<InventoryBatch>> GetExpiringSoonBatchesAsync(int withinDays)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(withinDays);
        return await db.InventoryBatches
            .Include(b => b.InventoryItem)
            .Where(b => b.InventoryItem!.IsActive && b.QuantityRemaining > 0 && b.ExpiryDate != null && b.ExpiryDate <= cutoff)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();
    }

    public async Task<List<InventoryBatch>> GetAllActiveBatchesAsync()
    {
        return await db.InventoryBatches
            .Include(b => b.InventoryItem)
            .Where(b => b.InventoryItem!.IsActive && b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
            .ToListAsync();
    }

    // Weighted average of UnitCost across an item's Restock history - the
    // real valuation basis. Falls back to InventoryItem.PurchaseFee (set by
    // the caller) for items with no cost-bearing restock yet.
    public async Task<Dictionary<Guid, decimal>> GetAverageCostsAsync(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var costed = await db.StockMovements
            .Where(m => ids.Contains(m.InventoryItemId) && m.Type == StockMovementType.Restock && m.UnitCost != null)
            .GroupBy(m => m.InventoryItemId)
            .Select(g => new { InventoryItemId = g.Key, TotalCost = g.Sum(m => m.Quantity * m.UnitCost!.Value), TotalQty = g.Sum(m => m.Quantity) })
            .ToListAsync();

        return costed
            .Where(x => x.TotalQty > 0)
            .ToDictionary(x => x.InventoryItemId, x => x.TotalCost / x.TotalQty);
    }

    public async Task<InventorySummary> GetSummaryAsync()
    {
        var items = await db.InventoryItems.Include(i => i.Batches).Where(i => i.IsActive).ToListAsync();
        var avgCosts = await GetAverageCostsAsync(items.Select(i => i.Id));

        decimal totalValuation = 0;
        var negativeMarginCount = 0;
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        foreach (var item in items)
        {
            var cost = avgCosts.GetValueOrDefault(item.Id, item.PurchaseFee ?? 0);
            totalValuation += item.CurrentStock * cost;
            if (item.IsForSale && item.SaleFee is not null && item.SaleFee.Value - cost < 0)
            {
                negativeMarginCount++;
            }
        }

        var lowStockCount = items.Count(i => i.CurrentStock <= i.ParLevel);
        var expiringSoonCount = items.Count(i => i.Batches.Any(b => b.QuantityRemaining > 0 && b.ExpiryDate != null && b.ExpiryDate <= cutoff));

        return new InventorySummary(totalValuation, lowStockCount, expiringSoonCount, negativeMarginCount);
    }
}

public record InventorySummary(decimal TotalValuation, int LowStockCount, int ExpiringSoonCount, int NegativeMarginCount);
