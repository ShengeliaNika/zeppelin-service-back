using Zeppelin.Enums;

namespace Zeppelin.Dtos.Patients;

public record PatientSummaryDto(
    Guid Id,
    int PatientNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Phone,
    string? Email,
    bool IsActive);

public record PatientDto(
    Guid Id,
    int PatientNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Sex,
    string? Phone,
    string? Email,
    string? IdentityNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    string? InsuranceGroupNumber,
    bool IsActive,
    IReadOnlyList<MedicalHistoryEntryDto> MedicalHistory);

public record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Sex,
    string? Phone,
    string? Email,
    string? IdentityNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    string? InsuranceGroupNumber);

public record UpdatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Sex,
    string? Phone,
    string? Email,
    string? IdentityNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    string? InsuranceGroupNumber,
    bool IsActive);

public record PatientStatusCountsDto(int All, int Initial, int Archived);

public record MedicalHistoryEntryDto(
    Guid Id,
    MedicalHistoryType Type,
    string Description,
    string? Severity,
    bool IsActive,
    DateTime NotedAtUtc);

public record CreateMedicalHistoryEntryRequest(MedicalHistoryType Type, string Description, string? Severity);
