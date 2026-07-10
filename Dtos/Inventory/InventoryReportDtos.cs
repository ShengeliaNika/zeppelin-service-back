using Zeppelin.Enums;

namespace Zeppelin.Dtos.Inventory;

public enum PurchaseListReason
{
    LowStock,
    ExpiringSoon,
    Both,
}

public record PurchaseListLineDto(
    Guid InventoryItemId,
    string InventoryItemName,
    InventoryCategory Category,
    string Unit,
    decimal CurrentStock,
    decimal ParLevel,
    decimal SuggestedQuantity,
    PurchaseListReason Reason);

public record PurchaseListSupplierGroupDto(
    Guid? SupplierId,
    string SupplierName,
    IReadOnlyList<PurchaseListLineDto> Lines);

public record CategoryUsageDto(InventoryCategory Category, decimal UsageQuantity, decimal WasteQuantity, decimal EstimatedCost);

public record ItemUsageDto(Guid InventoryItemId, string InventoryItemName, decimal UsageQuantity, decimal EstimatedCost);

public record WasteStatDto(Guid InventoryItemId, string InventoryItemName, decimal WasteQuantity, decimal WastePercentage);

public record MonthlyCostPointDto(int Year, int Month, decimal EstimatedCost);

public record DoctorUsageDto(Guid DentistUserId, string DentistName, decimal UsageQuantity, decimal WasteQuantity, decimal EstimatedCost);

public record UsageCostReportDto(
    DateOnly From,
    DateOnly To,
    decimal TotalUsageCost,
    decimal TotalWasteCost,
    IReadOnlyList<CategoryUsageDto> CategoryUsage,
    IReadOnlyList<ItemUsageDto> TopUsedItems,
    IReadOnlyList<WasteStatDto> WasteStats,
    IReadOnlyList<MonthlyCostPointDto> CostOverTime,
    IReadOnlyList<DoctorUsageDto> UsageByDoctor);
