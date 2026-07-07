using Zeppelin.Domain.Enums;

namespace Zeppelin.Api.Dtos.Scheduling;

public record AppointmentTypeDto(Guid Id, string Name, int DefaultDurationMinutes, string? Color, int? RecallIntervalMonths, bool IsActive);

public record CreateAppointmentTypeRequest(string Name, int DefaultDurationMinutes, string? Color, int? RecallIntervalMonths);

public record ChairDto(Guid Id, string Name, bool IsActive);

public record CreateChairRequest(string Name);

public record StaffDirectoryEntryDto(Guid Id, string FirstName, string LastName, IReadOnlyList<string> Roles);

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DentistUserId,
    string DentistName,
    Guid? ChairId,
    string? ChairName,
    Guid AppointmentTypeId,
    string AppointmentTypeName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    AppointmentStatus Status,
    string? Notes,
    string? CancelledReason);

public record CreateAppointmentRequest(
    Guid PatientId,
    Guid DentistUserId,
    Guid? ChairId,
    Guid AppointmentTypeId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Notes);

public record RescheduleAppointmentRequest(
    Guid DentistUserId,
    Guid? ChairId,
    Guid AppointmentTypeId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Notes);

public record UpdateAppointmentStatusRequest(AppointmentStatus Status, string? CancelledReason);
