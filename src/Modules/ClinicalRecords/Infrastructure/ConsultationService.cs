using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class ConsultationService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IConsultationService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> RequestConsultationAsync(
        ClaimsPrincipal actor,
        RequestConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.EncounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (command.TargetDepartmentId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "targetDepartmentId", "Hedef bölüm kimliği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.ReasonForConsultation))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "reasonForConsultation", "Konsültasyon gerekçesi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.ClinicalQuestion))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "clinicalQuestion", "Klinik soru / danışılan konu zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "patientId", "Hasta kimliği karşılaşmanın hastasıyla eşleşmelidir.");
        }

        if (!ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter))
        {
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon yalnızca devam eden bir karşılaşmadan istenebilir.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ConsultationRequest,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var requestingDoctorId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Konsültasyon isteme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var consultationId = Guid.NewGuid();

        var consultation = ConsultationRequest.Create(
            consultationId,
            command.EncounterId,
            command.PatientId,
            requestingDoctorId,
            command.TargetDepartmentId,
            command.TargetPractitionerId,
            command.Urgency,
            command.ReasonForConsultation,
            command.ClinicalQuestion,
            nowUtc);

        _dbContext.ConsultationRequests.Add(consultation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationRequest",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Klinik konsültasyon istendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> AcceptConsultationAsync(
        ClaimsPrincipal actor,
        AcceptConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var consultation = await _dbContext.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon talebi bulunamadı.");
        }

        if (!await CanRespondAsync(actor, consultation, cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Konsültasyon kabul etme izniniz veya hedef bölüm/uzman atamanız bulunmamaktadır.");
        }

        if (consultation.Status != ConsultationStatus.Requested)
        {
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                $"Yalnızca talep durumundaki konsültasyonlar kabul edilebilir. Mevcut durum: {consultation.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(consultation).Property(c => c.Version).OriginalValue = command.ExpectedVersion;
        consultation.Accept(practitionerId, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(consultation).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationAccept",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon kabul edildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> CompleteConsultationAsync(
        ClaimsPrincipal actor,
        CompleteConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.ConsultationReport))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "consultationReport", "Konsültasyon yanıt raporu zorunludur.");
        }

        var consultation = await _dbContext.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon talebi bulunamadı.");
        }

        if (!await CanRespondAsync(actor, consultation, cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId)
            || consultation.AssignedPractitionerId != practitionerId)
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Yalnızca konsültasyonu kabul eden atanmış uzman yanıtlayıp tamamlayabilir.");
        }

        if (consultation.Status != ConsultationStatus.Accepted && consultation.Status != ConsultationStatus.InProgress)
        {
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                $"Konsültasyon tamamlanabilmesi için kabul edilmiş olmalıdır. Mevcut durum: {consultation.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(consultation).Property(c => c.Version).OriginalValue = command.ExpectedVersion;
        consultation.Complete(practitionerId, command.ConsultationReport, command.Recommendation, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(consultation).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationComplete",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon tamamlandı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> DeclineConsultationAsync(
        ClaimsPrincipal actor,
        DeclineConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "reason", "Red gerekçesi zorunludur.");
        }

        var consultation = await _dbContext.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon talebi bulunamadı.");
        }

        if (!await CanRespondAsync(actor, consultation, cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Konsültasyon reddetme izniniz veya hedef bölüm/uzman atamanız bulunmamaktadır.");
        }

        if (consultation.Status != ConsultationStatus.Requested)
        {
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                $"Yalnızca talep durumundaki konsültasyonlar reddedilebilir. Mevcut durum: {consultation.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(consultation).Property(c => c.Version).OriginalValue = command.ExpectedVersion;
        consultation.Decline(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(consultation).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationDecline",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon gerekçeli olarak reddedildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> CancelConsultationAsync(
        ClaimsPrincipal actor,
        CancelConsultationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "reason", "İptal gerekçesi zorunludur.");
        }

        var consultation = await _dbContext.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon talebi bulunamadı.");
        }

        if (!ClinicalRecordAccessControl.HasPermission(
                actor,
                HospitalPermissions.ClinicalRecords.ConsultationRequest)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId)
            || consultation.RequestingPractitionerId != practitionerId)
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Yalnızca konsültasyonu isteyen hekim talebi iptal edebilir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(consultation).Property(c => c.Version).OriginalValue = command.ExpectedVersion;
        consultation.Cancel(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(consultation).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationCancel",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon gerekçeli olarak iptal edildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkConsultationEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ConsultationDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var consultation = await _dbContext.ConsultationRequests
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon talebi bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(consultation.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ConsultationRequest,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId)
            || (consultation.RequestingPractitionerId != practitionerId
                && !actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)))
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>(
                "Konsültasyonu hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(consultation).Property(c => c.Version).OriginalValue = command.ExpectedVersion;
        consultation.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(consultation).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ConsultationDto>(
                "Konsültasyon başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationEnteredInError",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<ConsultationDto>> GetConsultationByIdAsync(
        ClaimsPrincipal actor,
        Guid consultationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var consultation = await _dbContext.ConsultationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consultationId, cancellationToken);

        if (consultation is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ConsultationDto>(
                "Konsültasyon bulunamadı.");
        }

        if (!await CanViewConsultationAsync(actor, consultation, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<ConsultationDto>();
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationView",
            consultation.Id.ToString(),
            AuditOutcome.Success,
            "Konsültasyon görüntülendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(consultation));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetEncounterConsultationsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ConsultationDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<IReadOnlyList<ConsultationDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ConsultationDto>>();
        }

        var consultationQuery = _dbContext.ConsultationRequests
            .AsNoTracking()
            .Where(c => c.EncounterId == encounterId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId))
        {
            consultationQuery = consultationQuery.Where(c => c.Status != ConsultationStatus.EnteredInError);
        }

        var consultations = await consultationQuery
            .OrderBy(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = consultations.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ConsultationDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetDepartmentPendingConsultationsAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (departmentId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ConsultationDto>>(
                "departmentId", "Bölüm kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessDepartmentAsync(
                actor,
                departmentId,
                HospitalPermissions.ClinicalRecords.ConsultationRespond,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ConsultationDto>>();
        }

        var consultations = await _dbContext.ConsultationRequests
            .AsNoTracking()
            .Where(c => c.TargetDepartmentId == departmentId &&
                        (c.Status == ConsultationStatus.Requested || c.Status == ConsultationStatus.Accepted || c.Status == ConsultationStatus.InProgress))
            .OrderByDescending(c => c.Urgency)
            .ThenBy(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = consultations.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ConsultationDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetPatientConsultationsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ConsultationDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ConsultationDto>>();
        }

        var consultationQuery = _dbContext.ConsultationRequests
            .AsNoTracking()
            .Where(c => c.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            consultationQuery = consultationQuery.Where(c => c.Status != ConsultationStatus.EnteredInError);
        }

        var consultations = await consultationQuery
            .OrderByDescending(c => c.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ConsultationView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta konsültasyonları listelendi.",
            cancellationToken);

        var dtos = consultations.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ConsultationDto>>(dtos);
    }

    private async Task<bool> CanRespondAsync(
        ClaimsPrincipal actor,
        ConsultationRequest consultation,
        CancellationToken cancellationToken)
    {
        if (!ClinicalRecordAccessControl.HasPermission(
                actor,
                HospitalPermissions.ClinicalRecords.ConsultationRespond)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        if (consultation.AssignedPractitionerId.HasValue)
        {
            return consultation.AssignedPractitionerId.Value == actorPersonId;
        }

        if (consultation.TargetPractitionerId.HasValue)
        {
            return consultation.TargetPractitionerId.Value == actorPersonId;
        }

        return await _accessControl.CanAccessDepartmentAsync(
            actor,
            consultation.TargetDepartmentId,
            HospitalPermissions.ClinicalRecords.ConsultationRespond,
            cancellationToken);
    }

    private async Task<bool> CanViewConsultationAsync(
        ClaimsPrincipal actor,
        ConsultationRequest consultation,
        CancellationToken cancellationToken)
    {
        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, consultation.PatientId))
        {
            return consultation.Status != ConsultationStatus.EnteredInError;
        }

        if (!ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        if (consultation.RequestingPractitionerId == actorPersonId
            && ClinicalRecordAccessControl.HasPermission(
                actor,
                HospitalPermissions.ClinicalRecords.ConsultationRequest))
        {
            return true;
        }

        if ((consultation.TargetPractitionerId == actorPersonId
                || consultation.AssignedPractitionerId == actorPersonId)
            && ClinicalRecordAccessControl.HasPermission(
                actor,
                HospitalPermissions.ClinicalRecords.ConsultationRespond))
        {
            return true;
        }

        var encounter = await _accessControl.FindEncounterAsync(consultation.EncounterId, cancellationToken);
        return encounter is not null
            && await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: false,
                cancellationToken);
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

    private static ConsultationDto MapToDto(ConsultationRequest c)
    {
        return new ConsultationDto(
            c.Id,
            c.EncounterId,
            c.PatientId,
            c.RequestingPractitionerId,
            c.TargetDepartmentId,
            c.TargetPractitionerId,
            c.AssignedPractitionerId,
            c.Urgency,
            c.Status,
            c.ReasonForConsultation,
            c.ClinicalQuestion,
            c.ConsultationReport,
            c.Recommendation,
            c.DeclineReason,
            c.CancellationReason,
            c.EnteredInErrorReason,
            c.RequestedAtUtc,
            c.AcceptedAtUtc,
            c.CompletedAtUtc,
            c.UpdatedAtUtc,
            c.Version);
    }
}
