using Zeppelin.Api.Dtos.Inventory;
using Zeppelin.Domain.Enums;

namespace Zeppelin.Api.Dtos.Admin;

public record DashboardSummaryDto(
    int TodaysAppointmentsCount,
    int AppointmentsThisWeekCount,
    int ActivePatientsCount,
    int LowStockCount,
    int ExpiringSoonCount,
    int RecallDueCount,
    decimal EstimatedRevenueThisMonth,
    decimal InventoryValuation,
    int CompletedLast7DaysCount,
    int NoShowLast7DaysCount,
    int CancelledLast7DaysCount);

public record RecallReminderDto(Guid Id, Guid PatientId, string PatientName, string AppointmentTypeName, DateOnly DueDate);

public record CombinedAlertsDto(
    IReadOnlyList<InventoryItemDto> LowStock,
    IReadOnlyList<ExpiringBatchDto> ExpiringSoon,
    IReadOnlyList<RecallReminderDto> RecallDue);

public record AuditLogEntryDto(
    Guid Id,
    string UserName,
    string EntityName,
    Guid EntityId,
    AuditAction Action,
    string? ChangedFieldsJson,
    DateTime TimestampUtc);
