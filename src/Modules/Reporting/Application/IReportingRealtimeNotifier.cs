namespace HospitalManagement.Modules.Reporting.Application;

public interface IReportingRealtimeNotifier
{
    Task NotifyDashboardUpdatedAsync(
        string dashboardType,
        DateOnly metricDate,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
