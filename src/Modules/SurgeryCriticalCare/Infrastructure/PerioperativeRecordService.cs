using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class PerioperativeRecordService : IPerioperativeRecordService
{
    private readonly SurgeryDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public PerioperativeRecordService(
        SurgeryDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SurgeryOperationResult<PerioperativeRecordDto>> SaveRecordAsync(
        SavePerioperativeRecordDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.SurgeryBookingId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<PerioperativeRecordDto>("SurgeryBookingId", "Ameliyat randevusu seçilmelidir.");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == dto.SurgeryBookingId, cancellationToken);
        if (booking is null)
        {
            return SurgeryOperationResult.NotFound<PerioperativeRecordDto>("Ameliyat randevusu bulunamadı.");
        }

        if (booking.Status == SurgeryBookingStatus.Cancelled)
        {
            return SurgeryOperationResult.Conflict<PerioperativeRecordDto>("İptal edilmiş bir ameliyat için perioperatif kayıt tutulamaz.");
        }

        var record = await _dbContext.PerioperativeRecords
            .Include(r => r.Corrections)
            .FirstOrDefaultAsync(r => r.SurgeryBookingId == dto.SurgeryBookingId, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (record is null)
        {
            record = PerioperativeRecord.Create(
                Guid.NewGuid(),
                booking.Id,
                booking.PatientId,
                booking.OperatingRoomId,
                dto.AnesthesiaType,
                dto.PostOpDisposition,
                now);

            _dbContext.PerioperativeRecords.Add(record);
        }

        if (record.IsSigned)
        {
            return SurgeryOperationResult.Conflict<PerioperativeRecordDto>(
                "İmzalı perioperatif kayıt doğrudan düzenlenemez. Lütfen düzeltme / ek not ekleyiniz.");
        }

        try
        {
            record.UpdateDraft(
                dto.RoomEntryTimeUtc,
                dto.AnesthesiaStartTimeUtc,
                dto.IncisionTimeUtc,
                dto.ClosureTimeUtc,
                dto.AnesthesiaEndTimeUtc,
                dto.RoomExitTimeUtc,
                dto.AnesthesiaType,
                dto.AnesthesiaNotes,
                dto.IntraoperativeFindings,
                dto.IntraoperativeComplications,
                dto.EstimatedBloodLossMl,
                dto.SpecimensCollected,
                dto.CountsConfirmed,
                dto.PostOpDisposition,
                dto.PostOpInstructions,
                now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<PerioperativeRecordDto>("PerioperativeTimeSequence", ex.Message);
        }

        // Automatic booking status synchronization
        if (dto.IncisionTimeUtc.HasValue && booking.Status is SurgeryBookingStatus.Scheduled or SurgeryBookingStatus.PreOpCleared)
        {
            booking.MarkInProgress(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.PerioperativeRecordSave",
            record.Id.ToString(),
            "Perioperatif kayıt taslağı güncellendi",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                RecordId = record.Id,
                BookingId = booking.Id,
                CountsConfirmed = dto.CountsConfirmed,
                PostOpDisposition = dto.PostOpDisposition.ToString(),
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(record));
    }

    public async Task<SurgeryOperationResult<PerioperativeRecordDto>> SignRecordAsync(
        Guid recordId,
        Guid signingDoctorId,
        CancellationToken cancellationToken = default)
    {
        if (signingDoctorId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<PerioperativeRecordDto>("SigningDoctorId", "İmzalayan hekim seçilmelidir.");
        }

        var record = await _dbContext.PerioperativeRecords
            .Include(r => r.Corrections)
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

        if (record is null)
        {
            return SurgeryOperationResult.NotFound<PerioperativeRecordDto>("Perioperatif kayıt bulunamadı.");
        }

        if (record.IsSigned)
        {
            return SurgeryOperationResult.Conflict<PerioperativeRecordDto>("Bu kayıt zaten imzalanmıştır.");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == record.SurgeryBookingId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            record.Sign(signingDoctorId, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<PerioperativeRecordDto>("Sign", ex.Message);
        }

        if (booking != null)
        {
            booking.MarkCompleted(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.PerioperativeRecordSign",
            record.Id.ToString(),
            "Perioperatif ameliyat kaydı resmî olarak imzalandı ve kilitlendi",
            signingDoctorId,
            JsonSerializer.Serialize(new
            {
                RecordId = record.Id,
                BookingId = record.SurgeryBookingId,
                SignedByDoctorId = signingDoctorId,
                SignedAtUtc = now,
                PreOpCleared = booking?.PreOpChecklist?.IsFullyCleared == true,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(record));
    }

    public async Task<SurgeryOperationResult<PerioperativeCorrectionDto>> AddCorrectionAsync(
        Guid recordId,
        AddPerioperativeCorrectionDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (doctorId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<PerioperativeCorrectionDto>("DoctorId", "Düzeltmeyi yapan hekim seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.ReasonForCorrection))
        {
            return SurgeryOperationResult.Validation<PerioperativeCorrectionDto>("ReasonForCorrection", "Düzeltme gerekçesi belirtilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.CorrectionNote))
        {
            return SurgeryOperationResult.Validation<PerioperativeCorrectionDto>("CorrectionNote", "Düzeltme notu boş olamaz.");
        }

        var record = await _dbContext.PerioperativeRecords
            .Include(r => r.Corrections)
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

        if (record is null)
        {
            return SurgeryOperationResult.NotFound<PerioperativeCorrectionDto>("Perioperatif kayıt bulunamadı.");
        }

        if (!record.IsSigned)
        {
            return SurgeryOperationResult.Conflict<PerioperativeCorrectionDto>(
                "Kayıt henüz imzalanmamıştır. Taslak üzerinde doğrudan değişiklik yapabilirsiniz.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var correction = record.AddCorrection(
            Guid.NewGuid(),
            doctorId,
            dto.ReasonForCorrection,
            dto.CorrectionNote,
            now);

        _dbContext.PerioperativeCorrections.Add(correction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.PerioperativeRecordCorrect",
            record.Id.ToString(),
            "İmzalı perioperatif kayda düzeltme/ek not eklendi",
            doctorId,
            JsonSerializer.Serialize(new
            {
                RecordId = record.Id,
                CorrectionId = correction.Id,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(new PerioperativeCorrectionDto(
            correction.Id,
            correction.PerioperativeRecordId,
            correction.CorrectedByDoctorId,
            correction.CorrectedAtUtc,
            correction.ReasonForCorrection,
            correction.CorrectionNote));
    }

    public async Task<PerioperativeRecordDto?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PerioperativeRecords
            .AsNoTracking()
            .Include(r => r.Corrections)
            .FirstOrDefaultAsync(r => r.SurgeryBookingId == bookingId, cancellationToken);

        return record is null ? null : MapToDto(record);
    }

    public async Task<PerioperativeRecordDto?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PerioperativeRecords
            .AsNoTracking()
            .Include(r => r.Corrections)
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);

        return record is null ? null : MapToDto(record);
    }

    private static PerioperativeRecordDto MapToDto(PerioperativeRecord r) =>
        new(
            r.Id,
            r.SurgeryBookingId,
            r.PatientId,
            r.OperatingRoomId,
            r.RoomEntryTimeUtc,
            r.AnesthesiaStartTimeUtc,
            r.IncisionTimeUtc,
            r.ClosureTimeUtc,
            r.AnesthesiaEndTimeUtc,
            r.RoomExitTimeUtc,
            r.AnesthesiaType,
            r.AnesthesiaNotes,
            r.IntraoperativeFindings,
            r.IntraoperativeComplications,
            r.EstimatedBloodLossMl,
            r.SpecimensCollected,
            r.CountsConfirmed,
            r.PostOpDisposition,
            r.PostOpInstructions,
            r.IsSigned,
            r.SignedByDoctorId,
            r.SignedAtUtc,
            r.Corrections.Select(c => new PerioperativeCorrectionDto(
                c.Id,
                c.PerioperativeRecordId,
                c.CorrectedByDoctorId,
                c.CorrectedAtUtc,
                c.ReasonForCorrection,
                c.CorrectionNote)).ToList(),
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.Version);

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
            TargetResourceType: "PerioperativeRecord",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
