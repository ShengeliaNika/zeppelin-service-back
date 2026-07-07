using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Clinical;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/patients/{patientId:guid}/tooth-records")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class DentalChartController(ZeppelinDbContext db, ICurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ToothRecordDto>>> GetAll(Guid patientId)
    {
        var records = await db.ToothRecords
            .Include(t => t.RecordedByUser)
            .Where(t => t.PatientId == patientId)
            .OrderBy(t => t.ToothNumber)
            .ToListAsync();

        return Ok(records.Select(ToDto).ToList());
    }

    [HttpPut]
    [Authorize(Policy = Policies.ClinicalStaff)]
    public async Task<ActionResult<ToothRecordDto>> Upsert(Guid patientId, UpsertToothRecordRequest request)
    {
        var patientExists = await db.Patients.AnyAsync(p => p.Id == patientId);
        if (!patientExists)
        {
            return NotFound();
        }

        var record = await db.ToothRecords
            .Include(t => t.RecordedByUser)
            .FirstOrDefaultAsync(t => t.PatientId == patientId && t.ToothNumber == request.ToothNumber);

        if (record is null)
        {
            record = new ToothRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                ToothNumber = request.ToothNumber,
            };
            db.ToothRecords.Add(record);
        }

        record.Status = request.Status;
        record.Surface = request.Surface;
        record.Notes = request.Notes;
        record.RecordedAtUtc = DateTime.UtcNow;
        record.RecordedByUserId = currentUser.UserId!.Value;

        await db.SaveChangesAsync();
        await db.Entry(record).Reference(r => r.RecordedByUser).LoadAsync();

        return Ok(ToDto(record));
    }

    private static ToothRecordDto ToDto(ToothRecord record) => new(
        record.Id,
        record.ToothNumber,
        record.Status,
        record.Surface,
        record.Notes,
        record.RecordedAtUtc,
        record.RecordedByUser is null ? string.Empty : $"{record.RecordedByUser.FirstName} {record.RecordedByUser.LastName}");
}
