using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zeppelin.Api.Dtos.Clinical;
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
        [FromQuery] string? search,
        [FromQuery] string? searchBy = "name",
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25)
    {
        take = Math.Clamp(take, 1, MaxPageSize);
        skip = Math.Max(0, skip);

        var query = db.Patients.AsNoTracking();

        // Omitted status = today's behavior (active only), so existing callers
        // like the appointment-booking patient picker are unaffected.
        query = status?.ToLowerInvariant() switch
        {
            "all" => query,
            "archived" => query.Where(p => !p.IsActive),
            "initial" => query.Where(p => p.IsActive && !p.Appointments.Any()),
            _ => query.Where(p => p.IsActive),
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = searchBy?.ToLowerInvariant() switch
            {
                "phone" => query.Where(p => p.Phone != null && p.Phone.Contains(term)),
                "mrn" => int.TryParse(term, out var mrn) ? query.Where(p => p.PatientNumber == mrn) : query.Where(p => false),
                "identity" => query.Where(p => p.IdentityNumber != null && EF.Functions.ILike(p.IdentityNumber, $"%{term}%")),
                "email" => query.Where(p => p.Email != null && EF.Functions.ILike(p.Email, $"%{term}%")),
                _ => query.Where(p => EF.Functions.ILike(p.FirstName, $"%{term}%") || EF.Functions.ILike(p.LastName, $"%{term}%")),
            };
        }

        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip(skip)
            .Take(take)
            .Select(p => new PatientSummaryDto(p.Id, p.PatientNumber, p.FirstName, p.LastName, p.DateOfBirth, p.Phone, p.Email, p.IsActive))
            .ToListAsync();

        return Ok(new PagedResultDto<PatientSummaryDto>(patients, totalCount, skip, take));
    }

    [HttpGet("status-counts")]
    public async Task<ActionResult<PatientStatusCountsDto>> GetStatusCounts()
    {
        var all = await db.Patients.CountAsync();
        var archived = await db.Patients.CountAsync(p => !p.IsActive);
        var initial = await db.Patients.CountAsync(p => p.IsActive && !p.Appointments.Any());

        return Ok(new PatientStatusCountsDto(all, initial, archived));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        var patient = await db.Patients
            .Include(p => p.MedicalHistory)
            .FirstOrDefaultAsync(p => p.Id == id);

        return patient is null ? NotFound() : Ok(ToDto(patient));
    }

    [HttpGet("{id:guid}/visit-notes")]
    public async Task<ActionResult<IReadOnlyList<VisitNoteDto>>> GetVisitNotes(Guid id)
    {
        var notes = await db.VisitNotes
            .Include(n => n.AuthoredByUser)
            .Where(n => n.PatientId == id)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync();

        return Ok(notes.Select(VisitNotesController.ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request)
    {
        var nextPatientNumber = (await db.Patients.MaxAsync(p => (int?)p.PatientNumber) ?? 0) + 1;

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            PatientNumber = nextPatientNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Sex = request.Sex,
            Phone = request.Phone,
            Email = request.Email,
            IdentityNumber = request.IdentityNumber,
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
        patient.IdentityNumber = request.IdentityNumber;
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
        patient.PatientNumber,
        patient.FirstName,
        patient.LastName,
        patient.DateOfBirth,
        patient.Sex,
        patient.Phone,
        patient.Email,
        patient.IdentityNumber,
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
