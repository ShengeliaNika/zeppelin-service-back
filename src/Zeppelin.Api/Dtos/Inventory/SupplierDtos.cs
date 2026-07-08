namespace Zeppelin.Api.Dtos.Inventory;

public record SupplierItemLinkDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    decimal? LastUnitCost,
    string? SupplierSku,
    bool IsPreferred);

public record SupplierDto(
    Guid Id,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsActive,
    IReadOnlyList<SupplierItemLinkDto> LinkedItems);

public record CreateSupplierRequest(string Name, string? ContactName, string? Phone, string? Email, string? Notes);

public record UpdateSupplierRequest(string Name, string? ContactName, string? Phone, string? Email, string? Notes, bool IsActive);

public record LinkItemSupplierRequest(Guid SupplierId, decimal? LastUnitCost, string? SupplierSku, bool IsPreferred);

public record UpdateItemSupplierLinkRequest(decimal? LastUnitCost, string? SupplierSku, bool IsPreferred);
