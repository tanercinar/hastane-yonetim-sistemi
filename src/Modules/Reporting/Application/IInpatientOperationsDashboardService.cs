namespace HospitalManagement.Modules.Reporting.Application;

public interface IInpatientOperationsDashboardService
{
    Task<InpatientOperationsSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WardOccupancyDetailDto>> GetWardOccupancyAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmergencyTriageQueueMetricDto>> GetEmergencyTriageMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);
}
