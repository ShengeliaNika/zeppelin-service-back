using Zeppelin.Entities.Identity;
using Zeppelin.Entities.Scheduling;
using Zeppelin.Enums;

namespace Zeppelin.Entities.Inventory;

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public Guid? InventoryBatchId { get; set; }
    public InventoryBatch? InventoryBatch { get; set; }
    public StockMovementType Type { get; set; }

    // Always positive; Type determines whether it adds to or subtracts from
    // CurrentStock, which keeps reporting/aggregation unambiguous.
    public decimal Quantity { get; set; }

    public Guid? AppointmentTypeId { get; set; }
    public AppointmentType? AppointmentType { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    // Only meaningfully populated on Restock; source of truth for
    // ItemSupplier.LastUnitCost, not a financial ledger entry.
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal? UnitCost { get; set; }

    public Guid RecordedByUserId { get; set; }
    public ApplicationUser? RecordedByUser { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
