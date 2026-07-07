using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Clinical;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Domain.Enums;
using Zeppelin.Infrastructure;
using Zeppelin.Infrastructure.Auditing;
using Zeppelin.Infrastructure.Storage;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.SchedulingStaff)]
public class AttachmentsController(ZeppelinDbContext db, IFileStorage fileStorage, ICurrentUserAccessor currentUser) : ControllerBase
{
    private const long MaxUploadSizeBytes = 20 * 1024 * 1024;

    [HttpGet("api/patients/{patientId:guid}/attachments")]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetForPatient(Guid patientId)
    {
        var attachments = await db.Attachments
            .Include(a => a.UploadedByUser)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.UploadedAtUtc)
            .ToListAsync();

        return Ok(attachments.Select(ToDto).ToList());
    }

    [HttpPost("api/patients/{patientId:guid}/attachments")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid patientId, [FromForm] AttachmentType type, [FromForm] IFormFile file, [FromForm] Guid? appointmentId)
    {
        var patientExists = await db.Patients.AnyAsync(p => p.Id == patientId);
        if (!patientExists)
        {
            return NotFound();
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "File is empty." });
        }

        await using var stream = file.OpenReadStream();
        var storageKey = await fileStorage.SaveAsync(stream, file.FileName);

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            AppointmentId = appointmentId,
            Type = type,
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StorageKey = storageKey,
            UploadedByUserId = currentUser.UserId!.Value,
        };

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();
        await db.Entry(attachment).Reference(a => a.UploadedByUser).LoadAsync();

        return Ok(ToDto(attachment));
    }

    [HttpGet("api/attachments/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
        if (attachment is null)
        {
            return NotFound();
        }

        var stream = await fileStorage.OpenReadAsync(attachment.StorageKey);
        return File(stream, attachment.ContentType, attachment.FileName);
    }

    private static AttachmentDto ToDto(Attachment attachment) => new(
        attachment.Id,
        attachment.Type,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.UploadedAtUtc,
        attachment.UploadedByUser is null ? string.Empty : $"{attachment.UploadedByUser.FirstName} {attachment.UploadedByUser.LastName}");
}
