using Microsoft.EntityFrameworkCore;
using Zeppelin.Domain.Entities.Inventory;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Infrastructure.Services;

public record PurchaseListLine(InventoryItem Item, decimal SuggestedQuantity, PurchaseListReason Reason);

public enum PurchaseListReason
{
    LowStock,
    ExpiringSoon,
    Both,
}

public record PurchaseListSupplierGroup(Guid? SupplierId, string SupplierName, IReadOnlyList<PurchaseListLine> Lines);

public record CategoryUsage(InventoryCategory Category, decimal UsageQuantity, decimal WasteQuantity, decimal EstimatedCost);

public record ItemUsage(InventoryItem Item, decimal UsageQuantity, decimal EstimatedCost);

public record WasteStat(InventoryItem Item, decimal WasteQuantity, decimal WastePercentage);

public record MonthlyCostPoint(int Year, int Month, decimal EstimatedCost);

public record UsageCostReport(
    decimal TotalUsageCost,
    decimal TotalWasteCost,
    IReadOnlyList<CategoryUsage> CategoryUsage,
    IReadOnlyList<ItemUsage> TopUsedItems,
    IReadOnlyList<WasteStat> WasteStats,
    IReadOnlyList<MonthlyCostPoint> CostOverTime);

// Read-only reporting/aggregation, kept separate from InventoryService (which
// owns stock mutation) since these are just derived views over the same data.
public class InventoryReportingService(ZeppelinDbContext db, InventoryService inventoryService)
{
    public async Task<IReadOnlyList<PurchaseListSupplierGroup>> GetPurchaseListAsync(int expiringWithinDays)
    {
        var lowStockItems = await inventoryService.GetLowStockItemsAsync();
        var expiringBatches = await inventoryService.GetExpiringSoonBatchesAsync(expiringWithinDays);

        var lowStockIds = lowStockItems.Select(i => i.Id).ToHashSet();
        var expiringIds = expiringBatches.Select(b => b.InventoryItemId).ToHashSet();

        var itemsById = lowStockItems.ToDictionary(i => i.Id);
        foreach (var batch in expiringBatches)
        {
            itemsById.TryAdd(batch.InventoryItemId, batch.InventoryItem!);
        }

        var lines = itemsById.Values.Select(item =>
        {
            var reason = (lowStockIds.Contains(item.Id), expiringIds.Contains(item.Id)) switch
            {
                (true, true) => PurchaseListReason.Both,
                (true, false) => PurchaseListReason.LowStock,
                _ => PurchaseListReason.ExpiringSoon,
            };
            var suggested = Math.Max(Math.Max(item.ParLevel - item.CurrentStock, 0), item.ReorderQuantity ?? 0);
            return new PurchaseListLine(item, suggested, reason);
        });

        return lines
            .GroupBy(l =>
            {
                var link = l.Item.ItemSuppliers.FirstOrDefault(x => x.IsPreferred) ?? l.Item.ItemSuppliers.FirstOrDefault();
                return (SupplierId: link?.SupplierId, SupplierName: link?.Supplier?.Name ?? "No supplier assigned");
            })
            .Select(g => new PurchaseListSupplierGroup(g.Key.SupplierId, g.Key.SupplierName, g.OrderBy(l => l.Item.Name).ToList()))
            .OrderBy(g => g.SupplierName)
            .ToList();
    }

    public async Task<UsageCostReport> GetUsageCostReportAsync(DateOnly from, DateOnly to, InventoryCategory? category)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var movementsQuery = db.StockMovements
            .Include(m => m.InventoryItem)
            .Where(m => m.RecordedAtUtc >= fromUtc && m.RecordedAtUtc <= toUtc
                && (m.Type == StockMovementType.UsageDeduction || m.Type == StockMovementType.Waste));

        if (category is not null)
        {
            movementsQuery = movementsQuery.Where(m => m.InventoryItem!.Category == category);
        }

        var movements = await movementsQuery.ToListAsync();
        if (movements.Count == 0)
        {
            return new UsageCostReport(0, 0, [], [], [], []);
        }

        var itemIds = movements.Select(m => m.InventoryItemId).Distinct().ToList();
        var costByItemId = await db.ItemSuppliers
            .Where(l => itemIds.Contains(l.InventoryItemId) && l.LastUnitCost != null)
            .GroupBy(l => l.InventoryItemId)
            .Select(g => new { InventoryItemId = g.Key, Cost = g.OrderByDescending(l => l.IsPreferred).First().LastUnitCost!.Value })
            .ToDictionaryAsync(x => x.InventoryItemId, x => x.Cost);

        decimal CostOf(StockMovement m) => m.Quantity * costByItemId.GetValueOrDefault(m.InventoryItemId, 0);

        var usageMovements = movements.Where(m => m.Type == StockMovementType.UsageDeduction).ToList();
        var wasteMovements = movements.Where(m => m.Type == StockMovementType.Waste).ToList();

        var categoryUsage = movements
            .GroupBy(m => m.InventoryItem!.Category)
            .Select(g => new CategoryUsage(
                g.Key,
                g.Where(m => m.Type == StockMovementType.UsageDeduction).Sum(m => m.Quantity),
                g.Where(m => m.Type == StockMovementType.Waste).Sum(m => m.Quantity),
                g.Sum(CostOf)))
            .OrderByDescending(c => c.EstimatedCost)
            .ToList();

        var topUsedItems = usageMovements
            .GroupBy(m => m.InventoryItem!)
            .Select(g => new ItemUsage(g.Key, g.Sum(m => m.Quantity), g.Sum(CostOf)))
            .OrderByDescending(i => i.UsageQuantity)
            .Take(10)
            .ToList();

        var wasteStats = wasteMovements
            .GroupBy(m => m.InventoryItem!)
            .Select(g =>
            {
                var wasteQty = g.Sum(m => m.Quantity);
                var usageQty = usageMovements.Where(m => m.InventoryItemId == g.Key.Id).Sum(m => m.Quantity);
                var total = wasteQty + usageQty;
                return new WasteStat(g.Key, wasteQty, total == 0 ? 0 : Math.Round(wasteQty / total * 100, 1));
            })
            .OrderByDescending(w => w.WasteQuantity)
            .ToList();

        var costOverTime = movements
            .GroupBy(m => (m.RecordedAtUtc.Year, m.RecordedAtUtc.Month))
            .Select(g => new MonthlyCostPoint(g.Key.Year, g.Key.Month, g.Sum(CostOf)))
            .OrderBy(p => p.Year).ThenBy(p => p.Month)
            .ToList();

        return new UsageCostReport(
            usageMovements.Sum(CostOf),
            wasteMovements.Sum(CostOf),
            categoryUsage,
            topUsedItems,
            wasteStats,
            costOverTime);
    }
}
