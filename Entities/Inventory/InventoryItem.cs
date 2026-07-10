using Zeppelin.Enums;

namespace Zeppelin.Entities.Inventory;

public class InventoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public InventoryCategory Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Notes { get; set; }

    // Denormalized running total, kept in sync transactionally with every
    // StockMovement insert so reads don't need to aggregate movements.
    public decimal CurrentStock { get; set; }
    public decimal ParLevel { get; set; }

    // Optional override for the purchase-list suggested quantity; falls back
    // to (ParLevel - CurrentStock) when unset.
    public decimal? ReorderQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // Default/fallback unit cost used for valuation until real Restock cost
    // history exists (see InventoryService.GetAverageCostsAsync).
    public decimal? PurchaseFee { get; set; }
    public decimal? SaleFee { get; set; }
    public bool IsForSale { get; set; }
    public InventorySaleType? SaleType { get; set; }

    public string? Package { get; set; }
    public string? Dimensions { get; set; }
    public decimal? Weight { get; set; }

    public List<InventoryBatch> Batches { get; set; } = [];
    public List<StockMovement> Movements { get; set; } = [];
    public List<ItemSupplier> ItemSuppliers { get; set; } = [];
}
