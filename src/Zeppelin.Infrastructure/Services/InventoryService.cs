using Microsoft.EntityFrameworkCore;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Infrastructure.Services;

public class InventoryItemNotFoundException(Guid id) : Exception($"Inventory item {id} was not found.");

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
        Guid recordedByUserId)
    {
        var item = await db.InventoryItems.Include(i => i.Batches).FirstOrDefaultAsync(i => i.Id == inventoryItemId)
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
            RecordedByUserId = recordedByUserId,
            Notes = notes,
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync();

        return movement;
    }

    public async Task<List<InventoryItem>> GetLowStockItemsAsync()
    {
        return await db.InventoryItems
            .Include(i => i.Batches)
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
}
