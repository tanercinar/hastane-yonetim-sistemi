namespace HospitalManagement.Modules.Reporting.Application;

public interface IOutpatientDashboardService
{
    Task<OutpatientDashboardSummaryDto> GetSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutpatientDepartmentMetricDto>> GetDepartmentMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutpatientDoctorMetricDto>> GetDoctorMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
