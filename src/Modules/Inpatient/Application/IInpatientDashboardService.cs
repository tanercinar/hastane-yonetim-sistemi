namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientDashboardService
{
    Task<InpatientDashboardDto> GetDashboardSummaryAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
