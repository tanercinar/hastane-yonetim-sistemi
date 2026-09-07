using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class DiagnosisService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IDiagnosisService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<IReadOnlyList<DiagnosisCatalogItemDto>> SearchDiagnosisCatalogAsync(
        string? query,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.DiagnosisCatalogItems
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            queryable = queryable.Where(c =>
                EF.Functions.ILike(c.Code, pattern) ||
                EF.Functions.ILike(c.NameTurkish, pattern) ||
                EF.Functions.ILike(c.NameEnglish, pattern));
        }

        var items = await queryable
            .OrderBy(c => c.Code)
            .Take(Math.Clamp(maxResults, 1, 100))
            .ToListAsync(cancellationToken);

        return items.Select(c => new DiagnosisCatalogItemDto(
            c.Id,
            c.Code,
            c.NameTurkish,
            c.NameEnglish,
            c.Chapter,
            c.Block,
            c.CatalogVersion)).ToList();
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> RecordDiagnosisAsync(
        ClaimsPrincipal actor,
        RecordDiagnosisCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.EncounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.DiagnosisTitle))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "diagnosisTitle", "Tanı başlığı zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDiagnosisDto>(
                "Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "patientId", "Hasta kimliği karşılaşmanın hastasıyla eşleşmelidir.");
        }

        if (!ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Tanı yalnızca devam eden bir karşılaşmaya eklenebilir.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDiagnosisDto>(
                "Tanı kaydetme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var diagnosisId = Guid.NewGuid();

        EncounterDiagnosis diagnosis;
        if (command.IsCoded)
        {
            if (string.IsNullOrWhiteSpace(command.Icd10Code))
            {
                return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                    "icd10Code", "Kodlanmış tanıda ICD-10 tanı kodu zorunludur.");
            }

            var lookupCode = command.Icd10Code.Trim().ToUpperInvariant();
            var catalogItem = await _dbContext.DiagnosisCatalogItems
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == lookupCode, cancellationToken);

            if (catalogItem is null)
            {
                return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                    "icd10Code", "Tanı kodu etkin demo ICD-10 kataloğunda bulunamadı.");
            }

            diagnosis = EncounterDiagnosis.CreateCoded(
                diagnosisId,
                command.EncounterId,
                command.PatientId,
                practitionerId,
                command.DiagnosisType,
                command.Icd10Code,
                catalogItem.NameTurkish,
                catalogItem.CatalogVersion,
                command.Notes,
                nowUtc);
        }
        else
        {
            diagnosis = EncounterDiagnosis.CreateFreeText(
                diagnosisId,
                command.EncounterId,
                command.PatientId,
                practitionerId,
                command.DiagnosisType,
                command.DiagnosisTitle,
                command.Notes,
                nowUtc);
        }

        _dbContext.EncounterDiagnoses.Add(diagnosis);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.DiagnosisRecord",
            diagnosis.Id.ToString(),
            AuditOutcome.Success,
            "Karşılaşma tanısı kaydedildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(diagnosis));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> UpdateDiagnosisAsync(
        ClaimsPrincipal actor,
        UpdateDiagnosisCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.DiagnosisTitle))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "diagnosisTitle", "Tanı başlığı zorunludur.");
        }

        var diagnosis = await _dbContext.EncounterDiagnoses
            .FirstOrDefaultAsync(d => d.Id == command.DiagnosisId, cancellationToken);

        if (diagnosis is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDiagnosisDto>(
                "Tanı kaydı bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(diagnosis.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDiagnosisDto>(
                "Tanı güncelleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        if (!ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Tamamlanmış veya kapatılmış karşılaşmanın tanısı yerinde değiştirilemez.");
        }

        if (diagnosis.IsEnteredInError)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Hatalı giriş olarak işaretlenmiş bir tanı kaydı güncellenemez.");
        }

        if (diagnosis.DiagnosisType == DiagnosisType.Final)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Kesin tanı sessizce değiştirilemez. Eski kaydı gerekçeli olarak hatalı giriş durumuna alın ve yeni tanı kaydı oluşturun.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        string? catalogVersion = null;
        string? catalogTitle = null;

        if (command.IsCoded)
        {
            if (string.IsNullOrWhiteSpace(command.Icd10Code))
            {
                return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                    "icd10Code", "Kodlanmış tanıda ICD-10 tanı kodu zorunludur.");
            }

            var lookupCode = command.Icd10Code.Trim().ToUpperInvariant();
            var catalogItem = await _dbContext.DiagnosisCatalogItems
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == lookupCode, cancellationToken);

            if (catalogItem is null)
            {
                return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                    "icd10Code", "Tanı kodu etkin demo ICD-10 kataloğunda bulunamadı.");
            }

            catalogVersion = catalogItem.CatalogVersion;
            catalogTitle = catalogItem.NameTurkish;
        }

        var diagnosisTitle = command.IsCoded
            ? catalogTitle!
            : command.DiagnosisTitle.Trim();

        _dbContext.Entry(diagnosis).Property(d => d.Version).OriginalValue = command.ExpectedVersion;
        diagnosis.Update(
            command.DiagnosisType,
            command.IsCoded,
            command.Icd10Code,
            diagnosisTitle,
            catalogVersion,
            command.Notes,
            nowUtc);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(diagnosis).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Tanı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.DiagnosisUpdate",
            diagnosis.Id.ToString(),
            AuditOutcome.Success,
            "Taslak/ön tanı güncellendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(diagnosis));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDiagnosisDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkDiagnosisEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDiagnosisDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var diagnosis = await _dbContext.EncounterDiagnoses
            .FirstOrDefaultAsync(d => d.Id == command.DiagnosisId, cancellationToken);

        if (diagnosis is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDiagnosisDto>(
                "Tanı kaydı bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(diagnosis.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.DiagnosisRecord,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDiagnosisDto>(
                "Tanıyı hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(diagnosis).Property(d => d.Version).OriginalValue = command.ExpectedVersion;
        diagnosis.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(diagnosis).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<EncounterDiagnosisDto>(
                "Tanı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.DiagnosisEnteredInError",
            diagnosis.Id.ToString(),
            AuditOutcome.Success,
            "Tanı gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(diagnosis));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDiagnosisDto>>> GetEncounterDiagnosesAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<EncounterDiagnosisDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<IReadOnlyList<EncounterDiagnosisDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<EncounterDiagnosisDto>>();
        }

        var diagnosisQuery = _dbContext.EncounterDiagnoses
            .AsNoTracking()
            .Where(d => d.EncounterId == encounterId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId))
        {
            diagnosisQuery = diagnosisQuery.Where(d => !d.IsEnteredInError);
        }

        var diagnoses = await diagnosisQuery
            .OrderBy(d => d.DiagnosisType)
            .ThenBy(d => d.DiagnosedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = diagnoses.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<EncounterDiagnosisDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDiagnosisDto>>> GetPatientDiagnosesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<EncounterDiagnosisDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<EncounterDiagnosisDto>>();
        }

        var diagnosisQuery = _dbContext.EncounterDiagnoses
            .AsNoTracking()
            .Where(d => d.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            diagnosisQuery = diagnosisQuery.Where(d => !d.IsEnteredInError);
        }

        var diagnoses = await diagnosisQuery
            .OrderByDescending(d => d.DiagnosedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.DiagnosisView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta tanıları listelendi.",
            cancellationToken);

        var dtos = diagnoses.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<EncounterDiagnosisDto>>(dtos);
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string reason,
        CancellationToken cancellationToken)
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
            TargetResourceType: "ClinicalRecords",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static EncounterDiagnosisDto MapToDto(EncounterDiagnosis d)
    {
        return new EncounterDiagnosisDto(
            d.Id,
            d.EncounterId,
            d.PatientId,
            d.DiagnosedByPractitionerId,
            d.DiagnosisType,
            d.IsCoded,
            d.Icd10Code,
            d.DiagnosisTitle,
            d.CatalogVersion,
            d.Notes,
            d.DiagnosedAtUtc,
            d.UpdatedAtUtc,
            d.IsEnteredInError,
            d.EnteredInErrorReason,
            d.Version);
    }
}
