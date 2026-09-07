using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class ProjectionRebuilder : IProjectionRebuilder
{
    private static readonly string[] SupportedProjections =
    [
        "DailyOutpatient",
        "DiagnosticWorkload",
        "BedOccupancy",
        "PharmacyDispensing",
    ];

    private readonly ReportingDbContext _dbContext;
    private readonly IReportingProjectionEngine _projectionEngine;
    private readonly TimeProvider _timeProvider;

    public ProjectionRebuilder(
        ReportingDbContext dbContext,
        IReportingProjectionEngine projectionEngine,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _projectionEngine = projectionEngine ?? throw new ArgumentNullException(nameof(projectionEngine));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RebuildSummaryDto> RebuildAllProjectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var rebuiltList = new List<string>();

        foreach (var projectionName in SupportedProjections)
        {
            await RebuildSingleInternalAsync(projectionName, cancellationToken);
            rebuiltList.Add(projectionName);
        }

        return new RebuildSummaryDto(
            Success: true,
            TotalProjectionsRebuilt: rebuiltList.Count,
            RebuiltProjections: rebuiltList,
            CompletedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            Message: "Tüm raporlama projeksiyonları başarıyla sıfırlandı ve yeniden kuruldu.");
    }

    public async Task<RebuildSummaryDto> RebuildProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);

        var normalizedName = projectionName.Trim();
        var match = SupportedProjections.FirstOrDefault(
            p => p.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return new RebuildSummaryDto(
                Success: false,
                TotalProjectionsRebuilt: 0,
                RebuiltProjections: [],
                CompletedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
                Message: $"Geçersiz projeksiyon adı: '{projectionName}'. Desteklenen projeksiyonlar: {string.Join(", ", SupportedProjections)}");
        }

        await RebuildSingleInternalAsync(match, cancellationToken);
        return new RebuildSummaryDto(
            Success: true,
            TotalProjectionsRebuilt: 1,
            RebuiltProjections: [match],
            CompletedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            Message: $"'{match}' projeksiyonu başarıyla sıfırlandı ve yeniden kuruldu.");
    }

    private async Task RebuildSingleInternalAsync(
        string projectionName,
        CancellationToken cancellationToken)
    {
        var checkpoint = await _dbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(c => c.ProjectionName == projectionName, cancellationToken);

        if (checkpoint is null)
        {
            checkpoint = new ProjectionCheckpoint(projectionName);
            _dbContext.ProjectionCheckpoints.Add(checkpoint);
        }

        checkpoint.MarkRebuilding();

        var sourceEvents = await _dbContext.ProjectionSourceEvents
            .AsNoTracking()
            .Where(sourceEvent => sourceEvent.ProjectionName == projectionName)
            .OrderBy(sourceEvent => sourceEvent.Position)
            .ToListAsync(cancellationToken);

        // Remove processed events for this projection
        var eventsToRemove = await _dbContext.ProjectionProcessedEvents
            .Where(e => e.ProjectionName == projectionName)
            .ToListAsync(cancellationToken);

        _dbContext.ProjectionProcessedEvents.RemoveRange(eventsToRemove);

        // Clear projection tables according to projection name
        switch (projectionName)
        {
            case "DailyOutpatient":
                var outpatientRows = await _dbContext.DailyOutpatientMetrics.ToListAsync(cancellationToken);
                _dbContext.DailyOutpatientMetrics.RemoveRange(outpatientRows);
                break;

            case "DiagnosticWorkload":
                var diagRows = await _dbContext.DiagnosticWorkloadMetrics.ToListAsync(cancellationToken);
                _dbContext.DiagnosticWorkloadMetrics.RemoveRange(diagRows);
                break;

            case "BedOccupancy":
                var bedRows = await _dbContext.BedOccupancyMetrics.ToListAsync(cancellationToken);
                _dbContext.BedOccupancyMetrics.RemoveRange(bedRows);
                break;

            case "PharmacyDispensing":
                var pharmacyRows = await _dbContext.PharmacyDispensingMetrics.ToListAsync(cancellationToken);
                _dbContext.PharmacyDispensingMetrics.RemoveRange(pharmacyRows);
                break;
        }

        checkpoint.MarkActive(0, _timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var sourceEvent in sourceEvents)
        {
            await ReplaySourceEventAsync(sourceEvent, cancellationToken);
        }
    }

    private Task<bool> ReplaySourceEventAsync(
        ProjectionSourceEvent sourceEvent,
        CancellationToken cancellationToken) => sourceEvent.EventType switch
        {
            nameof(AppointmentProjectedEvent) => _projectionEngine.ProjectAppointmentEventAsync(
                Deserialize<AppointmentProjectedEvent>(sourceEvent),
                cancellationToken),
            nameof(DiagnosticProjectedEvent) => _projectionEngine.ProjectDiagnosticEventAsync(
                Deserialize<DiagnosticProjectedEvent>(sourceEvent),
                cancellationToken),
            nameof(BedOccupancyProjectedEvent) => _projectionEngine.ProjectBedOccupancyEventAsync(
                Deserialize<BedOccupancyProjectedEvent>(sourceEvent),
                cancellationToken),
            nameof(PharmacyProjectedEvent) => _projectionEngine.ProjectPharmacyEventAsync(
                Deserialize<PharmacyProjectedEvent>(sourceEvent),
                cancellationToken),
            _ => throw new InvalidOperationException(
                $"Desteklenmeyen raporlama kaynak olay türü: {sourceEvent.EventType}"),
        };

    private static TEvent Deserialize<TEvent>(ProjectionSourceEvent sourceEvent) =>
        JsonSerializer.Deserialize<TEvent>(sourceEvent.PayloadJson)
        ?? throw new InvalidOperationException(
            $"Raporlama kaynak olayı okunamadı. EventId={sourceEvent.EventId}, Type={sourceEvent.EventType}");
}
