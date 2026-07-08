using Zeppelin.Domain.Enums;

namespace Zeppelin.Api.Dtos.Inventory;

public record InventoryBatchDto(Guid Id, string? LotNumber, DateOnly? ExpiryDate, decimal QuantityRemaining);

public record ItemSupplierLinkDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    decimal? LastUnitCost,
    string? SupplierSku,
    bool IsPreferred);

public record InventoryItemDto(
    Guid Id,
    string Name,
    InventoryCategory Category,
    string Unit,
    string? Sku,
    string? Notes,
    decimal CurrentStock,
    decimal ParLevel,
    decimal? ReorderQuantity,
    bool IsActive,
    bool IsForSale,
    decimal? PurchaseFee,
    decimal? SaleFee,
    InventorySaleType? SaleType,
    string? Package,
    string? Dimensions,
    decimal? Weight,
    decimal? AverageCost,
    decimal? Margin,
    decimal Valuation,
    IReadOnlyList<InventoryBatchDto> Batches,
    IReadOnlyList<ItemSupplierLinkDto> Suppliers);

public record CreateInventoryItemRequest(
    string Name,
    InventoryCategory Category,
    string Unit,
    string? Sku,
    string? Notes,
    decimal ParLevel,
    decimal? ReorderQuantity,
    bool IsForSale,
    decimal? PurchaseFee,
    decimal? SaleFee,
    InventorySaleType? SaleType,
    string? Package,
    string? Dimensions,
    decimal? Weight);

public record UpdateInventoryItemRequest(
    string Name,
    InventoryCategory Category,
    string Unit,
    string? Sku,
    string? Notes,
    decimal ParLevel,
    decimal? ReorderQuantity,
    bool IsActive,
    bool IsForSale,
    decimal? PurchaseFee,
    decimal? SaleFee,
    InventorySaleType? SaleType,
    string? Package,
    string? Dimensions,
    decimal? Weight);

public record InventorySummaryDto(decimal TotalValuation, int LowStockCount, int ExpiringSoonCount, int NegativeMarginCount);

public record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    decimal Quantity,
    Guid? InventoryBatchId,
    Guid? AppointmentTypeId,
    Guid? SupplierId,
    string? SupplierName,
    decimal? UnitCost,
    string? Notes,
    string RecordedByName,
    DateTime RecordedAtUtc);

public record CreateStockMovementRequest(
    StockMovementType Type,
    decimal Quantity,
    string? LotNumber,
    DateOnly? ExpiryDate,
    Guid? AppointmentTypeId,
    Guid? AppointmentId,
    Guid? SupplierId,
    decimal? UnitCost,
    string? Notes);

public record InventoryAlertsDto(
    IReadOnlyList<InventoryItemDto> LowStock,
    IReadOnlyList<ExpiringBatchDto> ExpiringSoon);

public record ExpiringBatchDto(Guid InventoryItemId, string InventoryItemName, Guid BatchId, string? LotNumber, DateOnly ExpiryDate, decimal QuantityRemaining);

public record AdjustmentLogEntryDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    decimal NewQuantity,
    string? Notes,
    string RecordedByName,
    DateTime RecordedAtUtc);

public record AppointmentSupplyUsageDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    StockMovementType Type,
    decimal Quantity,
    string RecordedByName,
    DateTime RecordedAtUtc);

public record InventoryBatchWithItemDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Unit,
    string? LotNumber,
    DateOnly? ExpiryDate,
    decimal QuantityRemaining);
