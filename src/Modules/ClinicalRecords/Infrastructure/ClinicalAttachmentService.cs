using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.BuildingBlocks.Storage;
using HospitalManagement.Modules.ClinicalRecords.Application;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

public sealed class ClinicalAttachmentService(
    ClinicalRecordsDbContext dbContext,
    ClinicalRecordAccessControl accessControl,
    IBlobStorageService blobStorage,
    IAttachmentMalwareScanner malwareScanner,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : IClinicalAttachmentService
{
    private const string ContainerName = "clinical-attachments";

    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ClinicalRecordAccessControl _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
    private readonly IBlobStorageService _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    private readonly IAttachmentMalwareScanner _malwareScanner = malwareScanner ?? throw new ArgumentNullException(nameof(malwareScanner));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> UploadAttachmentAsync(
        ClaimsPrincipal actor,
        UploadAttachmentCommand command,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(fileStream);

        if (command.EncounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalAttachmentDto>(
                "Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "patientId", "Hasta kimliği karşılaşmanın hastasıyla eşleşmelidir.");
        }

        if (!ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter))
        {
            return ClinicalEncounterOperationResult.Conflict<ClinicalAttachmentDto>(
                "Klinik ek yalnızca devam eden bir karşılaşmaya yüklenebilir.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalAttachmentUpload,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalAttachmentDto>(
                "Klinik ek yükleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var fileBytes = await ReadWithLimitAsync(fileStream, cancellationToken);
        if (fileBytes is null)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "file", "Dosya boyutu maksimum izin verilen 15 MB sınırını aşıyor.");
        }

        var sanitizedFileName = AttachmentSecurityValidator.SanitizeFileName(command.RawFileName);
        var validation = AttachmentSecurityValidator.ValidateAttachment(
            sanitizedFileName,
            command.DeclaredContentType,
            fileBytes);

        if (!validation.IsValid)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "file", validation.ErrorMessage ?? "Geçersiz veya zararlı dosya içeriği.");
        }


        var malwareScan = await _malwareScanner.ScanAsync(fileBytes, cancellationToken);
        if (!malwareScan.IsClean)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "file", "Dosya MOCK kötü amaçlı içerik kontrolünden geçemedi.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var attachmentId = Guid.NewGuid();
        var sha256 = AttachmentSecurityValidator.ComputeSha256Checksum(fileBytes);
        var storageKey = $"{command.PatientId:N}/{attachmentId:N}";

        using (var uploadStream = new MemoryStream(fileBytes))
        {
            await _blobStorage.UploadAsync(
                ContainerName,
                storageKey,
                uploadStream,
                validation.ContentType ?? "application/octet-stream",
                cancellationToken);
        }

        var attachment = ClinicalAttachment.Create(
            attachmentId,
            command.EncounterId,
            command.PatientId,
            practitionerId,
            command.AttachmentType,
            sanitizedFileName,
            storageKey,
            validation.ContentType ?? "application/octet-stream",
            fileBytes.Length,
            sha256,
            command.Description,
            nowUtc);

        _dbContext.ClinicalAttachments.Add(attachment);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _blobStorage.DeleteAsync(ContainerName, storageKey, cancellationToken);
            throw;
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AttachmentUpload",
            attachment.Id.ToString(),
            AuditOutcome.Success,
            "Klinik ek yüklendi ve MOCK kötü amaçlı içerik kontrolünden geçti.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(attachment));
    }

    public async Task<ClinicalEncounterOperationResult<AttachmentFileDownloadResult>> DownloadAttachmentAsync(
        ClaimsPrincipal actor,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var attachment = await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment is null)
        {
            return ClinicalEncounterOperationResult.NotFound<AttachmentFileDownloadResult>(
                "Klinik dosya kaydı bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(attachment.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken)
            || (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, attachment.PatientId)
                && attachment.IsEnteredInError))
        {
            return ClinicalEncounterOperationResult.Forbidden<AttachmentFileDownloadResult>(
                "Bu dosyayı indirme yetkiniz bulunmamaktadır.");
        }

        var stream = await _blobStorage.DownloadAsync(ContainerName, attachment.StorageKey, cancellationToken);
        if (stream is null)
        {
            return ClinicalEncounterOperationResult.NotFound<AttachmentFileDownloadResult>(
                "Depolama alanında dosya içeriği bulunamadı.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AttachmentDownload",
            attachment.Id.ToString(),
            AuditOutcome.Success,
            "Klinik ek indirildi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(new AttachmentFileDownloadResult(
            stream,
            attachment.ContentType,
            attachment.FileName,
            attachment.ByteSize));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> GetAttachmentMetadataAsync(
        ClaimsPrincipal actor,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var attachment = await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);

        if (attachment is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalAttachmentDto>(
                "Klinik dosya kaydı bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(attachment.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken)
            || (ClinicalRecordAccessControl.IsPatientOwnRecord(actor, attachment.PatientId)
                && attachment.IsEnteredInError))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalAttachmentDto>();
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AttachmentView",
            attachment.Id.ToString(),
            AuditOutcome.Success,
            "Klinik ek metaverisi görüntülendi.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(attachment));
    }

    public async Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkAttachmentEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExpectedVersion <= 0)
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "expectedVersion", "Beklenen kayıt sürümü zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return ClinicalEncounterOperationResult.Validation<ClinicalAttachmentDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        var attachment = await _dbContext.ClinicalAttachments
            .FirstOrDefaultAsync(a => a.Id == command.AttachmentId, cancellationToken);

        if (attachment is null)
        {
            return ClinicalEncounterOperationResult.NotFound<ClinicalAttachmentDto>(
                "Klinik dosya kaydı bulunamadı.");
        }

        var encounter = await _accessControl.FindEncounterAsync(attachment.EncounterId, cancellationToken);
        if (encounter is null
            || !await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.ClinicalNoteCorrect,
                allowPatientOwnRecord: false,
                cancellationToken)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var practitionerId))
        {
            return ClinicalEncounterOperationResult.Forbidden<ClinicalAttachmentDto>(
                "Dosyayı hatalı giriş olarak işaretleme izniniz veya kaynak kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.Entry(attachment).Property(a => a.Version).OriginalValue = command.ExpectedVersion;
        attachment.MarkEnteredInError(practitionerId, command.Reason, nowUtc);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return ClinicalEncounterOperationResult.Conflict<ClinicalAttachmentDto>(
                "Klinik ek başka bir kullanıcı tarafından değiştirildi. Güncel sürümü yükleyip yeniden deneyin.");
        }

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AttachmentEnteredInError",
            attachment.Id.ToString(),
            AuditOutcome.Success,
            "Klinik ek gerekçeli olarak hatalı giriş durumuna alındı.",
            cancellationToken);

        return ClinicalEncounterOperationResult.Success(MapToDto(attachment));
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalAttachmentDto>>> GetEncounterAttachmentsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (encounterId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ClinicalAttachmentDto>>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        var encounter = await _accessControl.FindEncounterAsync(encounterId, cancellationToken);
        if (encounter is null)
        {
            return ClinicalEncounterOperationResult.NotFound<IReadOnlyList<ClinicalAttachmentDto>>(
                "Karşılaşma bulunamadı.");
        }

        if (!await _accessControl.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ClinicalAttachmentDto>>();
        }

        var isPatientOwnRecord = ClinicalRecordAccessControl.IsPatientOwnRecord(actor, encounter.PatientId);

        var attachments = await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .Where(a => a.EncounterId == encounterId
                && (!isPatientOwnRecord || !a.IsEnteredInError))
            .OrderByDescending(a => a.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = attachments.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ClinicalAttachmentDto>>(dtos);
    }

    public async Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalAttachmentDto>>> GetPatientAttachmentsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (patientId == Guid.Empty)
        {
            return ClinicalEncounterOperationResult.Validation<IReadOnlyList<ClinicalAttachmentDto>>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (!await _accessControl.CanAccessPatientAsync(
                actor,
                patientId,
                HospitalPermissions.ClinicalRecords.EncounterView,
                allowPatientOwnRecord: true,
                cancellationToken))
        {
            return ClinicalEncounterOperationResult.Forbidden<IReadOnlyList<ClinicalAttachmentDto>>();
        }

        var isPatientOwnRecord = ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId);

        var attachments = await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId
                && (!isPatientOwnRecord || !a.IsEnteredInError))
            .OrderByDescending(a => a.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "ClinicalRecords.AttachmentView",
            patientId.ToString(),
            AuditOutcome.Success,
            "Hasta klinik ekleri listelendi.",
            cancellationToken);

        var dtos = attachments.Select(MapToDto).ToList();
        return ClinicalEncounterOperationResult.Success<IReadOnlyList<ClinicalAttachmentDto>>(dtos);
    }

    private static async Task<byte[]?> ReadWithLimitAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        const int bufferSize = 81920;
        var buffer = new byte[bufferSize];
        await using var destination = new MemoryStream();

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
            {
                return destination.ToArray();
            }

            if (destination.Length + bytesRead > AttachmentSecurityValidator.MaxFileSizeBytes)
            {
                return null;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
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

    private static ClinicalAttachmentDto MapToDto(ClinicalAttachment a)
    {
        return new ClinicalAttachmentDto(
            a.Id,
            a.EncounterId,
            a.PatientId,
            a.UploadedByPractitionerId,
            a.AttachmentType,
            a.FileName,
            a.ContentType,
            a.ByteSize,
            a.Sha256Checksum,
            a.Description,
            a.UploadedAtUtc,
            a.IsEnteredInError,
            a.EnteredInErrorReason,
            a.Version);
    }
}
