using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class IcuFlowsheetService : IIcuFlowsheetService
{
    private readonly SurgeryDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public IcuFlowsheetService(
        SurgeryDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SurgeryOperationResult<IcuFlowsheetEntryDto>> AddFlowsheetEntryAsync(
        CreateIcuFlowsheetEntryDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.IcuAdmissionId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<IcuFlowsheetEntryDto>("IcuAdmissionId", "Yoğun bakım kabul ID boş olamaz.");
        }

        var admission = await _dbContext.IcuAdmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == dto.IcuAdmissionId, cancellationToken);

        if (admission is null)
        {
            return SurgeryOperationResult.NotFound<IcuFlowsheetEntryDto>("Yoğun bakım kabul kaydı bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var recordedAt = dto.RecordedAtUtc ?? now;

        IcuFlowsheetEntry entry;
        try
        {
            entry = IcuFlowsheetEntry.Create(
                Guid.NewGuid(),
                dto.IcuAdmissionId,
                recordedAt,
                requestingStaffId,
                dto.HeartRateBpm,
                dto.SystolicBpMmHg,
                dto.DiastolicBpMmHg,
                dto.RespiratoryRateBpm,
                dto.OxygenSaturationPct,
                dto.BodyTemperatureCelsius,
                dto.GlasgowComaScale,
                dto.RichmondAgitationSedationScale,
                dto.VentilationMode,
                dto.FractionOfInspiredOxygenPct,
                dto.PositiveEndExpiratoryPressure,
                dto.TidalVolumeMl,
                dto.PeakInspiratoryPressure,
                dto.IvFluidIntakeMl,
                dto.EnteralNutritionIntakeMl,
                dto.UrineOutputMl,
                dto.DrainOutputMl,
                dto.ClinicalNotes,
                now);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return SurgeryOperationResult.Validation<IcuFlowsheetEntryDto>(ex.ParamName ?? "Flowsheet", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return SurgeryOperationResult.Validation<IcuFlowsheetEntryDto>(ex.ParamName ?? "Flowsheet", ex.Message);
        }

        _dbContext.IcuFlowsheetEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.IcuFlowsheetRecord",
            entry.Id.ToString(),
            "Yoğun bakım akış gözlemi kaydedildi",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                AdmissionId = admission.Id,
                EntryId = entry.Id,
                RecordedAtUtc = entry.RecordedAtUtc,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(entry));
    }

    public async Task<List<IcuFlowsheetEntryDto>> GetFlowsheetEntriesAsync(
        Guid icuAdmissionId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IcuFlowsheetEntries
            .AsNoTracking()
            .Where(e => e.IcuAdmissionId == icuAdmissionId);

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.RecordedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.RecordedAtUtc <= toUtc.Value);
        }

        var list = await query
            .OrderByDescending(e => e.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<IcuFluidBalanceSummaryDto> GetFluidBalanceSummaryAsync(
        Guid icuAdmissionId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var start = fromUtc ?? now.AddHours(-24);
        var end = toUtc ?? now;

        var entries = await _dbContext.IcuFlowsheetEntries
            .AsNoTracking()
            .Where(e => e.IcuAdmissionId == icuAdmissionId && e.RecordedAtUtc >= start && e.RecordedAtUtc <= end)
            .ToListAsync(cancellationToken);

        var totalIv = entries.Sum(e => e.IvFluidIntakeMl ?? 0);
        var totalEnteral = entries.Sum(e => e.EnteralNutritionIntakeMl ?? 0);
        var totalIntake = totalIv + totalEnteral;

        var totalUrine = entries.Sum(e => e.UrineOutputMl ?? 0);
        var totalDrain = entries.Sum(e => e.DrainOutputMl ?? 0);
        var totalOutput = totalUrine + totalDrain;

        var net = totalIntake - totalOutput;

        return new IcuFluidBalanceSummaryDto(
            icuAdmissionId,
            start,
            end,
            totalIv,
            totalEnteral,
            totalIntake,
            totalUrine,
            totalDrain,
            totalOutput,
            net,
            entries.Count);
    }

    private static IcuFlowsheetEntryDto MapToDto(IcuFlowsheetEntry e) =>
        new(
            e.Id,
            e.IcuAdmissionId,
            e.RecordedAtUtc,
            e.RecordedByStaffId,
            e.HeartRateBpm,
            e.SystolicBpMmHg,
            e.DiastolicBpMmHg,
            e.MeanArterialPressureMmHg,
            e.RespiratoryRateBpm,
            e.OxygenSaturationPct,
            e.BodyTemperatureCelsius,
            e.GlasgowComaScale,
            e.RichmondAgitationSedationScale,
            e.VentilationMode,
            e.FractionOfInspiredOxygenPct,
            e.PositiveEndExpiratoryPressure,
            e.TidalVolumeMl,
            e.PeakInspiratoryPressure,
            e.IvFluidIntakeMl,
            e.EnteralNutritionIntakeMl,
            e.UrineOutputMl,
            e.DrainOutputMl,
            e.TotalIntakeMl,
            e.TotalOutputMl,
            e.NetFluidBalanceMl,
            e.ClinicalNotes,
            e.CreatedAtUtc);

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
            TargetResourceType: "IcuFlowsheet",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
