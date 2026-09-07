namespace HospitalManagement.Modules.Reporting.Application;

public interface IDiagnosticDashboardService
{
    Task<DiagnosticDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosticModalityMetricDto>> GetModalityMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosticCriticalAlertMetricDto>> GetCriticalAlertMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);
}
