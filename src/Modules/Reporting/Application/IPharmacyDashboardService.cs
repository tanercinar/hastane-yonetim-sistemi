namespace HospitalManagement.Modules.Reporting.Application;

public interface IPharmacyDashboardService
{
    Task<PharmacyDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PharmacyStockAlertMetricDto>> GetStockAlertsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);
}
