using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Identity;
using Zeppelin.Domain.Entities.Scheduling;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Domain.Entities.Patients;

public class Attachment : IAuditable
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public AttachmentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    // Opaque key resolved through IFileStorage - never a raw filesystem path,
    // so the storage backend can be swapped without touching this table.
    public string StorageKey { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
