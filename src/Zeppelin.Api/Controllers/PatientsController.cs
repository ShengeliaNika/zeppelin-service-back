using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Common;
using Zeppelin.Api.Dtos.Patients;
using Zeppelin.Domain.Common;
using Zeppelin.Domain.Entities.Patients;
using Zeppelin.Infrastructure;

namespace Zeppelin.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize(Policy = Policies.SchedulingStaff)]
public class PatientsController(ZeppelinDbContext db) : ControllerBase
{
    private const int MaxPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PatientSummaryDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] int skip = 0, [FromQuery] int take = 25)
    {
        take = Math.Clamp(take, 1, MaxPageSize);
        skip = Math.Max(0, skip);

        var query = db.Patients.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(term) ||
                p.LastName.ToLower().Contains(term) ||
                (p.Phone != null && p.Phone.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip(skip)
            .Take(take)
            .Select(p => new PatientSummaryDto(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Phone, p.IsActive))
            .ToListAsync();

        return Ok(new PagedResultDto<PatientSummaryDto>(patients, totalCount, skip, take));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        var patient = await db.Patients
            .Include(p => p.MedicalHistory)
            .FirstOrDefaultAsync(p => p.Id == id);

        return patient is null ? NotFound() : Ok(ToDto(patient));
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request)
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Sex = request.Sex,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            InsuranceProvider = request.InsuranceProvider,
            InsurancePolicyNumber = request.InsurancePolicyNumber,
            InsuranceGroupNumber = request.InsuranceGroupNumber,
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, ToDto(patient));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, UpdatePatientRequest request)
    {
        var patient = await db.Patients.Include(p => p.MedicalHistory).FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return NotFound();
        }

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Sex = request.Sex;
        patient.Phone = request.Phone;
        patient.Email = request.Email;
        patient.AddressLine1 = request.AddressLine1;
        patient.AddressLine2 = request.AddressLine2;
        patient.City = request.City;
        patient.EmergencyContactName = request.EmergencyContactName;
        patient.EmergencyContactPhone = request.EmergencyContactPhone;
        patient.InsuranceProvider = request.InsuranceProvider;
        patient.InsurancePolicyNumber = request.InsurancePolicyNumber;
        patient.InsuranceGroupNumber = request.InsuranceGroupNumber;
        patient.IsActive = request.IsActive;
        patient.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(ToDto(patient));
    }

    [HttpPost("{id:guid}/medical-history")]
    public async Task<ActionResult<MedicalHistoryEntryDto>> AddMedicalHistory(Guid id, CreateMedicalHistoryEntryRequest request)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return NotFound();
        }

        var entry = new MedicalHistoryEntry
        {
            Id = Guid.NewGuid(),
            PatientId = id,
            Type = request.Type,
            Description = request.Description,
            Severity = request.Severity,
        };

        db.MedicalHistoryEntries.Add(entry);
        await db.SaveChangesAsync();

        return Ok(new MedicalHistoryEntryDto(entry.Id, entry.Type, entry.Description, entry.Severity, entry.IsActive, entry.NotedAtUtc));
    }

    private static PatientDto ToDto(Patient patient) => new(
        patient.Id,
        patient.FirstName,
        patient.LastName,
        patient.DateOfBirth,
        patient.Sex,
        patient.Phone,
        patient.Email,
        patient.AddressLine1,
        patient.AddressLine2,
        patient.City,
        patient.EmergencyContactName,
        patient.EmergencyContactPhone,
        patient.InsuranceProvider,
        patient.InsurancePolicyNumber,
        patient.InsuranceGroupNumber,
        patient.IsActive,
        patient.MedicalHistory
            .OrderByDescending(m => m.NotedAtUtc)
            .Select(m => new MedicalHistoryEntryDto(m.Id, m.Type, m.Description, m.Severity, m.IsActive, m.NotedAtUtc))
            .ToList());
}
