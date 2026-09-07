namespace HospitalManagement.Modules.Reporting.Application;

public interface IProjectionRebuilder
{
    Task<RebuildSummaryDto> RebuildAllProjectionsAsync(
        CancellationToken cancellationToken = default);

    Task<RebuildSummaryDto> RebuildProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default);
}
