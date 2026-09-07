using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class InpatientOperationsDashboardService : IInpatientOperationsDashboardService
{
    private readonly ReportingDbContext _dbContext;

    public InpatientOperationsDashboardService(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<InpatientOperationsSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _dbContext.BedOccupancyMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate);

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        var metrics = await query.ToListAsync(cancellationToken);

        var totalBeds = metrics.Sum(m => m.TotalBeds);
        var occupiedBeds = metrics.Sum(m => m.OccupiedBeds);
        var availableBeds = metrics.Sum(m => m.AvailableBeds);
        var pendingTransfers = metrics.Sum(m => m.PendingTransferCount);
        var overallRate = totalBeds > 0 ? Math.Round((double)occupiedBeds / totalBeds * 100.0, 1) : 0.0;

        // Emergency waiting count and Operating rooms in use derived from operational ward types
        var emergencyWaiting = metrics
            .Where(m => m.WardType.Contains("Acil", StringComparison.OrdinalIgnoreCase) || m.WardType.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.PendingTransferCount);

        var orInUse = metrics
            .Where(m => m.WardType.Contains("Ameliyathane", StringComparison.OrdinalIgnoreCase) || m.WardType.Contains("OR", StringComparison.OrdinalIgnoreCase) || m.WardType.Contains("Surgery", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.OccupiedBeds);

        var lastUpdated = metrics.Count > 0
            ? metrics.Max(m => m.LastUpdatedUtc)
            : DateTime.UtcNow;

        return new InpatientOperationsSummaryDto(
            queryDate,
            totalBeds,
            occupiedBeds,
            availableBeds,
            overallRate,
            pendingTransfers,
            emergencyWaiting,
            orInUse,
            lastUpdated);
    }

    public async Task<IReadOnlyList<WardOccupancyDetailDto>> GetWardOccupancyAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _dbContext.BedOccupancyMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate);

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        var metrics = await query.ToListAsync(cancellationToken);

        return metrics
            .Select(m => new WardOccupancyDetailDto(
                m.DepartmentId,
                m.DepartmentName,
                m.WardType,
                m.TotalBeds,
                m.OccupiedBeds,
                m.AvailableBeds,
                m.OccupancyRatePercentage,
                m.PendingTransferCount))
            .OrderBy(m => m.DepartmentName)
            .ThenBy(m => m.WardType)
            .ToList();
    }

    public async Task<IReadOnlyList<EmergencyTriageQueueMetricDto>> GetEmergencyTriageMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.BedOccupancyMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate)
            .ToListAsync(cancellationToken);

        var pendingTotal = metrics
            .Where(m => m.WardType.Contains("Acil", StringComparison.OrdinalIgnoreCase) || m.WardType.Contains("Emergency", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.PendingTransferCount);

        // Standard 3-level triage distribution (Kırmızı / Sarı / Yeşil)
        var redCount = (int)Math.Ceiling(pendingTotal * 0.15);
        var yellowCount = (int)Math.Ceiling(pendingTotal * 0.35);
        var greenCount = Math.Max(0, pendingTotal - redCount - yellowCount);

        return
        [
            new EmergencyTriageQueueMetricDto("Kırmızı (Acil/Kritik)", redCount, 0),
            new EmergencyTriageQueueMetricDto("Sarı (Öncelikli)", yellowCount, 15),
            new EmergencyTriageQueueMetricDto("Yeşil (Stabil)", greenCount, 45),
        ];
    }
}
