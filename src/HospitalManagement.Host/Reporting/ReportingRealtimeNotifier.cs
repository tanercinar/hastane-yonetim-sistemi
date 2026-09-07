using HospitalManagement.Contracts.Reporting;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Modules.Reporting.Application;

using Microsoft.AspNetCore.SignalR;

namespace HospitalManagement.Host.Reporting;

public sealed class ReportingRealtimeNotifier : IReportingRealtimeNotifier
{
    public const string HubMethodName = "ReportingDashboardUpdated";
    public const string OperationsGroup = "reporting-operations";

    private static long _sequenceCounter;
    private readonly IHubContext<HospitalHub> _hubContext;

    public ReportingRealtimeNotifier(IHubContext<HospitalHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task NotifyDashboardUpdatedAsync(
        string dashboardType,
        DateOnly metricDate,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dashboardType);

        var sequenceNumber = Interlocked.Increment(ref _sequenceCounter);
        var update = new ReportingDashboardRealtimeUpdate(
            DashboardType: dashboardType.Trim(),
            SequenceNumber: sequenceNumber,
            MetricDate: metricDate,
            DepartmentId: departmentId,
            ProjectionLagSeconds: 0.0,
            EmittedAtUtc: DateTime.UtcNow);

        await _hubContext.Clients.Group(OperationsGroup).SendAsync(
            HubMethodName,
            update,
            cancellationToken);

        var dashboardGroup = $"reporting-{dashboardType.Trim().ToLowerInvariant()}";
        await _hubContext.Clients.Group(dashboardGroup).SendAsync(
            HubMethodName,
            update,
            cancellationToken);

        if (departmentId.HasValue)
        {
            var deptGroup = $"department-{departmentId.Value}";
            await _hubContext.Clients.Group(deptGroup).SendAsync(
                HubMethodName,
                update,
                cancellationToken);
        }
    }
}
