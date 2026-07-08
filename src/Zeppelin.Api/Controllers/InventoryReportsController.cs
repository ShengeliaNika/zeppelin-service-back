using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure.Services;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/inventory-reports")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class InventoryReportsController(InventoryReportingService reportingService) : ControllerBase
{
    [HttpGet("purchase-list")]
    public async Task<ActionResult<IReadOnlyList<PurchaseListSupplierGroupDto>>> GetPurchaseList([FromQuery] int expiringWithinDays = 30)
    {
        var groups = await reportingService.GetPurchaseListAsync(expiringWithinDays);
        return Ok(groups.Select(g => new PurchaseListSupplierGroupDto(
            g.SupplierId,
            g.SupplierName,
            g.Lines.Select(l => new PurchaseListLineDto(
                l.Item.Id,
                l.Item.Name,
                l.Item.Category,
                l.Item.Unit,
                l.Item.CurrentStock,
                l.Item.ParLevel,
                l.SuggestedQuantity,
                ToReasonDto(l.Reason)))
            .ToList()))
            .ToList());
    }

    [HttpGet("usage-cost")]
    public async Task<ActionResult<UsageCostReportDto>> GetUsageCostReport(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] InventoryCategory? category = null)
    {
        var report = await reportingService.GetUsageCostReportAsync(from, to, category);
        return Ok(new UsageCostReportDto(
            from,
            to,
            report.TotalUsageCost,
            report.TotalWasteCost,
            report.CategoryUsage.Select(c => new CategoryUsageDto(c.Category, c.UsageQuantity, c.WasteQuantity, c.EstimatedCost)).ToList(),
            report.TopUsedItems.Select(i => new ItemUsageDto(i.Item.Id, i.Item.Name, i.UsageQuantity, i.EstimatedCost)).ToList(),
            report.WasteStats.Select(w => new WasteStatDto(w.Item.Id, w.Item.Name, w.WasteQuantity, w.WastePercentage)).ToList(),
            report.CostOverTime.Select(p => new MonthlyCostPointDto(p.Year, p.Month, p.EstimatedCost)).ToList()));
    }

    private static Dtos.Inventory.PurchaseListReason ToReasonDto(Infrastructure.Services.PurchaseListReason reason) => reason switch
    {
        Infrastructure.Services.PurchaseListReason.LowStock => Dtos.Inventory.PurchaseListReason.LowStock,
        Infrastructure.Services.PurchaseListReason.ExpiringSoon => Dtos.Inventory.PurchaseListReason.ExpiringSoon,
        _ => Dtos.Inventory.PurchaseListReason.Both,
    };
}
