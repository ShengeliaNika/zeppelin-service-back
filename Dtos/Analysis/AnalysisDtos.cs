namespace Zeppelin.Dtos.Analysis;

public record DailyAppointmentStatsDto(DateOnly Date, int Scheduled, int Completed, int NoShow, int Cancelled);

public record AppointmentsTrendDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyAppointmentStatsDto> Daily,
    decimal CompletionRate,
    decimal NoShowRate);

public record WeeklyPatientGrowthDto(DateOnly WeekStart, int NewPatients);

public record PatientGrowthDto(DateOnly From, DateOnly To, int TotalNewPatients, IReadOnlyList<WeeklyPatientGrowthDto> Weekly);

public record MonthlyRevenueDto(int Year, int Month, decimal EstimatedRevenue);

public record RevenueTrendDto(IReadOnlyList<MonthlyRevenueDto> Monthly);
