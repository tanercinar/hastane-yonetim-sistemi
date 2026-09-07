using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class AllergyProblemService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IAllergyProblemService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<AllergyDto>> CreateAllergyAsync(
        ClaimsPrincipal actor,
        CreateAllergyCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<AllergyDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Substance))
        {
            return ClinicalEncounterOperationResult.Validation<AllergyDto>(
                "substance", "Alerjen madde adı zorunludur.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                command.PatientId,
                command.EncounterId,
                requireOpenEncounter: command.EncounterId.HasValue,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<AllergyDto>(
                "Alerji kaydı oluşturma izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var allergyId = Guid.NewGuid();

        var allergy = AllergyIntolerance.Create(
            allergyId,
            command.PatientId,
            command.EncounterId,
            command.Substance,
            command.Category,
            command.Criticality,
            command.Manifestation,
            command.OnsetDateTimeUtc,
            command.Notes,
            practitionerId,
            nowUtc);

        _dbContext.AllergyIntolerances.Add(allergy);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AllergyCreate",
            allergy.Id.ToString(),
            AuditOutcome.Success,
            "Alerji kaydı oluşturuldu.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(allergy));
    }

    public async Task<ClinicalEncounterOperationResult<AllergyDto>> UpdateAllergyStatusAsync(
        ClaimsPrincipal actor,
        UpdateAllergyStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<AllergyDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var allergy = await _dbContext.AllergyIntolerances
            .FirstOrDefaultAsync(a => a.Id == command.AllergyId, cancellationToken);

        if (allergy is null)
        {
            return ClinicalEncounterOperationResult.NotFound<AllergyDto>(
                "Alerji kaydı bulunamadı.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                allergy.PatientId,
                allergy.EncounterId,
                requireOpenEncounter: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<AllergyDto>(
                "Alerji durumunu güncelleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        allergy.UpdateStatus(command.ClinicalStatus, command.Notes, practitionerId, nowUtc);
        if (!await TrySaveWithVersionAsync(allergy, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<AllergyDto>(
                "Alerji kaydı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AllergyUpdate",
            allergy.Id.ToString(),
            AuditOutcome.Success,
            "Alerji durumu güncellendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(allergy));
    }

    public async Task<ClinicalEncounterOperationResult<AllergyDto>> MarkAllergyEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkAllergyEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<AllergyDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<AllergyDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var allergy = await _dbContext.AllergyIntolerances
            .FirstOrDefaultAsync(a => a.Id == command.AllergyId, cancellationToken);

        if (allergy is null)
        {
            return ClinicalEncounterOperationResult.NotFound<AllergyDto>(
                "Alerji kaydı bulunamadı.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                allergy.PatientId,
                allergy.EncounterId,
                requireOpenEncounter: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<AllergyDto>(
                "Alerjiyi hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        allergy.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        if (!await TrySaveWithVersionAsync(allergy, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<AllergyDto>(
                "Alerji kaydı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AllergyEnteredInError",
            allergy.Id.ToString(),
            AuditOutcome.Success,
            "Alerji kaydı gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(allergy));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<AllergyDto>>> GetPatientAllergiesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<AllergyDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<AllergyDto>>();
        }

        var allergyQuery = _dbContext.AllergyIntolerances
            .AsNoTracking()
            .Where(a => a.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            allergyQuery = allergyQuery.Where(a =>
                a.VerificationStatus != AllergyVerificationStatus.EnteredInError);
        }

        var allergies = await allergyQuery
            .OrderByDescending(a => a.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AllergyView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta alerjileri görüntülendi.",
            cancellationToken);

        var dtos = allergies.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<AllergyDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> CreateProblemAsync(
        ClaimsPrincipal actor,
        CreateClinicalProblemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalProblemDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.ProblemTitle))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalProblemDto>(
                "problemTitle", "Problem başlığı zorunludur.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                command.PatientId,
                command.EncounterId,
                requireOpenEncounter: command.EncounterId.HasValue,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalProblemDto>(
                "Problem kaydı oluşturma izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var problemId = Guid.NewGuid();

        var problem = ClinicalProblem.Create(
            problemId,
            command.PatientId,
            command.EncounterId,
            command.ProblemTitle,
            command.Code,
            command.Category,
            command.OnsetDate,
            command.Notes,
            practitionerId,
            nowUtc);

        _dbContext.ClinicalProblems.Add(problem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ProblemCreate",
            problem.Id.ToString(),
            AuditOutcome.Success,
            "Klinik problem kaydı oluşturuldu.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(problem));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> UpdateProblemStatusAsync(
        ClaimsPrincipal actor,
        UpdateClinicalProblemStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalProblemDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var problem = await _dbContext.ClinicalProblems
            .FirstOrDefaultAsync(p => p.Id == command.ProblemId, cancellationToken);

        if (problem is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalProblemDto>(
                "Problem kaydı bulunamadı.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                problem.PatientId,
                problem.EncounterId,
                requireOpenEncounter: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalProblemDto>(
                "Problem durumunu güncelleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        problem.UpdateStatus(command.ClinicalStatus, command.ResolvedDate, command.Notes, practitionerId, nowUtc);
        if (!await TrySaveWithVersionAsync(problem, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalProblemDto>(
                "Problem kaydı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ProblemUpdate",
            problem.Id.ToString(),
            AuditOutcome.Success,
            "Klinik problem durumu güncellendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(problem));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalProblemDto>> MarkProblemEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkClinicalProblemEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalProblemDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalProblemDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var problem = await _dbContext.ClinicalProblems
            .FirstOrDefaultAsync(p => p.Id == command.ProblemId, cancellationToken);

        if (problem is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalProblemDto>(
                "Problem kaydı bulunamadı.");
        }

        if (!await CanManageClinicalHistoryAsync(
                actor,
                problem.PatientId,
                problem.EncounterId,
                requireOpenEncounter: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalProblemDto>(
                "Problemi hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        problem.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        if (!await TrySaveWithVersionAsync(problem, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalProblemDto>(
                "Problem kaydı başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ProblemEnteredInError",
            problem.Id.ToString(),
            AuditOutcome.Success,
            "Klinik problem gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(problem));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalProblemDto>>> GetPatientProblemsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ClinicalProblemDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ClinicalProblemDto>>();
        }

        var problemQuery = _dbContext.ClinicalProblems
            .AsNoTracking()
            .Where(p => p.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            problemQuery = problemQuery.Where(p =>
                p.VerificationStatus != ProblemVerificationStatus.EnteredInError);
        }

        var problems = await problemQuery
            .OrderByDescending(p => p.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ProblemView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta klinik problemleri görüntülendi.",
            cancellationToken);

        var dtos = problems.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ClinicalProblemDto>>(dtos);
    }

    private async Task<bool> CanManageClinicalHistoryAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        Guid? encounterId,
        bool requireOpenEncounter,
        CancellationToken cancellationToken)
    {
        var permission = ClinicalRecordAccessControl.HasPermission(
            actor,
            HospitalPermissions.ClinicalRecords.DiagnosisRecord)
            ? HospitalPermissions.ClinicalRecords.DiagnosisRecord
            : ClinicalRecordAccessControl.HasPermission(
                actor,
                HospitalPermissions.ClinicalRecords.ObservationRecordVital)
                ? HospitalPermissions.ClinicalRecords.ObservationRecordVital
                : null;

        if (permission is null)
        {
            return false;
        }

        if (!encounterId.HasValue)
        {
            return await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                permission,
                allowPatientOwnRecord: false,
                cancellationToken);
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId.Value, cancellationToken);
        if (encounter is null
            || encounter.PatientId != patientId
            || (requireOpenEncounter
                && !ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter)))
        {
            return false;
        }

        return await _accessControl.CanAccessEncounterAsync(
            actor,
            encounter,
            permission,
            allowPatientOwnRecord: false,
            cancellationToken);
    }

    private async Task<bool> TrySaveWithVersionAsync<TEntity>(
        TEntity entity,
        long expectedVersion,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        _dbContext.Entry(entity).Property("Version").OriginalValue = expectedVersion;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
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

    private static AllergyDto MapToDto(AllergyIntolerance a)
    {
        return new AllergyDto(
            a.Id,
            a.PatientId,
            a.EncounterId,
            a.Substance,
            a.Category,
            a.Criticality,
            a.ClinicalStatus,
            a.VerificationStatus,
            a.Manifestation,
            a.OnsetDateTimeUtc,
            a.Notes,
            a.RecordedByPractitionerId,
            a.RecordedAtUtc,
            a.UpdatedAtUtc,
            a.EnteredInErrorReason,
            a.Version);
    }

    private static ClinicalProblemDto MapToDto(ClinicalProblem p)
    {
        return new ClinicalProblemDto(
            p.Id,
            p.PatientId,
            p.EncounterId,
            p.ProblemTitle,
            p.Code,
            p.Category,
            p.ClinicalStatus,
            p.VerificationStatus,
            p.OnsetDate,
            p.ResolvedDate,
            p.Notes,
            p.RecordedByPractitionerId,
            p.RecordedAtUtc,
            p.UpdatedAtUtc,
            p.EnteredInErrorReason,
            p.Version);
    }
}
