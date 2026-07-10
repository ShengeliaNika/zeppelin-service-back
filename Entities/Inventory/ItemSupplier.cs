namespace Zeppelin.Entities.Inventory;

// Links an InventoryItem to a Supplier it can be bought from. LastUnitCost is
// updated opportunistically from Restock movements, not a financial ledger.
public class ItemSupplier
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal? LastUnitCost { get; set; }
    public string? SupplierSku { get; set; }
    public bool IsPreferred { get; set; }
}
