namespace HospitalManagement.Modules.Reporting.Application;

public interface IReportingReadModelService
{
    Task<IReadOnlyList<DailyOutpatientMetricDto>> GetOutpatientMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosticWorkloadMetricDto>> GetDiagnosticMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BedOccupancyMetricDto>> GetBedOccupancyMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        string? wardType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PharmacyDispensingMetricDto>> GetPharmacyMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectionCheckpointDto>> GetCheckpointsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectionLagDto>> GetProjectionLagAsync(
        CancellationToken cancellationToken = default);
}
