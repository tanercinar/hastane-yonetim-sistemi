using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class PregnancyTrackingService : IPregnancyTrackingService
{
    private readonly SpecialtyCareDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public PregnancyTrackingService(
        SpecialtyCareDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SpecialtyOperationResult<PregnancyEpisodeDto>> CreateEpisodeAsync(
        CreatePregnancyEpisodeDto dto,
        Guid creatingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<PregnancyEpisodeDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (dto.OpeningEncounterId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<PregnancyEpisodeDto>("OpeningEncounterId", "Başlangıç karşılaşması seçilmelidir.");
        }

        if (dto.Gravida < 1)
        {
            return SpecialtyOperationResult.Validation<PregnancyEpisodeDto>("Gravida", "Gravida sayısı en az 1 olmalıdır.");
        }

        if (dto.Para < 0 || dto.Abortus < 0 || dto.LivingChildren < 0)
        {
            return SpecialtyOperationResult.Validation<PregnancyEpisodeDto>("ObstetricHistory", "Doğum, düşük ve yaşayan çocuk sayıları negatif olamaz.");
        }

        var activeEpisodeExists = await _dbContext.PregnancyEpisodes
            .AnyAsync(p => p.PatientId == dto.PatientId && p.Status == PregnancyEpisodeStatus.Active, cancellationToken);

        if (activeEpisodeExists)
        {
            return SpecialtyOperationResult.Conflict<PregnancyEpisodeDto>("Bu hasta için hâlihazırda aktif bir gebelik takip süreci bulunmaktadır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var episode = PregnancyEpisode.Create(
            Guid.NewGuid(),
            dto.PatientId,
            dto.OpeningEncounterId,
            dto.Gravida,
            dto.Para,
            dto.Abortus,
            dto.LivingChildren,
            dto.LastMenstrualPeriodUtc,
            dto.EstimatedDeliveryDateUtc,
            dto.BloodGroupAndRh,
            dto.RiskCategory,
            dto.RiskFactorsNotes,
            dto.AssignedDoctorId,
            dto.AssignedMidwifeId,
            now);

        _dbContext.PregnancyEpisodes.Add(episode);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.PregnancyEpisodeCreate",
            episode.Id.ToString(),
            creatingStaffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(episode));
    }

    public async Task<SpecialtyOperationResult<AntenatalVisitDto>> RecordAntenatalVisitAsync(
        RecordAntenatalVisitDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var episode = await _dbContext.PregnancyEpisodes
            .Include(p => p.AntenatalVisits)
            .FirstOrDefaultAsync(p => p.Id == dto.PregnancyEpisodeId, cancellationToken);

        if (episode is null)
        {
            return SpecialtyOperationResult.NotFound<AntenatalVisitDto>("Gebelik takip kaydı bulunamadı.");
        }

        if (episode.Status != PregnancyEpisodeStatus.Active)
        {
            return SpecialtyOperationResult.Conflict<AntenatalVisitDto>("Yalnızca aktif gebelik takiplerine antenatal vizit kaydedilebilir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        AntenatalVisit visit;
        try
        {
            visit = episode.RecordAntenatalVisit(
                Guid.NewGuid(),
                dto.EncounterId,
                dto.VisitDateUtc,
                dto.GestationalAgeWeeks,
                dto.GestationalAgeDays,
                dto.MaternalWeightKg,
                dto.SystolicBpMmHg,
                dto.DiastolicBpMmHg,
                dto.FundalHeightCm,
                dto.FetalHeartRateBpm,
                dto.FetalPresentation,
                dto.EdemaLevel,
                dto.UrineProteinPresent,
                dto.UrineGlucosePresent,
                staffId,
                dto.ClinicalNotes,
                dto.NextVisitRecommendedDateUtc,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<AntenatalVisitDto>("Visit", ex.Message);
        }

        _dbContext.AntenatalVisits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.AntenatalVisitRecord",
            visit.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapVisitToDto(visit));
    }

    public async Task<SpecialtyOperationResult<PregnancyEpisodeDto>> UpdateRiskCategoryAsync(
        Guid episodeId,
        PregnancyRiskCategory newRiskCategory,
        string? riskNotes,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var episode = await _dbContext.PregnancyEpisodes
            .Include(p => p.AntenatalVisits)
            .FirstOrDefaultAsync(p => p.Id == episodeId, cancellationToken);

        if (episode is null)
        {
            return SpecialtyOperationResult.NotFound<PregnancyEpisodeDto>("Gebelik takip kaydı bulunamadı.");
        }

        if (episode.Status != PregnancyEpisodeStatus.Active)
        {
            return SpecialtyOperationResult.Conflict<PregnancyEpisodeDto>("Yalnızca aktif gebelik takiplerinin risk düzeyi güncellenebilir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        episode.UpdateRiskCategory(newRiskCategory, riskNotes, now);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.PregnancyEpisodeUpdate",
            episode.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(episode));
    }

    public async Task<SpecialtyOperationResult<PregnancyEpisodeDto>> CompleteEpisodeAsync(
        Guid episodeId,
        PregnancyEpisodeStatus outcomeStatus,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var episode = await _dbContext.PregnancyEpisodes
            .Include(p => p.AntenatalVisits)
            .FirstOrDefaultAsync(p => p.Id == episodeId, cancellationToken);

        if (episode is null)
        {
            return SpecialtyOperationResult.NotFound<PregnancyEpisodeDto>("Gebelik takip kaydı bulunamadı.");
        }

        if (episode.Status != PregnancyEpisodeStatus.Active)
        {
            return SpecialtyOperationResult.Conflict<PregnancyEpisodeDto>("Gebelik takibi zaten aktif durumda değildir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            episode.CompleteEpisode(outcomeStatus, now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<PregnancyEpisodeDto>("Outcome", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.PregnancyEpisodeComplete",
            episode.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(episode));
    }

    public async Task<PregnancyEpisodeDto?> GetEpisodeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var episode = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .Include(p => p.AntenatalVisits)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return episode is null ? null : MapToDto(episode);
    }

    public async Task<List<PregnancyEpisodeDto>> GetEpisodesByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var episodes = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .Include(p => p.AntenatalVisits)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return episodes.Select(MapToDto).ToList();
    }

    public async Task<List<PregnancyEpisodeDto>> GetActiveEpisodesAsync(CancellationToken cancellationToken = default)
    {
        var episodes = await _dbContext.PregnancyEpisodes
            .AsNoTracking()
            .Include(p => p.AntenatalVisits)
            .Where(p => p.Status == PregnancyEpisodeStatus.Active)
            .OrderBy(p => p.EstimatedDeliveryDateUtc)
            .ToListAsync(cancellationToken);

        return episodes.Select(MapToDto).ToList();
    }

    private static PregnancyEpisodeDto MapToDto(PregnancyEpisode e) =>
        new(
            e.Id,
            e.PatientId,
            e.OpeningEncounterId,
            e.EpisodeProtocolNumber,
            e.Gravida,
            e.Para,
            e.Abortus,
            e.LivingChildren,
            e.LastMenstrualPeriodUtc,
            e.EstimatedDeliveryDateUtc,
            e.BloodGroupAndRh,
            e.RiskCategory,
            e.RiskFactorsNotes,
            e.Status,
            e.AssignedDoctorId,
            e.AssignedMidwifeId,
            e.CreatedAtUtc,
            e.UpdatedAtUtc,
            e.AntenatalVisits.OrderBy(v => v.VisitDateUtc).Select(MapVisitToDto).ToList());

    private static AntenatalVisitDto MapVisitToDto(AntenatalVisit v) =>
        new(
            v.Id,
            v.PregnancyEpisodeId,
            v.EncounterId,
            v.VisitDateUtc,
            v.GestationalAgeWeeks,
            v.GestationalAgeDays,
            v.MaternalWeightKg,
            v.SystolicBpMmHg,
            v.DiastolicBpMmHg,
            v.FundalHeightCm,
            v.FetalHeartRateBpm,
            v.FetalPresentation,
            v.EdemaLevel,
            v.UrineProteinPresent,
            v.UrineGlucosePresent,
            v.StaffId,
            v.ClinicalNotes,
            v.NextVisitRecommendedDateUtc,
            v.CreatedAtUtc);

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
            TargetResourceType: "PregnancyEpisode",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: "Uzmanlık klinik kayıt eylemi tamamlandı.",
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
