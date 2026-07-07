namespace Zeppelin.Domain.Entities.Inventory;

public class InventoryBatch
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal QuantityRemaining { get; set; }
}
