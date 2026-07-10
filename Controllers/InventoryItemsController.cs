using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Dtos.Common;
using Zeppelin.Dtos.Inventory;
using Zeppelin.Common;
using Zeppelin.Entities.Inventory;
using Zeppelin.Enums;
using Zeppelin;
using Zeppelin.Services;

namespace Zeppelin.Controllers;

[ApiController]
[Route("api/inventory-items")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class InventoryItemsController(ZeppelinDbContext db, InventoryService inventoryService) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<InventoryItemDto>>> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        [FromQuery] InventoryCategory? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string? quickFilter = null)
    {
        take = Math.Clamp(take, 1, MaxPageSize);
        skip = Math.Max(0, skip);

        var query = db.InventoryItems
            .Include(i => i.Batches)
            .Include(i => i.ItemSuppliers).ThenInclude(l => l.Supplier)
            .Where(i => i.IsActive);

        if (category is not null)
        {
            query = query.Where(i => i.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{search}%") || (i.Sku != null && EF.Functions.ILike(i.Sku, $"%{search}%")));
        }
        if (string.Equals(quickFilter, "lowStock", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(i => i.CurrentStock <= i.ParLevel);
        }
        else if (string.Equals(quickFilter, "nearExpiry", StringComparison.OrdinalIgnoreCase))
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
            query = query.Where(i => i.Batches.Any(b => b.QuantityRemaining > 0 && b.ExpiryDate != null && b.ExpiryDate <= cutoff));
        }

        // Negative margin needs restock-cost history to compute, which can't
        // be expressed as a SQL predicate here - filter in-memory instead.
        // Clinic catalogs are small (dozens-hundreds of items), so loading
        // the filtered set unpaginated is cheap.
        if (string.Equals(quickFilter, "negativeMargin", StringComparison.OrdinalIgnoreCase))
        {
            var candidates = await query.OrderBy(i => i.Name).ToListAsync();
            var candidateCosts = await inventoryService.GetAverageCostsAsync(candidates.Select(i => i.Id));
            var negativeMarginItems = candidates
                .Where(i => i.IsForSale && i.SaleFee is not null && i.SaleFee.Value - candidateCosts.GetValueOrDefault(i.Id, i.PurchaseFee ?? 0) < 0)
                .ToList();

            var page = negativeMarginItems.Skip(skip).Take(take).ToList();
            return Ok(new PagedResultDto<InventoryItemDto>(
                page.Select(i => ToDto(i, candidateCosts)).ToList(), negativeMarginItems.Count, skip, take));
        }

        var ordered = query.OrderBy(i => i.Name);
        var totalCount = await ordered.CountAsync();
        var items = await ordered.Skip(skip).Take(take).ToListAsync();
        var avgCosts = await inventoryService.GetAverageCostsAsync(items.Select(i => i.Id));

        return Ok(new PagedResultDto<InventoryItemDto>(items.Select(i => ToDto(i, avgCosts)).ToList(), totalCount, skip, take));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id)
    {
        var item = await db.InventoryItems
            .Include(i => i.Batches)
            .Include(i => i.ItemSuppliers).ThenInclude(l => l.Supplier)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        var avgCosts = await inventoryService.GetAverageCostsAsync([item.Id]);
        return Ok(ToDto(item, avgCosts));
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyList<InventoryBatchWithItemDto>>> GetAllBatches()
    {
        var batches = await inventoryService.GetAllActiveBatchesAsync();
        return Ok(batches.Select(b => new InventoryBatchWithItemDto(
            b.Id, b.InventoryItemId, b.InventoryItem!.Name, b.InventoryItem.Unit, b.LotNumber, b.ExpiryDate, b.QuantityRemaining)).ToList());
    }

    [HttpGet("adjustments")]
    public async Task<ActionResult<IReadOnlyList<AdjustmentLogEntryDto>>> GetAdjustments([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, MaxPageSize);

        var adjustments = await db.StockMovements
            .Include(m => m.InventoryItem)
            .Include(m => m.RecordedByUser)
            .Where(m => m.Type == StockMovementType.Adjustment)
            .OrderByDescending(m => m.RecordedAtUtc)
            .Take(take)
            .ToListAsync();

        return Ok(adjustments.Select(m => new AdjustmentLogEntryDto(
            m.Id,
            m.InventoryItemId,
            m.InventoryItem!.Name,
            m.InventoryItem.Unit,
            m.Quantity,
            m.Notes,
            m.RecordedByUser is null ? string.Empty : $"{m.RecordedByUser.FirstName} {m.RecordedByUser.LastName}",
            m.RecordedAtUtc)).ToList());
    }

    [HttpGet("summary")]
    public async Task<ActionResult<InventorySummaryDto>> GetSummary()
    {
        var summary = await inventoryService.GetSummaryAsync();
        return Ok(new InventorySummaryDto(summary.TotalValuation, summary.LowStockCount, summary.ExpiringSoonCount, summary.NegativeMarginCount));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<InventoryAlertsDto>> GetAlerts([FromQuery] int expiringWithinDays = 30)
    {
        var lowStockItems = await inventoryService.GetLowStockItemsAsync();
        var avgCosts = await inventoryService.GetAverageCostsAsync(lowStockItems.Select(i => i.Id));
        var lowStock = lowStockItems.Select(i => ToDto(i, avgCosts)).ToList();
        var expiringSoon = (await inventoryService.GetExpiringSoonBatchesAsync(expiringWithinDays))
            .Select(b => new ExpiringBatchDto(b.InventoryItemId, b.InventoryItem!.Name, b.Id, b.LotNumber, b.ExpiryDate!.Value, b.QuantityRemaining))
            .ToList();

        return Ok(new InventoryAlertsDto(lowStock, expiringSoon));
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<InventoryItemDto>> Create(CreateInventoryItemRequest request)
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Category = request.Category,
            Unit = request.Unit,
            Sku = request.Sku,
            Notes = request.Notes,
            ParLevel = request.ParLevel,
            ReorderQuantity = request.ReorderQuantity,
            IsForSale = request.IsForSale,
            PurchaseFee = request.PurchaseFee,
            SaleFee = request.SaleFee,
            SaleType = request.SaleType,
            Package = request.Package,
            Dimensions = request.Dimensions,
            Weight = request.Weight,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ToDto(item, new Dictionary<Guid, decimal>()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, UpdateInventoryItemRequest request)
    {
        var item = await db.InventoryItems
            .Include(i => i.Batches)
            .Include(i => i.ItemSuppliers).ThenInclude(l => l.Supplier)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.Name = request.Name;
        item.Category = request.Category;
        item.Unit = request.Unit;
        item.Sku = request.Sku;
        item.Notes = request.Notes;
        item.ParLevel = request.ParLevel;
        item.ReorderQuantity = request.ReorderQuantity;
        item.IsActive = request.IsActive;
        item.IsForSale = request.IsForSale;
        item.PurchaseFee = request.PurchaseFee;
        item.SaleFee = request.SaleFee;
        item.SaleType = request.SaleType;
        item.Package = request.Package;
        item.Dimensions = request.Dimensions;
        item.Weight = request.Weight;

        await db.SaveChangesAsync();
        var avgCosts = await inventoryService.GetAverageCostsAsync([item.Id]);
        return Ok(ToDto(item, avgCosts));
    }

    internal static InventoryItemDto ToDto(InventoryItem item, IReadOnlyDictionary<Guid, decimal> avgCosts)
    {
        var averageCost = avgCosts.TryGetValue(item.Id, out var cost) ? cost : item.PurchaseFee;
        var valuationCost = averageCost ?? 0;
        var margin = item.IsForSale && item.SaleFee is not null ? item.SaleFee.Value - (averageCost ?? item.PurchaseFee ?? 0) : (decimal?)null;

        return new(
            item.Id,
            item.Name,
            item.Category,
            item.Unit,
            item.Sku,
            item.Notes,
            item.CurrentStock,
            item.ParLevel,
            item.ReorderQuantity,
            item.IsActive,
            item.IsForSale,
            item.PurchaseFee,
            item.SaleFee,
            item.SaleType,
            item.Package,
            item.Dimensions,
            item.Weight,
            averageCost,
            margin,
            item.CurrentStock * valuationCost,
            item.Batches
                .Where(b => b.QuantityRemaining > 0)
                .OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue)
                .Select(b => new InventoryBatchDto(b.Id, b.LotNumber, b.ExpiryDate, b.QuantityRemaining))
                .ToList(),
            item.ItemSuppliers
                .Select(l => new ItemSupplierLinkDto(l.Id, l.SupplierId, l.Supplier!.Name, l.LastUnitCost, l.SupplierSku, l.IsPreferred))
                .ToList());
    }
}
