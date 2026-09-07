using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class ClinicalNoteService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IClinicalNoteService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> CreateDraftNoteAsync(
        ClaimsPrincipal actor,
        CreateDraftNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.EncounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "title", "Not başlığı zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "patientId", "Hasta kimliği karşılaşmanın hastasıyla eşleşmelidir.");
        }

        if (!ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter))
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Klinik not yalnızca devam eden bir karşılaşmaya eklenebilir.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteEditDraft,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var authorId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>(
                "Taslak klinik not oluşturma izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var noteId = Guid.NewGuid();

        var note = ClinicalNote.CreateDraft(
            noteId,
            command.EncounterId,
            command.PatientId,
            authorId,
            command.NoteType,
            command.Title,
            command.ChiefComplaint,
            command.HistoryOfPresentIllness,
            command.PhysicalExamination,
            command.Assessment,
            command.Plan,
            command.Content,
            nowUtc);

        _dbContext.ClinicalNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteDraftCreate",
            note.Id.ToString(),
            AuditOutcome.Success,
            "Taslak klinik not oluşturuldu.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(note));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> UpdateDraftNoteAsync(
        ClaimsPrincipal actor,
        UpdateDraftNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "title", "Not başlığı zorunludur.");
        }

        var note = await _dbContext.ClinicalNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId, cancellationToken);

        if (note is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Klinik not bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(note.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteEditDraft,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId)
            || note.AuthorPractitionerId != actorPersonId)
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>(
                "Yalnızca notun yazarı, yetkili olduğu karşılaşmadaki taslağı düzenleyebilir.");
        }

        if (note.Status != ClinicalNoteStatus.Draft)
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "İmzalanmış veya kapatılmış bir klinik not doğrudan değiştirilemez. Lütfen ek not (addendum) oluşturunuz.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(note).Property(n => n.Version).OriginalValue = command.ExpectedVersion;

        note.UpdateDraft(
            command.Title,
            command.ChiefComplaint,
            command.HistoryOfPresentIllness,
            command.PhysicalExamination,
            command.Assessment,
            command.Plan,
            command.Content,
            nowUtc);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(note).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Klinik not başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteDraftUpdate",
            note.Id.ToString(),
            AuditOutcome.Success,
            "Taslak klinik not güncellendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(note));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> SignNoteAsync(
        ClaimsPrincipal actor,
        SignClinicalNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var note = await _dbContext.ClinicalNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId, cancellationToken);

        if (note is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Klinik not bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(note.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteSign,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId)
            || note.AuthorPractitionerId != practitionerId)
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>(
                "Yalnızca notun yazarı, yetkili olduğu karşılaşmadaki taslağı imzalayabilir.");
        }

        if (note.Status != ClinicalNoteStatus.Draft)
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Yalnızca taslak durumundaki notlar imzalanabilir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(note).Property(n => n.Version).OriginalValue = command.ExpectedVersion;
        note.Sign(practitionerId, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(note).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Klinik not başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteSign",
            note.Id.ToString(),
            AuditOutcome.Success,
            "Klinik not imzalandı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(note));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> AddAddendumAsync(
        ClaimsPrincipal actor,
        AddClinicalNoteAddendumCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.AddendumContent))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "addendumContent", "Ek not içeriği zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "reason", "Ek not gerekçesi zorunludur.");
        }

        var parentNote = await _dbContext.ClinicalNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId, cancellationToken);

        if (parentNote is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Orijinal klinik not bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(parentNote.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>(
                "Ek not / düzeltme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        if (parentNote.Status != ClinicalNoteStatus.Signed && parentNote.Status != ClinicalNoteStatus.Amended)
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Yalnızca imzalanmış notlara ek not eklenebilir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var addendumId = Guid.NewGuid();

        _dbContext.Entry(parentNote).Property(n => n.Version).OriginalValue = command.ExpectedVersion;

        var addendumNote = parentNote.CreateAddendum(
            addendumId,
            command.AddendumContent,
            command.Reason,
            practitionerId,
            nowUtc);

        _dbContext.ClinicalNotes.Add(addendumNote);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(parentNote).State = EntityState.Detached;
            _dbContext.Entry(addendumNote).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Orijinal klinik not başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteAddendum",
            addendumNote.Id.ToString(),
            AuditOutcome.Success,
            "İmzalı klinik nota gerekçeli ek not eklendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(addendumNote));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkClinicalNoteEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalNoteDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var note = await _dbContext.ClinicalNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId, cancellationToken);

        if (note is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Klinik not bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(note.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>(
                "Klinik notu hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(note).Property(n => n.Version).OriginalValue = command.ExpectedVersion;
        note.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(note).State = EntityState.Detached;
            return ClinicalEncounterOperationResult.Conflict<ClinicalNoteDto>(
                "Klinik not başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteEnteredInError",
            note.Id.ToString(),
            AuditOutcome.Success,
            "Klinik not gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(note));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalNoteDto>> GetNoteByIdAsync(
        ClaimsPrincipal actor,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var note = await _dbContext.ClinicalNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        if (note is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalNoteDto>(
                "Klinik not bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(note.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken)
            || (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, note.PatientId)
                && note.Status is ClinicalNoteStatus.Draft or ClinicalNoteStatus.EnteredInError))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalNoteDto>();
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteView",
            note.Id.ToString(),
            AuditOutcome.Success,
            "Klinik not görüntülendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(note));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalNoteDto>>> GetEncounterNotesAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ClinicalNoteDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<IReadOnlyList<ClinicalNoteDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ClinicalNoteDto>>();
        }

        var patientOwnRecord = ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId);

        var noteQuery = _dbContext.ClinicalNotes
            .AsNoTracking()
            .Where(n => n.EncounterId == encounterId);

        if (patientOwnRecord)
        {
            noteQuery = noteQuery.Where(n =>
                n.Status == ClinicalNoteStatus.Signed || n.Status == ClinicalNoteStatus.Amended);
        }

        var notes = await noteQuery
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = notes.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ClinicalNoteDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalNoteDto>>> GetPatientNotesAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ClinicalNoteDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ClinicalNoteDto>>();
        }

        var noteQuery = _dbContext.ClinicalNotes
            .AsNoTracking()
            .Where(n => n.PatientId == patientId);

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            noteQuery = noteQuery.Where(n =>
                n.Status == ClinicalNoteStatus.Signed || n.Status == ClinicalNoteStatus.Amended);
        }

        var notes = await noteQuery
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ClinicalNoteView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta klinik notları listelendi.",
            cancellationToken);

        var dtos = notes.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ClinicalNoteDto>>(dtos);
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

    private static ClinicalNoteDto MapToDto(ClinicalNote n)
    {
        return new ClinicalNoteDto(
            n.Id,
            n.EncounterId,
            n.PatientId,
            n.AuthorPractitionerId,
            n.NoteType,
            n.Status,
            n.Title,
            n.ChiefComplaint,
            n.HistoryOfPresentIllness,
            n.PhysicalExamination,
            n.Assessment,
            n.Plan,
            n.Content,
            n.SignedAtUtc,
            n.SignedByPractitionerId,
            n.ParentNoteId,
            n.CorrectionReason,
            n.EnteredInErrorReason,
            n.CreatedAtUtc,
            n.UpdatedAtUtc,
            n.Version);
    }
}
