using Zeppelin.Domain.Enums;

namespace Zeppelin.Api.Dtos.Inventory;

public record InventoryBatchDto(Guid Id, string? LotNumber, DateOnly? ExpiryDate, decimal QuantityRemaining);

public record InventoryItemDto(
    Guid Id,
    string Name,
    InventoryCategory Category,
    string Unit,
    string? SupplierName,
    string? SupplierContact,
    decimal CurrentStock,
    decimal ParLevel,
    bool IsActive,
    IReadOnlyList<InventoryBatchDto> Batches);

public record CreateInventoryItemRequest(
    string Name,
    InventoryCategory Category,
    string Unit,
    string? SupplierName,
    string? SupplierContact,
    decimal ParLevel);

public record UpdateInventoryItemRequest(
    string Name,
    InventoryCategory Category,
    string Unit,
    string? SupplierName,
    string? SupplierContact,
    decimal ParLevel,
    bool IsActive);

public record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    decimal Quantity,
    Guid? InventoryBatchId,
    Guid? AppointmentTypeId,
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
    string? Notes);

public record InventoryAlertsDto(
    IReadOnlyList<InventoryItemDto> LowStock,
    IReadOnlyList<ExpiringBatchDto> ExpiringSoon);

public record ExpiringBatchDto(Guid InventoryItemId, string InventoryItemName, Guid BatchId, string? LotNumber, DateOnly ExpiryDate, decimal QuantityRemaining);
