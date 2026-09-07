using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Reporting.Infrastructure;

public sealed class OutpatientDashboardService : IOutpatientDashboardService
{
    private readonly ReportingDbContext _dbContext;

    public OutpatientDashboardService(ReportingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<OutpatientDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _dbContext.DailyOutpatientMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate);

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(m => m.DoctorId == doctorId.Value);
        }

        var metrics = await query.ToListAsync(cancellationToken);

        var total = metrics.Sum(m => m.TotalAppointments);
        var scheduled = metrics.Sum(m => m.ScheduledCount);
        var checkedIn = metrics.Sum(m => m.CheckedInCount);
        var inProgress = metrics.Sum(m => m.InProgressCount);
        var completed = metrics.Sum(m => m.CompletedCount);
        var cancelled = metrics.Sum(m => m.CancelledCount);
        var noShow = metrics.Sum(m => m.NoShowCount);
        var waitingQueue = checkedIn + inProgress;
        var lastUpdated = metrics.Count > 0
            ? metrics.Max(m => m.LastUpdatedUtc)
            : DateTime.UtcNow;

        return new OutpatientDashboardSummaryDto(
            queryDate,
            total,
            scheduled,
            checkedIn,
            inProgress,
            completed,
            cancelled,
            noShow,
            waitingQueue,
            lastUpdated);
    }

    public async Task<IReadOnlyList<OutpatientDepartmentMetricDto>> GetDepartmentMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _dbContext.DailyOutpatientMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate)
            .ToListAsync(cancellationToken);

        var grouped = metrics
            .GroupBy(m => new { m.DepartmentId, m.DepartmentName })
            .Select(g => new OutpatientDepartmentMetricDto(
                g.Key.DepartmentId,
                g.Key.DepartmentName,
                g.Sum(m => m.TotalAppointments),
                g.Sum(m => m.ScheduledCount),
                g.Sum(m => m.CheckedInCount),
                g.Sum(m => m.InProgressCount),
                g.Sum(m => m.CompletedCount),
                g.Sum(m => m.CancelledCount),
                g.Sum(m => m.NoShowCount)))
            .OrderBy(d => d.DepartmentName)
            .ToList();

        return grouped;
    }

    public async Task<IReadOnlyList<OutpatientDoctorMetricDto>> GetDoctorMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryDate = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _dbContext.DailyOutpatientMetrics
            .AsNoTracking()
            .Where(m => m.Date == queryDate && m.DoctorId.HasValue && !string.IsNullOrEmpty(m.DoctorName));

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        var metrics = await query.ToListAsync(cancellationToken);

        var grouped = metrics
            .GroupBy(m => new { DoctorId = m.DoctorId!.Value, m.DoctorName, m.DepartmentId, m.DepartmentName })
            .Select(g => new OutpatientDoctorMetricDto(
                g.Key.DoctorId,
                g.Key.DoctorName!,
                g.Key.DepartmentId,
                g.Key.DepartmentName,
                g.Sum(m => m.TotalAppointments),
                g.Sum(m => m.CompletedCount),
                g.Sum(m => m.CheckedInCount + m.InProgressCount)))
            .OrderBy(d => d.DoctorName)
            .ToList();

        return grouped;
    }
}
