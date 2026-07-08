using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Clinical;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.SchedulingStaff)]
public class TreatmentPlansController(ZeppelinDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet("api/patients/{patientId:guid}/treatment-plans")]
    public async Task<ActionResult<IReadOnlyList<TreatmentPlanDto>>> GetForPatient(Guid patientId)
    {
        var plans = await db.TreatmentPlans
            .Include(p => p.Items)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        return Ok(plans.Select(ToDto).ToList());
    }

    [HttpPost("api/patients/{patientId:guid}/treatment-plans")]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<TreatmentPlanDto>> Create(Guid patientId, CreateTreatmentPlanRequest request)
    {
        var patientExists = await db.Patients.AnyAsync(p => p.Id == patientId);
        if (!patientExists)
        {
            return NotFound();
        }

        var plan = new TreatmentPlan
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Title = request.Title,
            CreatedByUserId = currentUser.UserId!.Value,
        };

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            plan.Items.Add(new TreatmentPlanItem
            {
                Id = Guid.NewGuid(),
                Description = item.Description,
                ToothNumber = item.ToothNumber,
                EstimatedCost = item.EstimatedCost,
                SortOrder = i,
            });
        }

        db.TreatmentPlans.Add(plan);
        await db.SaveChangesAsync();

        return Ok(ToDto(plan));
    }

    [HttpPut("api/treatment-plans/{id:guid}/status")]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<TreatmentPlanDto>> UpdateStatus(Guid id, UpdateTreatmentPlanStatusRequest request)
    {
        var plan = await db.TreatmentPlans.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == id);
        if (plan is null)
        {
            return NotFound();
        }

        plan.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok(ToDto(plan));
    }

    [HttpPost("api/treatment-plans/{planId:guid}/items")]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<TreatmentPlanDto>> AddItem(Guid planId, CreateTreatmentPlanItemRequest request)
    {
        var plan = await db.TreatmentPlans.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == planId);
        if (plan is null)
        {
            return NotFound();
        }

        plan.Items.Add(new TreatmentPlanItem
        {
            Id = Guid.NewGuid(),
            Description = request.Description,
            ToothNumber = request.ToothNumber,
            EstimatedCost = request.EstimatedCost,
            SortOrder = plan.Items.Count,
        });

        await db.SaveChangesAsync();
        return Ok(ToDto(plan));
    }

    [HttpPut("api/treatment-plan-items/{itemId:guid}")]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<TreatmentPlanItemDto>> UpdateItem(Guid itemId, UpdateTreatmentPlanItemRequest request)
    {
        var item = await db.TreatmentPlanItems.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item is null)
        {
            return NotFound();
        }

        item.Status = request.Status;
        item.AppointmentId = request.AppointmentId;
        item.CompletedAtUtc = request.Status == TreatmentPlanItemStatus.Done ? DateTime.UtcNow : null;

        await db.SaveChangesAsync();
        return Ok(new TreatmentPlanItemDto(item.Id, item.Description, item.ToothNumber, item.Status, item.AppointmentId, item.EstimatedCost, item.SortOrder));
    }

    private static TreatmentPlanDto ToDto(TreatmentPlan plan) => new(
        plan.Id,
        plan.Title,
        plan.Status,
        plan.CreatedAtUtc,
        plan.Items
            .OrderBy(i => i.SortOrder)
            .Select(i => new TreatmentPlanItemDto(i.Id, i.Description, i.ToothNumber, i.Status, i.AppointmentId, i.EstimatedCost, i.SortOrder))
            .ToList());
}
