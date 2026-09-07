using HospitalManagement.Contracts.Reporting;

namespace HospitalManagement.Web.Client.Reporting;

public interface IReportingApiClient
{
    Task<RebuildProjectionsResponse> RebuildProjectionsAsync(
        RebuildProjectionsRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<List<ProjectionCheckpointResponse>> GetCheckpointsAsync(
        CancellationToken cancellationToken = default);

    Task<List<DailyOutpatientMetricResponse>> GetOutpatientMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticWorkloadMetricResponse>> GetDiagnosticMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default);

    Task<List<BedOccupancyMetricResponse>> GetBedOccupancyMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        string? wardType = null,
        CancellationToken cancellationToken = default);

    Task<List<PharmacyDispensingMetricResponse>> GetPharmacyMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    Task<OutpatientDashboardSummaryResponse?> GetOutpatientDashboardSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default);

    Task<List<OutpatientDepartmentMetricResponse>> GetOutpatientDepartmentMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<List<OutpatientDoctorMetricResponse>> GetOutpatientDoctorMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticDashboardSummaryResponse?> GetDiagnosticDashboardSummaryAsync(
        DateOnly? targetDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticModalityMetricResponse>> GetDiagnosticModalityMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticCriticalAlertMetricResponse>> GetDiagnosticCriticalAlertMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationsSummaryResponse?> GetInpatientOperationsSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<List<WardOccupancyDetailResponse>> GetWardOccupancyAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyTriageQueueMetricResponse>> GetEmergencyTriageMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<PharmacyDashboardSummaryResponse?> GetPharmacyDashboardSummaryAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<List<PharmacyStockAlertMetricResponse>> GetPharmacyStockAlertsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<List<ProjectionLagInfo>> GetProjectionLagAsync(
        CancellationToken cancellationToken = default);
}

