using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientTransferService : IInpatientTransferService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;

    public InpatientTransferService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    }

    public async Task<InpatientOperationResult<InpatientTransferDto>> RequestTransferAsync(
        CreateTransferDto request,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == request.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Yatış kaydı bulunamadı.");
        }

        if (admission.Status != AdmissionStatus.Admitted)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                $"Yalnızca yatakta aktif ({AdmissionStatus.Admitted}) durumundaki hastalar için transfer istemi başlatılabilir. Mevcut durum: {admission.Status}");
        }

        if (!admission.AssignedBedId.HasValue)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>("Hastanın kayıtlı bir yatağı bulunmamaktadır.");
        }

        var hasPendingTransfer = await _dbContext.Transfers
            .AnyAsync(t => t.AdmissionId == admission.Id &&
                (t.Status == TransferStatus.Requested || t.Status == TransferStatus.Accepted), cancellationToken);

        if (hasPendingTransfer)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                "Bu yatış için hâlihazırda onay veya tamamlama bekleyen bir transfer süreci bulunmaktadır.");
        }

        var targetWard = await _dbContext.Wards
            .FirstOrDefaultAsync(w => w.Id == request.TargetWardId && w.IsActive, cancellationToken);
        if (targetWard is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Hedef servis bulunamadı veya pasif durumda.");
        }

        var now = DateTime.UtcNow;

        if (request.TargetBedId.HasValue && request.TargetBedId.Value != Guid.Empty)
        {
            var targetBed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == request.TargetBedId.Value && b.WardId == targetWard.Id && b.IsActive, cancellationToken);
            if (targetBed is null)
            {
                return InpatientOperationResult.NotFound<InpatientTransferDto>("Seçilen hedef yatak serviste bulunamadı veya pasif.");
            }

            if (targetBed.Status != BedStatus.Available)
            {
                return InpatientOperationResult.Conflict<InpatientTransferDto>(
                    $"Hedef yatak ({targetBed.BedNumber}) müsait değil. Mevcut durum: {targetBed.Status}");
            }

            targetBed.Reserve(admission.Id, admission.PatientId, now);
        }

        var transferId = Guid.NewGuid();
        var transfer = InpatientTransfer.Request(
            transferId,
            admission.Id,
            admission.PatientId,
            admission.AdmittingWardId,
            admission.AssignedBedId.Value,
            request.TargetWardId,
            request.TargetBedId,
            request.TransferReason,
            request.ClinicalNotes,
            requestedByUserId,
            now);

        try
        {
            admission.InitiateTransfer(now);
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(ex.Message);
        }

        _dbContext.Transfers.Add(transfer);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                "Bu yatış için başka bir eşzamanlı aktif transfer istemi oluşturuldu.");
        }

        await PublishAuditAsync(
            "Inpatient.TransferRequest",
            transfer.Id.ToString(),
            "Hasta transfer istemi oluşturuldu",
            requestedByUserId,
            JsonSerializer.Serialize(new
            {
                TransferId = transfer.Id,
                AdmissionId = admission.Id,
                SourceWardId = admission.AdmittingWardId,
                SourceBedId = admission.AssignedBedId,
                TargetWardId = request.TargetWardId,
                TargetBedId = request.TargetBedId,
            }),
            now,
            cancellationToken);

        var response = await BuildTransferDtoAsync(transfer, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientTransferDto>> AcceptTransferAsync(
        Guid transferId,
        AcceptTransferDto? request,
        Guid acceptedByUserId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);
        if (transfer is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Transfer kaydı bulunamadı.");
        }

        var now = DateTime.UtcNow;

        if (request?.TargetBedId.HasValue == true && request.TargetBedId.Value != Guid.Empty)
        {
            var targetBed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == request.TargetBedId.Value && b.WardId == transfer.TargetWardId && b.IsActive, cancellationToken);
            if (targetBed is null)
            {
                return InpatientOperationResult.NotFound<InpatientTransferDto>("Belirtilen hedef yatak serviste bulunamadı veya pasif.");
            }

            if (targetBed.Status != BedStatus.Available && !(targetBed.Status == BedStatus.Reserved && targetBed.CurrentAdmissionId == transfer.AdmissionId))
            {
                return InpatientOperationResult.Conflict<InpatientTransferDto>(
                    $"Hedef yatak ({targetBed.BedNumber}) müsait değil. Mevcut durum: {targetBed.Status}");
            }

            if (targetBed.Status == BedStatus.Available)
            {
                targetBed.Reserve(transfer.AdmissionId, transfer.PatientId, now);
            }
        }

        try
        {
            transfer.Accept(acceptedByUserId, request?.TargetBedId, now);
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                "Transfer başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }

        await PublishAuditAsync(
            "Inpatient.TransferAccept",
            transfer.Id.ToString(),
            "Transfer kabul edildi",
            acceptedByUserId,
            JsonSerializer.Serialize(new
            {
                TransferId = transfer.Id,
                AcceptedByUserId = acceptedByUserId,
                TargetBedId = request?.TargetBedId,
            }),
            now,
            cancellationToken);

        var response = await BuildTransferDtoAsync(transfer, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientTransferDto>> CompleteTransferAsync(
        Guid transferId,
        CompleteTransferDto request,
        Guid completedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await _dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);
        if (transfer is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Transfer kaydı bulunamadı.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == transfer.AdmissionId, cancellationToken);
        if (admission is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Yatış kaydı bulunamadı.");
        }

        var sourceBed = await _dbContext.Beds
            .FirstOrDefaultAsync(b => b.Id == transfer.SourceBedId, cancellationToken);

        var targetBedId = request.TargetBedId != Guid.Empty ? request.TargetBedId : transfer.TargetBedId ?? Guid.Empty;
        if (targetBedId == Guid.Empty)
        {
            return InpatientOperationResult.Validation<InpatientTransferDto>("TargetBedId", "Transferin tamamlanabilmesi için hedef yatak seçilmelidir.");
        }

        var targetBed = await _dbContext.Beds
            .FirstOrDefaultAsync(b => b.Id == targetBedId && b.WardId == transfer.TargetWardId && b.IsActive, cancellationToken);
        if (targetBed is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Hedef yatak bulunamadı veya hedef servise ait değil.");
        }

        var targetWard = await _dbContext.Wards
            .AsNoTracking()
            .SingleOrDefaultAsync(ward => ward.Id == transfer.TargetWardId && ward.IsActive, cancellationToken);
        if (targetWard is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Hedef servis bulunamadı veya pasif durumda.");
        }

        if (targetBed.Status != BedStatus.Available && !(targetBed.Status == BedStatus.Reserved && targetBed.CurrentAdmissionId == admission.Id))
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                $"Hedef yatak ({targetBed.BedNumber}) müsait değil. Mevcut durum: {targetBed.Status}");
        }

        var now = DateTime.UtcNow;

        try
        {
            // Atomik Yatak Geçişi:
            // 1. Eski yatak serbest bırakılır ve temizlik durumuna alınır
            if (sourceBed is not null)
            {
                sourceBed.ReleaseBed(now, requireCleaning: true);
            }

            // 2. Yeni yatak dolu durumuna geçirilir
            targetBed.AssignAdmission(admission.Id, admission.PatientId, now);

            // 3. Transfer kaydı tamamlandı yapılır
            transfer.Complete(completedByUserId, targetBed.Id, now);

            // 4. Yatış kaydı yeni servis ve yatakla Admitted durumuna güncellenir
            admission.CompleteTransfer(transfer.TargetWardId, targetWard.DepartmentId, targetBed.Id, now);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                "Transfer veya yatak başka bir eşzamanlı işlem tarafından güncellendi. Kaydı yeniden yükleyiniz.");
        }
        catch (DbUpdateException)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(
                "Hedef yatak veya aktif transfer başka bir eşzamanlı işlem tarafından ayrıldı.");
        }

        await PublishAuditAsync(
            "Inpatient.TransferComplete",
            transfer.Id.ToString(),
            "Hasta transferi başarıyla tamamlandı",
            completedByUserId,
            JsonSerializer.Serialize(new
            {
                TransferId = transfer.Id,
                AdmissionId = admission.Id,
                SourceWardId = transfer.SourceWardId,
                SourceBedId = transfer.SourceBedId,
                TargetWardId = transfer.TargetWardId,
                TargetBedId = targetBed.Id,
                CompletedByUserId = completedByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildTransferDtoAsync(transfer, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientOperationResult<InpatientTransferDto>> CancelTransferAsync(
        Guid transferId,
        CancelTransferDto request,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transfer = await _dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);
        if (transfer is null)
        {
            return InpatientOperationResult.NotFound<InpatientTransferDto>("Transfer kaydı bulunamadı.");
        }

        var admission = await _dbContext.Admissions
            .FirstOrDefaultAsync(a => a.Id == transfer.AdmissionId, cancellationToken);

        var now = DateTime.UtcNow;

        if (transfer.TargetBedId.HasValue)
        {
            var targetBed = await _dbContext.Beds
                .FirstOrDefaultAsync(b => b.Id == transfer.TargetBedId.Value, cancellationToken);
            if (targetBed is not null && targetBed.Status == BedStatus.Reserved && targetBed.CurrentAdmissionId == transfer.AdmissionId)
            {
                targetBed.CancelReservation(now);
            }
        }

        try
        {
            transfer.Cancel(cancelledByUserId, request.Reason, now);
            admission?.CancelTransfer(now);
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Conflict<InpatientTransferDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Inpatient.TransferCancel",
            transfer.Id.ToString(),
            "Transfer süreci iptal edildi",
            cancelledByUserId,
            JsonSerializer.Serialize(new
            {
                TransferId = transfer.Id,
                AdmissionId = transfer.AdmissionId,
                CancelledByUserId = cancelledByUserId,
            }),
            now,
            cancellationToken);

        var response = await BuildTransferDtoAsync(transfer, cancellationToken);
        return InpatientOperationResult.Success(response!);
    }

    public async Task<InpatientTransferDto?> GetTransferByIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _dbContext.Transfers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken);

        if (transfer is null)
        {
            return null;
        }

        return await BuildTransferDtoAsync(transfer, cancellationToken);
    }

    public async Task<List<InpatientTransferSummaryDto>> GetTransfersAsync(
        Guid? admissionId = null,
        Guid? sourceWardId = null,
        Guid? targetWardId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transfers.AsNoTracking();

        if (admissionId.HasValue && admissionId.Value != Guid.Empty)
        {
            query = query.Where(t => t.AdmissionId == admissionId.Value);
        }

        if (sourceWardId.HasValue && sourceWardId.Value != Guid.Empty)
        {
            query = query.Where(t => t.SourceWardId == sourceWardId.Value);
        }

        if (targetWardId.HasValue && targetWardId.Value != Guid.Empty)
        {
            query = query.Where(t => t.TargetWardId == targetWardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TransferStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(t => t.Status == parsedStatus);
        }

        var transfers = await query
            .OrderByDescending(t => t.RequestedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var wardIds = transfers.Select(t => t.SourceWardId)
            .Concat(transfers.Select(t => t.TargetWardId))
            .Distinct().ToList();

        var bedIds = transfers.Select(t => t.SourceBedId)
            .Concat(transfers.Where(t => t.TargetBedId.HasValue).Select(t => t.TargetBedId!.Value))
            .Distinct().ToList();

        var wards = await _dbContext.Wards
            .AsNoTracking()
            .Where(w => wardIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var beds = await _dbContext.Beds
            .AsNoTracking()
            .Where(b => bedIds.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        return transfers.Select(t =>
        {
            var sourceWardName = wards.GetValueOrDefault(t.SourceWardId, "Kaynak Servis");
            var targetWardName = wards.GetValueOrDefault(t.TargetWardId, "Hedef Servis");

            beds.TryGetValue(t.SourceBedId, out var srcBed);
            Bed? tgtBed = null;
            if (t.TargetBedId.HasValue)
            {
                beds.TryGetValue(t.TargetBedId.Value, out tgtBed);
            }

            return new InpatientTransferSummaryDto(
                t.Id,
                t.AdmissionId,
                t.PatientId,
                t.SourceWardId,
                sourceWardName,
                t.SourceBedId,
                srcBed?.BedNumber ?? "-",
                t.TargetWardId,
                targetWardName,
                t.TargetBedId,
                tgtBed?.BedNumber,
                t.TransferReason,
                t.Status,
                t.RequestedAtUtc,
                t.CompletedAtUtc);
        }).ToList();
    }

    private async Task<InpatientTransferDto?> BuildTransferDtoAsync(
        InpatientTransfer transfer,
        CancellationToken cancellationToken)
    {
        var sourceWard = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == transfer.SourceWardId, cancellationToken);

        var targetWard = await _dbContext.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == transfer.TargetWardId, cancellationToken);

        var sourceBed = await _dbContext.Beds
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == transfer.SourceBedId, cancellationToken);

        string? sourceRoomNumber = null;
        if (sourceBed is not null)
        {
            var room = await _dbContext.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == sourceBed.RoomId, cancellationToken);
            sourceRoomNumber = room?.RoomNumber;
        }

        Bed? targetBed = null;
        string? targetRoomNumber = null;
        if (transfer.TargetBedId.HasValue)
        {
            targetBed = await _dbContext.Beds
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == transfer.TargetBedId.Value, cancellationToken);
            if (targetBed is not null)
            {
                var room = await _dbContext.Rooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == targetBed.RoomId, cancellationToken);
                targetRoomNumber = room?.RoomNumber;
            }
        }

        return new InpatientTransferDto(
            transfer.Id,
            transfer.AdmissionId,
            transfer.PatientId,
            transfer.SourceWardId,
            sourceWard?.Name ?? "Kaynak Servis",
            transfer.SourceBedId,
            sourceBed?.BedNumber ?? "-",
            sourceRoomNumber,
            transfer.TargetWardId,
            targetWard?.Name ?? "Hedef Servis",
            transfer.TargetBedId,
            targetBed?.BedNumber,
            targetRoomNumber,
            transfer.TransferReason,
            transfer.ClinicalNotes,
            transfer.Status,
            transfer.RequestedByUserId,
            transfer.RequestedAtUtc,
            transfer.AcceptedByUserId,
            transfer.AcceptedAtUtc,
            transfer.CompletedByUserId,
            transfer.CompletedAtUtc,
            transfer.CancelledByUserId,
            transfer.CancelledAtUtc,
            transfer.CancellationReason,
            transfer.Version);
    }

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        string reason,
        Guid actorUserId,
        string? detailsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: actorUserId != Guid.Empty ? actorUserId : null,
            ActorPersonId: null,
            ActorRole: null,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "InpatientTransfer",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
