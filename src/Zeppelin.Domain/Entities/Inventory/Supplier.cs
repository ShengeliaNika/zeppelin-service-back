namespace Zeppelin.Domain.Entities.Inventory;

public class Supplier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ItemSupplier> ItemLinks { get; set; } = [];
}
