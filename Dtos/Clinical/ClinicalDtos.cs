using Zeppelin.Enums;

namespace Zeppelin.Dtos.Clinical;

public record ToothRecordDto(Guid Id, int ToothNumber, ToothStatus Status, string? Surface, string? Notes, DateTime RecordedAtUtc, string RecordedByName);

public record UpsertToothRecordRequest(int ToothNumber, ToothStatus Status, string? Surface, string? Notes);

public record TreatmentPlanItemDto(
    Guid Id,
    string Description,
    int? ToothNumber,
    TreatmentPlanItemStatus Status,
    Guid? AppointmentId,
    decimal? EstimatedCost,
    int SortOrder);

public record TreatmentPlanDto(
    Guid Id,
    string Title,
    TreatmentPlanStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<TreatmentPlanItemDto> Items);

public record CreateTreatmentPlanItemRequest(string Description, int? ToothNumber, decimal? EstimatedCost);

public record CreateTreatmentPlanRequest(string Title, IReadOnlyList<CreateTreatmentPlanItemRequest> Items);

public record UpdateTreatmentPlanStatusRequest(TreatmentPlanStatus Status);

public record UpdateTreatmentPlanItemRequest(TreatmentPlanItemStatus Status, Guid? AppointmentId);

public record VisitNoteDto(Guid Id, Guid AppointmentId, Guid PatientId, string AuthoredByName, string NoteText, string? ProceduresPerformed, DateTime CreatedAtUtc);

public record CreateVisitNoteRequest(string NoteText, string? ProceduresPerformed);

public record AttachmentDto(Guid Id, AttachmentType Type, string FileName, string ContentType, long SizeBytes, DateTime UploadedAtUtc, string UploadedByName);
