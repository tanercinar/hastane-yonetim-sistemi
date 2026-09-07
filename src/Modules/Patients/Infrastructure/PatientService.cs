using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Domain;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Patients.Infrastructure;

public sealed class PatientService(
    PatientsDbContext dbContext,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IPatientService
{
    private readonly PatientsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<PatientOperationResult<PatientDetailDto>> RegisterPatientAsync(
        ClaimsPrincipal actor,
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return PatientOperationResult.Validation<PatientDetailDto>("firstName", "Ad alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return PatientOperationResult.Validation<PatientDetailDto>("lastName", "Soyad alanı zorunludur.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var personId = command.PersonId ?? Guid.NewGuid();
        var currentYear = now.Year;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({CreateMrnLockKey(currentYear)})",
            cancellationToken);

        // Check if person already has a patient record
        var existingForPerson = await _dbContext.Patients
            .AnyAsync(p => p.PersonId == personId, cancellationToken);
        if (existingForPerson)
        {
            return PatientOperationResult.Conflict<PatientDetailDto>("Bu kişi için zaten bir hasta kaydı mevcuttur.");
        }

        // Generate Medical Record Number
        var patientCountThisYear = await _dbContext.Patients
            .CountAsync(p => p.CreatedAtUtc.Year == currentYear, cancellationToken);
        var mrn = MedicalRecordNumberGenerator.Generate(currentYear, patientCountThisYear + 1);

        // Ensure MRN is unique
        while (await _dbContext.Patients.AnyAsync(p => p.MedicalRecordNumber == mrn, cancellationToken))
        {
            patientCountThisYear++;
            mrn = MedicalRecordNumberGenerator.Generate(currentYear, patientCountThisYear + 1);
        }

        var patient = Patient.Create(
            Guid.NewGuid(),
            personId,
            mrn,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Gender,
            command.NationalIdSynthetic,
            command.PhoneNumber,
            command.Email,
            command.Address,
            command.EmergencyContact,
            command.CommunicationPreferences,
            now);

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            AuditAction.PatientRegister,
            patient.Id.ToString(),
            AuditOutcome.Success,
            reason: $"Hasta kaydı oluşturuldu: MRN={mrn}",
            cancellationToken: cancellationToken);

        return PatientOperationResult.Success(MapToDetailDto(patient));
    }

    public async Task<PatientOperationResult<PatientDetailDto>> UpdatePatientAsync(
        ClaimsPrincipal actor,
        UpdatePatientCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (patient is null)
        {
            return PatientOperationResult.NotFound<PatientDetailDto>();
        }

        if (patient.Version != command.ExpectedVersion)
        {
            return PatientOperationResult.Conflict<PatientDetailDto>(
                "Hasta kaydı başka bir işlem tarafından güncellendi. Lütfen en güncel veriyi çekerek tekrar deneyin.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        patient.UpdateDemographics(
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Gender,
            command.NationalIdSynthetic,
            command.PhoneNumber,
            command.Email,
            command.Address,
            command.EmergencyContact,
            command.CommunicationPreferences,
            now);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PatientOperationResult.Conflict<PatientDetailDto>(
                "Hasta kaydı eşzamanlı bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            AuditAction.PatientDemographicsEdit,
            patient.Id.ToString(),
            AuditOutcome.Success,
            reason: $"Hasta demografik bilgileri güncellendi: MRN={patient.MedicalRecordNumber}",
            cancellationToken: cancellationToken);

        return PatientOperationResult.Success(MapToDetailDto(patient));
    }

    public async Task<PatientOperationResult<PatientDetailDto>> GetPatientByIdAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return PatientOperationResult.NotFound<PatientDetailDto>();
        }

        if (!CanAccessPatient(actor, patient))
        {
            return PatientOperationResult.Forbidden<PatientDetailDto>();
        }

        await PublishAuditAsync(
            actor,
            AuditAction.PatientView,
            patient.Id.ToString(),
            AuditOutcome.Success,
            reason: $"Hasta kaydı görüntülendi: MRN={patient.MedicalRecordNumber}",
            cancellationToken: cancellationToken);

        return PatientOperationResult.Success(MapToDetailDto(patient));
    }

    public async Task<PatientOperationResult<PatientDetailDto>> GetPatientByPersonIdAsync(
        ClaimsPrincipal actor,
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var patient = await _dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonId == personId, cancellationToken);
        if (patient is null)
        {
            return PatientOperationResult.NotFound<PatientDetailDto>();
        }

        if (!CanAccessPatient(actor, patient))
        {
            return PatientOperationResult.Forbidden<PatientDetailDto>();
        }

        return PatientOperationResult.Success(MapToDetailDto(patient));
    }

    public async Task<PatientOperationResult<PatientListResult>> SearchPatientsAsync(
        ClaimsPrincipal actor,
        PatientSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(query);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : Math.Min(query.PageSize, 100);

        IQueryable<Patient> dbQuery = _dbContext.Patients.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = $"%{query.Query.Trim()}%";
            dbQuery = dbQuery.Where(p =>
                EF.Functions.ILike(p.MedicalRecordNumber, term) ||
                EF.Functions.ILike(p.FirstName, term) ||
                EF.Functions.ILike(p.LastName, term) ||
                (p.NationalIdSynthetic != null && EF.Functions.ILike(p.NationalIdSynthetic, term)));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var summaries = items.Select(p => new PatientSummaryDto(
            p.Id,
            p.PersonId,
            p.MedicalRecordNumber,
            p.FirstName,
            p.LastName,
            p.DateOfBirth,
            p.Gender.ToString(),
            PatientMaskingHelper.MaskNationalId(p.NationalIdSynthetic),
            PatientMaskingHelper.MaskPhoneNumber(p.PhoneNumber),
            p.Email,
            p.IsActive,
            p.CreatedAtUtc)).ToList();

        await PublishAuditAsync(
            actor,
            AuditAction.PatientSearch,
            "patient_records",
            AuditOutcome.Success,
            reason: $"Hasta arama sorgusu çalıştırıldı. Sonuç: {totalCount}",
            cancellationToken: cancellationToken);

        return PatientOperationResult.Success(
            new PatientListResult(summaries, totalCount, page, pageSize));
    }

    public async Task<PatientOperationResult<DuplicateCheckResult>> CheckDuplicateAsync(
        DuplicateCheckQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidates = new List<DuplicateCandidateDto>();

        // 1. Sentetik TC Kimlik eşleşmesi
        if (!string.IsNullOrWhiteSpace(query.NationalIdSynthetic))
        {
            var nationalMatch = await _dbContext.Patients
                .AsNoTracking()
                .Where(p => p.NationalIdSynthetic == query.NationalIdSynthetic.Trim())
                .ToListAsync(cancellationToken);

            foreach (var match in nationalMatch)
            {
                candidates.Add(new DuplicateCandidateDto(
                    match.Id,
                    match.MedicalRecordNumber,
                    $"{match.FirstName} {match.LastName}",
                    match.DateOfBirth,
                    "Kimlik numarası tam eşleşmesi"));
            }
        }

        // 2. Ad + Soyad + Doğum Tarihi eşleşmesi
        var firstName = query.FirstName.Trim();
        var lastName = query.LastName.Trim();
        var nameDobMatch = await _dbContext.Patients
            .AsNoTracking()
            .Where(p =>
                EF.Functions.ILike(p.FirstName, firstName) &&
                EF.Functions.ILike(p.LastName, lastName) &&
                p.DateOfBirth == query.DateOfBirth)
            .ToListAsync(cancellationToken);

        foreach (var match in nameDobMatch)
        {
            if (candidates.All(c => c.PatientId != match.Id))
            {
                candidates.Add(new DuplicateCandidateDto(
                    match.Id,
                    match.MedicalRecordNumber,
                    $"{match.FirstName} {match.LastName}",
                    match.DateOfBirth,
                    "Ad, soyad ve doğum tarihi tam eşleşmesi"));
            }
        }

        return PatientOperationResult.Success(
            new DuplicateCheckResult(candidates.Count > 0, candidates));
    }

    private static bool CanAccessPatient(ClaimsPrincipal actor, Patient patient)
    {
        // Hasta kendi PersonId'sini görebilir
        var actorPersonIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(actorPersonIdStr, out var actorPersonId) && actorPersonId == patient.PersonId)
        {
            return true;
        }

        // Personel yetkisi kontrolü (Kayıt, Doktor, Hemşire vb.)
        if (actor.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Patient.DemographicsView) ||
            actor.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Patient.Search) ||
            actor.HasClaim(HospitalClaimTypes.Permission, HospitalPermissions.Patient.DemographicsEdit))
        {
            return true;
        }

        return false;
    }

    private static PatientDetailDto MapToDetailDto(Patient patient) =>
        new(
            patient.Id,
            patient.PersonId,
            patient.MedicalRecordNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.NationalIdSynthetic,
            patient.PhoneNumber,
            patient.Email,
            patient.Address is null ? null : new AddressDto(
                patient.Address.City,
                patient.Address.District,
                patient.Address.Line1,
                patient.Address.PostalCode),
            patient.EmergencyContact is null ? null : new EmergencyContactDto(
                patient.EmergencyContact.FullName,
                patient.EmergencyContact.Relationship,
                patient.EmergencyContact.PhoneNumber),
            new CommunicationPreferencesDto(
                patient.CommunicationPreferences.AllowSms,
                patient.CommunicationPreferences.AllowEmail,
                patient.CommunicationPreferences.PreferredLanguage),
            patient.IsActive,
            patient.CreatedAtUtc,
            patient.UpdatedAtUtc,
            patient.Version);

    private static long CreateMrnLockKey(int year) =>
        0x48534D4D524E0000L ^ (uint)year;

    private Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        Guid? actorUserId = null;
        Guid? actorPersonId = null;

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var uid))
        {
            actorUserId = uid;
        }

        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var pid))
        {
            actorPersonId = pid;
        }

        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "Patient",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        return _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
