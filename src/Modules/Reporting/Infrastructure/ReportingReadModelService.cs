using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class ReportingReadModelService : IReportingReadModelService
{
    private readonly ReportingDbContext _dbContext;

    public ReportingReadModelService(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<DailyOutpatientMetricDto>> GetOutpatientMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DailyOutpatientMetrics.AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(m => m.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Date <= endDate.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(m => m.DoctorId == doctorId.Value);
        }

        var results = await query
            .OrderByDescending(m => m.Date)
            .ThenBy(m => m.DepartmentName)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<DiagnosticWorkloadMetricDto>> GetDiagnosticMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DiagnosticWorkloadMetrics.AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(m => m.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Date <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(modalityOrSection))
        {
            query = query.Where(m => m.ModalityOrSection == modalityOrSection.Trim());
        }

        var results = await query
            .OrderByDescending(m => m.Date)
            .ThenBy(m => m.ModalityOrSection)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<BedOccupancyMetricDto>> GetBedOccupancyMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        string? wardType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BedOccupancyMetrics.AsNoTracking();

        if (targetDate.HasValue)
        {
            query = query.Where(m => m.Date == targetDate.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(wardType))
        {
            query = query.Where(m => m.WardType == wardType.Trim());
        }

        var results = await query
            .OrderByDescending(m => m.Date)
            .ThenBy(m => m.DepartmentName)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PharmacyDispensingMetricDto>> GetPharmacyMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PharmacyDispensingMetrics.AsNoTracking();

        if (startDate.HasValue)
        {
            query = query.Where(m => m.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.Date <= endDate.Value);
        }

        var results = await query
            .OrderByDescending(m => m.Date)
            .ToListAsync(cancellationToken);

        return results.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ProjectionCheckpointDto>> GetCheckpointsAsync(
        CancellationToken cancellationToken = default)
    {
        var checkpoints = await _dbContext.ProjectionCheckpoints
            .AsNoTracking()
            .OrderBy(c => c.ProjectionName)
            .ToListAsync(cancellationToken);

        return checkpoints.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<ProjectionLagDto>> GetProjectionLagAsync(
        CancellationToken cancellationToken = default)
    {
        var checkpoints = await _dbContext.ProjectionCheckpoints
            .AsNoTracking()
            .OrderBy(c => c.ProjectionName)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var result = new List<ProjectionLagDto>(checkpoints.Count);

        foreach (var c in checkpoints)
        {
            var lag = Math.Max(0, (now - c.LastProcessedTimestampUtc).TotalSeconds);
            var isHealthy = c.Status == ProjectionStatus.Active && (lag < 300 || c.LastProcessedPosition == 0);

            result.Add(new ProjectionLagDto(
                c.ProjectionName,
                c.LastProcessedPosition,
                c.LastProcessedTimestampUtc,
                Math.Round(lag, 2),
                isHealthy,
                now));
        }

        return result;
    }

    private static DailyOutpatientMetricDto MapToDto(DailyOutpatientMetric m) =>
        new(
            m.Id,
            m.Date,
            m.DepartmentId,
            m.DepartmentName,
            m.DoctorId,
            m.DoctorName,
            m.TotalAppointments,
            m.ScheduledCount,
            m.CheckedInCount,
            m.InProgressCount,
            m.CompletedCount,
            m.CancelledCount,
            m.NoShowCount,
            m.LastUpdatedUtc);

    private static DiagnosticWorkloadMetricDto MapToDto(DiagnosticWorkloadMetric m) =>
        new(
            m.Id,
            m.Date,
            m.ModalityOrSection,
            m.TotalOrders,
            m.PendingSpecimenCount,
            m.ProcessingCount,
            m.FinalizedCount,
            m.CriticalCount,
            m.AvgTurnaroundMinutes,
            m.LastUpdatedUtc);

    private static BedOccupancyMetricDto MapToDto(BedOccupancyMetric m) =>
        new(
            m.Id,
            m.Date,
            m.DepartmentId,
            m.DepartmentName,
            m.WardType,
            m.TotalBeds,
            m.OccupiedBeds,
            m.AvailableBeds,
            m.PendingTransferCount,
            m.OccupancyRatePercentage,
            m.LastUpdatedUtc);

    private static PharmacyDispensingMetricDto MapToDto(PharmacyDispensingMetric m) =>
        new(
            m.Id,
            m.Date,
            m.TotalPrescriptions,
            m.PendingDispenseCount,
            m.DispensedCount,
            m.LowStockItemCount,
            m.NearExpiryLotCount,
            m.LastUpdatedUtc);

    private static ProjectionCheckpointDto MapToDto(ProjectionCheckpoint c) =>
        new(
            c.ProjectionName,
            c.LastProcessedPosition,
            c.LastProcessedTimestampUtc,
            c.Status.ToString(),
            c.Version,
            c.LastError);
}
