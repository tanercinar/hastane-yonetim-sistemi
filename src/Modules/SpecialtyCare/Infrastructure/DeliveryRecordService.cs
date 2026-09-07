using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class DeliveryRecordService : IDeliveryRecordService
{
    private const string NewbornPatientUniqueConstraint = "UX_NewbornRecords_NewbornPatientId";
    private readonly SpecialtyCareDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public DeliveryRecordService(
        SpecialtyCareDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SpecialtyOperationResult<DeliveryRecordDto>> CreateDeliveryRecordAsync(
        CreateDeliveryRecordDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.MotherPatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DeliveryRecordDto>("MotherPatientId", "Anne hasta seçilmelidir.");
        }

        if (dto.AttendingDoctorId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<DeliveryRecordDto>("AttendingDoctorId", "Sorumlu hekim seçilmelidir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        DeliveryRecord delivery;
        try
        {
            delivery = DeliveryRecord.Create(
                Guid.NewGuid(),
                dto.PregnancyEpisodeId,
                dto.MotherPatientId,
                dto.EncounterId,
                dto.DeliveryMode,
                dto.DeliveryTimeUtc == default ? now : dto.DeliveryTimeUtc,
                dto.GestationalAgeWeeks,
                dto.GestationalAgeDays,
                dto.PerinealTear,
                dto.EstimatedBloodLossMl,
                dto.AttendingDoctorId,
                dto.AssistingMidwifeId,
                dto.PediatricianDoctorId,
                dto.MaternalComplicationsNotes,
                dto.DeliverySummaryNotes,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<DeliveryRecordDto>("Delivery", ex.Message);
        }

        _dbContext.DeliveryRecords.Add(delivery);

        // Add any provided newborns
        if (dto.Newborns is { Count: > 0 })
        {
            foreach (var nb in dto.Newborns)
            {
                try
                {
                    var newborn = delivery.AddNewborn(
                        Guid.NewGuid(),
                        nb.NewbornPatientId,
                        nb.BirthOrder,
                        nb.BirthTimeUtc == default ? delivery.DeliveryTimeUtc : nb.BirthTimeUtc,
                        nb.Gender,
                        nb.BirthWeightGrams,
                        nb.BirthLengthCm,
                        nb.HeadCircumferenceCm,
                        nb.ApgarScore1Min,
                        nb.ApgarScore5Min,
                        nb.ApgarScore10Min,
                        nb.ResuscitationGiven,
                        nb.CordBloodPh,
                        nb.ComplicationsNotes,
                        now);

                    _dbContext.NewbornRecords.Add(newborn);
                }
                catch (Exception ex)
                {
                    return SpecialtyOperationResult.Validation<DeliveryRecordDto>("Newborn", ex.Message);
                }
            }
        }

        // If pregnancy episode is linked, optionally mark it as Delivered
        if (dto.PregnancyEpisodeId.HasValue && dto.PregnancyEpisodeId.Value != Guid.Empty)
        {
            var episode = await _dbContext.PregnancyEpisodes
                .FirstOrDefaultAsync(p => p.Id == dto.PregnancyEpisodeId.Value, cancellationToken);

            if (episode is { Status: PregnancyEpisodeStatus.Active })
            {
                episode.CompleteEpisode(PregnancyEpisodeStatus.Delivered, now);
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsNewbornPatientConflict(ex))
        {
            return SpecialtyOperationResult.Conflict<DeliveryRecordDto>(
                "Yenidoğan Patient kimliği başka bir doğum kaydına bağlanmıştır.");
        }

        await PublishAuditAsync(
            "Specialty.DeliveryRecordCreate",
            delivery.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(delivery));
    }

    public async Task<SpecialtyOperationResult<NewbornDto>> AddNewbornAsync(
        Guid deliveryRecordId,
        AddNewbornDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var delivery = await _dbContext.DeliveryRecords
            .Include(d => d.Newborns)
            .FirstOrDefaultAsync(d => d.Id == deliveryRecordId, cancellationToken);

        if (delivery is null)
        {
            return SpecialtyOperationResult.NotFound<NewbornDto>("Doğum kaydı bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        NewbornRecord baby;
        try
        {
            baby = delivery.AddNewborn(
                Guid.NewGuid(),
                dto.NewbornPatientId,
                dto.BirthOrder,
                dto.BirthTimeUtc == default ? delivery.DeliveryTimeUtc : dto.BirthTimeUtc,
                dto.Gender,
                dto.BirthWeightGrams,
                dto.BirthLengthCm,
                dto.HeadCircumferenceCm,
                dto.ApgarScore1Min,
                dto.ApgarScore5Min,
                dto.ApgarScore10Min,
                dto.ResuscitationGiven,
                dto.CordBloodPh,
                dto.ComplicationsNotes,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<NewbornDto>("Newborn", ex.Message);
        }

        _dbContext.NewbornRecords.Add(baby);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsNewbornPatientConflict(ex))
        {
            return SpecialtyOperationResult.Conflict<NewbornDto>(
                "Yenidoğan Patient kimliği başka bir doğum kaydına bağlanmıştır.");
        }

        await PublishAuditAsync(
            "Specialty.NewbornRecordAdd",
            baby.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapNewbornToDto(baby));
    }

    public async Task<DeliveryRecordDto?> GetDeliveryRecordByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var delivery = await _dbContext.DeliveryRecords
            .AsNoTracking()
            .Include(d => d.Newborns)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return delivery is null ? null : MapToDto(delivery);
    }

    public async Task<List<DeliveryRecordDto>> GetDeliveryRecordsByMotherPatientIdAsync(Guid motherPatientId, CancellationToken cancellationToken = default)
    {
        var deliveries = await _dbContext.DeliveryRecords
            .AsNoTracking()
            .Include(d => d.Newborns)
            .Where(d => d.MotherPatientId == motherPatientId)
            .OrderByDescending(d => d.DeliveryTimeUtc)
            .ToListAsync(cancellationToken);

        return deliveries.Select(MapToDto).ToList();
    }

    private static DeliveryRecordDto MapToDto(DeliveryRecord d) =>
        new(
            d.Id,
            d.PregnancyEpisodeId,
            d.MotherPatientId,
            d.EncounterId,
            d.DeliveryProtocolNumber,
            d.DeliveryMode,
            d.DeliveryTimeUtc,
            d.GestationalAgeWeeks,
            d.GestationalAgeDays,
            d.PerinealTear,
            d.EstimatedBloodLossMl,
            d.AttendingDoctorId,
            d.AssistingMidwifeId,
            d.PediatricianDoctorId,
            d.MaternalComplicationsNotes,
            d.DeliverySummaryNotes,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Newborns.OrderBy(n => n.BirthOrder).Select(MapNewbornToDto).ToList());

    private static NewbornDto MapNewbornToDto(NewbornRecord n) =>
        new(
            n.Id,
            n.DeliveryRecordId,
            n.NewbornPatientId,
            n.BirthOrder,
            n.BirthTimeUtc,
            n.Gender,
            n.BirthWeightGrams,
            n.BirthLengthCm,
            n.HeadCircumferenceCm,
            n.ApgarScore1Min,
            n.ApgarScore5Min,
            n.ApgarScore10Min,
            n.ResuscitationGiven,
            n.CordBloodPh,
            n.ComplicationsNotes,
            n.CreatedAtUtc);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        Guid actorUserId,
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
            TargetResourceType: "DeliveryRecord",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: "Uzmanlık klinik kayıt eylemi tamamlandı.",
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static bool IsNewbornPatientConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: NewbornPatientUniqueConstraint,
        };
}
