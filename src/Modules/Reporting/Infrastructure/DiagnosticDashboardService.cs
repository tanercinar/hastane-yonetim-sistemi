using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class DiagnosticDashboardService : IDiagnosticDashboardService
{
    private readonly ReportingDbContext _dbContext;

    public DiagnosticDashboardService(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DiagnosticDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _dbContext.DiagnosticWorkloadMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate);

        if (!string.IsNullOrWhiteSpace(modalityOrSection))
        {
            query = query.Where(m => m.ModalityOrSection == modalityOrSection.Trim());
        }

        var metrics = await query.ToListAsync(cancellationToken);

        var total = metrics.Sum(m => m.TotalOrders);
        var pending = metrics.Sum(m => m.PendingSpecimenCount);
        var processing = metrics.Sum(m => m.ProcessingCount);
        var finalized = metrics.Sum(m => m.FinalizedCount);
        var critical = metrics.Sum(m => m.CriticalCount);

        var finalizedMetricsWithTat = metrics.Where(m => m.FinalizedCount > 0 && m.AvgTurnaroundMinutes > 0).ToList();
        var avgTat = finalizedMetricsWithTat.Count > 0
            ? Math.Round(finalizedMetricsWithTat.Sum(m => m.AvgTurnaroundMinutes * m.FinalizedCount) / finalizedMetricsWithTat.Sum(m => m.FinalizedCount), 1)
            : 0.0;

        var lastUpdated = metrics.Count > 0
            ? metrics.Max(m => m.LastUpdatedUtc)
            : DateTime.UtcNow;

        return new DiagnosticDashboardSummaryDto(
            queryDate,
            total,
            pending,
            processing,
            finalized,
            critical,
            avgTat,
            lastUpdated);
    }

    public async Task<IReadOnlyList<DiagnosticModalityMetricDto>> GetModalityMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.DiagnosticWorkloadMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate)
            .ToListAsync(cancellationToken);

        var grouped = metrics
            .GroupBy(m => m.ModalityOrSection)
            .Select(g =>
            {
                var finCount = g.Sum(m => m.FinalizedCount);
                var tatItems = g.Where(m => m.FinalizedCount > 0 && m.AvgTurnaroundMinutes > 0).ToList();
                var avgTat = tatItems.Count > 0 && finCount > 0
                    ? Math.Round(tatItems.Sum(m => m.AvgTurnaroundMinutes * m.FinalizedCount) / finCount, 1)
                    : 0.0;

                return new DiagnosticModalityMetricDto(
                    g.Key,
                    g.Sum(m => m.TotalOrders),
                    g.Sum(m => m.PendingSpecimenCount),
                    g.Sum(m => m.ProcessingCount),
                    finCount,
                    g.Sum(m => m.CriticalCount),
                    avgTat);
            })
            .OrderBy(m => m.ModalityOrSection)
            .ToList();

        return grouped;
    }

    public async Task<IReadOnlyList<DiagnosticCriticalAlertMetricDto>> GetCriticalAlertMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.DiagnosticWorkloadMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate && m.CriticalCount > 0)
            .OrderByDescending(m => m.CriticalCount)
            .ToListAsync(cancellationToken);

        return metrics
            .Select(m => new DiagnosticCriticalAlertMetricDto(
                m.Id,
                m.Date,
                m.ModalityOrSection,
                m.CriticalCount,
                m.LastUpdatedUtc))
            .ToList();
    }
}
