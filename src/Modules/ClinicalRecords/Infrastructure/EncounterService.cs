using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class EncounterService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IEncounterService
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> CreateEncounterAsync(
        ClaimsPrincipal actor,
        CreateEncounterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (command.DepartmentId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "departmentId", "Bölüm kimliği zorunludur.");
        }

        if (command.PrimaryPractitionerId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "primaryPractitionerId", "Sorumlu hekim kimliği zorunludur.");
        }

        if (!await _accessControl.CanCreateEncounterAsync(
                actor,
                command.PrimaryPractitionerId,
                command.DepartmentId,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Klinik karşılaşma oluşturma yetkiniz veya bölüm atamanız bulunmamaktadır.");
        }

        if (command.AppointmentId.HasValue && command.AppointmentId.Value != Guid.Empty)
        {
            var hasActiveEncounter = await _dbContext.Encounters
                .AnyAsync(e =>
                    e.AppointmentId == command.AppointmentId.Value &&
                    (e.Status == EncounterStatus.Planned ||
                     e.Status == EncounterStatus.InProgress ||
                     e.Status == EncounterStatus.Completed),
                    cancellationToken);

            if (hasActiveEncounter)
            {
                return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                    "Bu randevu için zaten aktif veya tamamlanmış bir klinik karşılaşma mevcuttur.");
            }
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var encounterId = Guid.NewGuid();

        var encounter = Encounter.Create(
            encounterId,
            command.AppointmentId,
            command.PatientId,
            command.DepartmentId,
            command.PrimaryPractitionerId,
            command.EncounterType,
            command.PlannedStartTimeUtc,
            command.ChiefComplaint,
            nowUtc,
            command.StartImmediately);

        _dbContext.Encounters.Add(encounter);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (command.AppointmentId.HasValue)
            {
                var duplicateCheck = await _dbContext.Encounters
                    .AnyAsync(e =>
                        e.AppointmentId == command.AppointmentId.Value &&
                        (e.Status == EncounterStatus.Planned ||
                         e.Status == EncounterStatus.InProgress ||
                         e.Status == EncounterStatus.Completed),
                        cancellationToken);

                if (duplicateCheck)
                {
                    return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                        "Bu randevu için zaten bir karşılaşma kaydı bulunmaktadır.");
                }
            }
            throw;
        }

        await PublishAuditAsync(
            actor,
            AuditAction.EncounterStart,
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma oluşturuldu.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> StartEncounterAsync(
        ClaimsPrincipal actor,
        StartEncounterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterStart,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Bu klinik karşılaşmayı başlatma yetkiniz bulunmamaktadır.");
        }

        if (!ActorMatchesCommand(actor, command.PractitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (encounter.Status != EncounterStatus.Planned)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                $"Yalnızca 'Planlandı' durumundaki karşılaşmalar başlatılabilir. Mevcut durum: {encounter.Status}");
        }

        var nowUtc = command.StartTimeUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        var newParticipant = encounter.Start(command.PractitionerId, nowUtc);

        if (newParticipant is not null)
        {
            _dbContext.EncounterParticipants.Add(newParticipant);
        }

        if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            AuditAction.EncounterStart,
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma başlatıldı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> CompleteEncounterAsync(
        ClaimsPrincipal actor,
        CompleteEncounterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterComplete,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Bu klinik karşılaşmayı tamamlama yetkiniz bulunmamaktadır.");
        }

        if (!ActorMatchesCommand(actor, command.PractitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (encounter.Status != EncounterStatus.InProgress)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                $"Yalnızca 'Devam Ediyor' durumundaki karşılaşmalar tamamlanabilir. Mevcut durum: {encounter.Status}");
        }

        // Completeness checks:
        // 1. Check for unfinalized draft notes
        var hasDraftNotes = await _dbContext.ClinicalNotes
            .AnyAsync(n => n.EncounterId == command.EncounterId && n.Status == ClinicalNoteStatus.Draft, cancellationToken);
        if (hasDraftNotes)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "clinicalNotes", "Karşılaşmada henüz imzalanmamış taslak klinik notlar bulunmaktadır. Lütfen muayeneyi tamamlamadan önce tüm taslak notları imzalayın.");
        }

        // 2. Must have at least one signed note or at least one diagnosis
        var hasSignedNotes = await _dbContext.ClinicalNotes
            .AnyAsync(n => n.EncounterId == command.EncounterId && (n.Status == ClinicalNoteStatus.Signed || n.Status == ClinicalNoteStatus.Amended), cancellationToken);

        var hasDiagnoses = await _dbContext.EncounterDiagnoses
            .AnyAsync(d => d.EncounterId == command.EncounterId && !d.IsEnteredInError, cancellationToken);

        if (!hasSignedNotes && !hasDiagnoses)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "clinicalCompleteness", "Muayeneyi tamamlamak için en az bir imzalı klinik not veya en az bir tanı girilmiş olmalıdır.");
        }

        var nowUtc = command.EndTimeUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        encounter.Complete(command.PractitionerId, nowUtc);

        if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            AuditAction.EncounterComplete,
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma tamamlandı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> ReopenEncounterAsync(
        ClaimsPrincipal actor,
        ReopenEncounterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "reason", "Karşılaşmayı yeniden açmak için geçerli bir klinik gerekçe belirtilmelidir.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteReopen,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Tamamlanmış klinik karşılaşmayı yeniden açma izniniz veya bölüm kapsamınız bulunmamaktadır.");
        }

        if (!ActorMatchesCommand(actor, command.PractitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (encounter.Status != EncounterStatus.Completed)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                $"Yalnızca 'Tamamlandı' (Completed) durumundaki karşılaşmalar yeniden açılabilir. Mevcut durum: {encounter.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        encounter.Reopen(command.PractitionerId, command.Reason, nowUtc);

        if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.EncounterReopen",
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma gerekçeli olarak yeniden açıldı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> CancelEncounterAsync(
        ClaimsPrincipal actor,
        CancelEncounterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterStart,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Bu klinik karşılaşmayı iptal etme yetkiniz bulunmamaktadır.");
        }

        if (!ActorMatchesCommand(actor, command.PractitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (encounter.Status is EncounterStatus.Completed or EncounterStatus.Cancelled or EncounterStatus.EnteredInError)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                $"Tamamlanmış veya kapatılmış karşılaşmalar iptal edilemez. Mevcut durum: {encounter.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        encounter.Cancel(command.PractitionerId, command.Reason, nowUtc);

        if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.EncounterCancel",
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma gerekçeli olarak iptal edildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkEncounterEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "reason", "Hatalı giriş işaretlemesi için bir gerekçe girilmelidir.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Karşılaşmayı hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        if (!ActorMatchesCommand(actor, command.PractitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (encounter.Status == EncounterStatus.EnteredInError)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma zaten hatalı giriş olarak işaretlenmiştir.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        encounter.MarkEnteredInError(command.PractitionerId, command.Reason, nowUtc);

        if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.EncounterEnteredInError",
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşma gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> AddParticipantAsync(
        ClaimsPrincipal actor,
        AddEncounterParticipantCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (command.PractitionerId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "practitionerId", "Katılımcı hekim kimliği zorunludur.");
        }

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .FirstOrDefaultAsync(e => e.Id == command.EncounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterStart,
                allowPatientOwnRecord: false,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>(
                "Bu karşılaşmaya katılımcı ekleme yetkiniz bulunmamaktadır.");
        }

        if (encounter.Status is EncounterStatus.Completed or EncounterStatus.Cancelled or EncounterStatus.EnteredInError)
        {
            return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                $"Sonlandırılmış karşılaşmaya katılımcı eklenemez. Mevcut durum: {encounter.Status}");
        }

        if (!await _accessControl.IsPractitionerAssignedToDepartmentAsync(
                command.PractitionerId,
                encounter.DepartmentId,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Validation<EncounterDto>(
                "practitionerId", "Katılımcı klinisyenin karşılaşma bölümünde etkin görevlendirmesi bulunmalıdır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var participant = encounter.AddParticipant(command.PractitionerId, command.Role, nowUtc);

        if (participant is not null)
        {
            _dbContext.EncounterParticipants.Add(participant);
            if (!await TrySaveEncounterAsync(encounter, command.ExpectedVersion, cancellationToken))
            {
                return ClinicalEncounterOperationResult.Conflict<EncounterDto>(
                    "Karşılaşma başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
            }
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.ParticipantAdd",
            encounter.Id.ToString(),
            AuditOutcome.Success,
            "Klinik karşılaşmaya katılımcı eklendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> GetEncounterByIdAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId)
            && encounter.Status == EncounterStatus.EnteredInError)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDto>>> GetPatientEncountersAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<EncounterDto>>();
        }

        var isPatientOwnRecord = ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId);
        var encounters = await _dbContext.Encounters
            .Include(e => e.Participants)
            .AsNoTracking()
            .Where(e => e.PatientId == patientId
                && (!isPatientOwnRecord || e.Status != EncounterStatus.EnteredInError))
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = encounters.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<EncounterDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<EncounterDto>> GetActiveEncounterByAppointmentIdAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var encounter = await _dbContext.Encounters
            .Include(e => e.Participants)
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.AppointmentId == appointmentId &&
                (e.Status == EncounterStatus.Planned ||
                 e.Status == EncounterStatus.InProgress ||
                 e.Status == EncounterStatus.Completed),
                cancellationToken);

        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<EncounterDto>();
        }

        if (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId)
            && encounter.Status == EncounterStatus.EnteredInError)
        {
            return ClinicalEncounterOperationResult.NotFound<EncounterDto>();
        }

        return ClinicalEncounterOperationResult.Success(MapToDto(encounter));
    }

    private static bool ActorMatchesCommand(ClaimsPrincipal actor, Guid practitionerId) =>
        ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId)
        && actorPersonId == practitionerId;

    private async Task<bool> TrySaveEncounterAsync(
        Encounter encounter,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        _dbContext.Entry(encounter).Property(e => e.Version).OriginalValue = expectedVersion;
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
            TargetResourceType: "ClinicalRecords.Encounter",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static EncounterDto MapToDto(Encounter encounter)
    {
        return new EncounterDto(
            encounter.Id,
            encounter.AppointmentId,
            encounter.PatientId,
            encounter.DepartmentId,
            encounter.PrimaryPractitionerId,
            encounter.EncounterType,
            encounter.Status,
            encounter.PlannedStartTimeUtc,
            encounter.ActualStartTimeUtc,
            encounter.ActualEndTimeUtc,
            encounter.ChiefComplaint,
            encounter.CancellationReason,
            encounter.EnteredInErrorReason,
            encounter.ReopenReason,
            encounter.ReopenedAtUtc,
            encounter.ReopenedByPractitionerId,
            encounter.Version,
            encounter.CreatedAtUtc,
            encounter.UpdatedAtUtc,
            encounter.Participants.Select(p => new EncounterParticipantDto(
                p.Id,
                p.EncounterId,
                p.PractitionerId,
                p.Role,
                p.JoinedAtUtc,
                p.LeftAtUtc)).ToList());
    }
}
