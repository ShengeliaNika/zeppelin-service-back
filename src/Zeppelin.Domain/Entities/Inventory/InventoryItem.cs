using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Inventory;

public class InventoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public InventoryCategory Category { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public string? SupplierContact { get; set; }

    // Denormalized running total, kept in sync transactionally with every
    // StockMovement insert so reads don't need to aggregate movements.
    public decimal CurrentStock { get; set; }
    public decimal ParLevel { get; set; }
    public bool IsActive { get; set; } = true;

    public List<InventoryBatch> Batches { get; set; } = [];
    public List<StockMovement> Movements { get; set; } = [];
}
