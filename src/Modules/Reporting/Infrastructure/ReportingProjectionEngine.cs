using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class ReportingProjectionEngine : IReportingProjectionEngine
{
    private const string OutpatientProjection = "DailyOutpatient";
    private const string DiagnosticProjection = "DiagnosticWorkload";
    private const string BedOccupancyProjection = "BedOccupancy";
    private const string PharmacyProjection = "PharmacyDispensing";

    private readonly ReportingDbContext _dbContext;
    private readonly IReportingRealtimeNotifier? _realtimeNotifier;

    public ReportingProjectionEngine(
        ReportingDbContext dbContext,
        IReportingRealtimeNotifier? realtimeNotifier = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<bool> ProjectAppointmentEventAsync(
        AppointmentProjectedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await EnsureSourceEventAsync(
            evt.EventId,
            OutpatientProjection,
            nameof(AppointmentProjectedEvent),
            evt,
            evt.OccurredAtUtc,
            cancellationToken);

        // Idempotency check: Deduplicate by EventId
        var alreadyProcessed = await _dbContext.ProjectionProcessedEvents
            .AnyAsync(e => e.EventId == evt.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return false;
        }

        var metric = await _dbContext.DailyOutpatientMetrics
            .FirstOrDefaultAsync(
                m => m.Date == evt.Date && m.DepartmentId == evt.DepartmentId && m.DoctorId == evt.DoctorId,
                cancellationToken);

        if (metric is null)
        {
            metric = new DailyOutpatientMetric(
                evt.Date,
                evt.DepartmentId,
                evt.DepartmentName,
                evt.DoctorId,
                evt.DoctorName,
                evt.OccurredAtUtc);

            _dbContext.DailyOutpatientMetrics.Add(metric);
        }

        metric.ApplyTransition(evt.PreviousStatus, evt.NewStatus, evt.OccurredAtUtc);

        _dbContext.ProjectionProcessedEvents.Add(
            new ProjectionProcessedEvent(evt.EventId, OutpatientProjection, evt.OccurredAtUtc));

        await UpdateCheckpointAsync(OutpatientProjection, evt.OccurredAtUtc, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_realtimeNotifier is not null)
        {
            await _realtimeNotifier.NotifyDashboardUpdatedAsync(
                "outpatient",
                evt.Date,
                evt.DepartmentId,
                cancellationToken);
        }

        return true;
    }

    public async Task<bool> ProjectDiagnosticEventAsync(
        DiagnosticProjectedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await EnsureSourceEventAsync(
            evt.EventId,
            DiagnosticProjection,
            nameof(DiagnosticProjectedEvent),
            evt,
            evt.OccurredAtUtc,
            cancellationToken);

        var alreadyProcessed = await _dbContext.ProjectionProcessedEvents
            .AnyAsync(e => e.EventId == evt.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return false;
        }

        var metric = await _dbContext.DiagnosticWorkloadMetrics
            .FirstOrDefaultAsync(
                m => m.Date == evt.Date && m.ModalityOrSection == evt.ModalityOrSection,
                cancellationToken);

        if (metric is null)
        {
            metric = new DiagnosticWorkloadMetric(
                evt.Date,
                evt.ModalityOrSection,
                evt.OccurredAtUtc);

            _dbContext.DiagnosticWorkloadMetrics.Add(metric);
        }

        metric.ApplyOrderTransition(
            evt.PreviousStatus,
            evt.NewStatus,
            evt.IsCritical,
            evt.TurnaroundMinutes,
            evt.OccurredAtUtc);

        _dbContext.ProjectionProcessedEvents.Add(
            new ProjectionProcessedEvent(evt.EventId, DiagnosticProjection, evt.OccurredAtUtc));

        await UpdateCheckpointAsync(DiagnosticProjection, evt.OccurredAtUtc, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_realtimeNotifier is not null)
        {
            await _realtimeNotifier.NotifyDashboardUpdatedAsync(
                "diagnostics",
                evt.Date,
                null,
                cancellationToken);
        }

        return true;
    }

    public async Task<bool> ProjectBedOccupancyEventAsync(
        BedOccupancyProjectedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await EnsureSourceEventAsync(
            evt.EventId,
            BedOccupancyProjection,
            nameof(BedOccupancyProjectedEvent),
            evt,
            evt.OccurredAtUtc,
            cancellationToken);

        var alreadyProcessed = await _dbContext.ProjectionProcessedEvents
            .AnyAsync(e => e.EventId == evt.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return false;
        }

        var metric = await _dbContext.BedOccupancyMetrics
            .FirstOrDefaultAsync(
                m => m.Date == evt.Date && m.DepartmentId == evt.DepartmentId && m.WardType == evt.WardType,
                cancellationToken);

        if (metric is null)
        {
            metric = new BedOccupancyMetric(
                evt.Date,
                evt.DepartmentId,
                evt.DepartmentName,
                evt.WardType,
                evt.TotalBeds,
                evt.OccupiedBeds,
                evt.PendingTransfers,
                evt.OccurredAtUtc);

            _dbContext.BedOccupancyMetrics.Add(metric);
        }
        else
        {
            metric.UpdateOccupancy(
                evt.TotalBeds,
                evt.OccupiedBeds,
                evt.PendingTransfers,
                evt.OccurredAtUtc);
        }

        _dbContext.ProjectionProcessedEvents.Add(
            new ProjectionProcessedEvent(evt.EventId, BedOccupancyProjection, evt.OccurredAtUtc));

        await UpdateCheckpointAsync(BedOccupancyProjection, evt.OccurredAtUtc, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_realtimeNotifier is not null)
        {
            await _realtimeNotifier.NotifyDashboardUpdatedAsync(
                "inpatient",
                evt.Date,
                evt.DepartmentId,
                cancellationToken);
        }

        return true;
    }

    public async Task<bool> ProjectPharmacyEventAsync(
        PharmacyProjectedEvent evt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);

        await EnsureSourceEventAsync(
            evt.EventId,
            PharmacyProjection,
            nameof(PharmacyProjectedEvent),
            evt,
            evt.OccurredAtUtc,
            cancellationToken);

        var alreadyProcessed = await _dbContext.ProjectionProcessedEvents
            .AnyAsync(e => e.EventId == evt.EventId, cancellationToken);

        if (alreadyProcessed)
        {
            return false;
        }

        var metric = await _dbContext.PharmacyDispensingMetrics
            .FirstOrDefaultAsync(m => m.Date == evt.Date, cancellationToken);

        if (metric is null)
        {
            metric = new PharmacyDispensingMetric(
                evt.Date,
                totalPrescriptions: evt.PendingDelta > 0 ? evt.PendingDelta : 0,
                pendingDispenseCount: Math.Max(0, evt.PendingDelta),
                dispensedCount: Math.Max(0, evt.DispensedDelta),
                lowStockItemCount: evt.LowStockCount ?? 0,
                nearExpiryLotCount: evt.NearExpiryCount ?? 0,
                createdUtc: evt.OccurredAtUtc);

            _dbContext.PharmacyDispensingMetrics.Add(metric);
        }
        else
        {
            metric.UpdateCounts(
                evt.PendingDelta,
                evt.DispensedDelta,
                evt.LowStockCount,
                evt.NearExpiryCount,
                evt.OccurredAtUtc);
        }

        _dbContext.ProjectionProcessedEvents.Add(
            new ProjectionProcessedEvent(evt.EventId, PharmacyProjection, evt.OccurredAtUtc));

        await UpdateCheckpointAsync(PharmacyProjection, evt.OccurredAtUtc, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_realtimeNotifier is not null)
        {
            await _realtimeNotifier.NotifyDashboardUpdatedAsync(
                "pharmacy",
                evt.Date,
                null,
                cancellationToken);
        }

        return true;
    }

    private async Task UpdateCheckpointAsync(
        string projectionName,
        DateTime timestampUtc,
        CancellationToken cancellationToken)
    {
        var checkpoint = await _dbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(c => c.ProjectionName == projectionName, cancellationToken);

        if (checkpoint is null)
        {
            checkpoint = new ProjectionCheckpoint(projectionName);
            _dbContext.ProjectionCheckpoints.Add(checkpoint);
        }

        checkpoint.MarkActive(checkpoint.LastProcessedPosition + 1, timestampUtc);
    }

    private async Task EnsureSourceEventAsync<TEvent>(
        Guid eventId,
        string projectionName,
        string eventType,
        TEvent projectionEvent,
        DateTime occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var sourceExists = await _dbContext.ProjectionSourceEvents
            .AnyAsync(sourceEvent => sourceEvent.EventId == eventId, cancellationToken);
        if (sourceExists)
        {
            return;
        }

        _dbContext.ProjectionSourceEvents.Add(new ProjectionSourceEvent(
            eventId,
            projectionName,
            eventType,
            JsonSerializer.Serialize(projectionEvent),
            occurredAtUtc));
    }
}
