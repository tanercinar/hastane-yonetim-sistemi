namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface ISpecialtyReportingService
{
    Task<SpecialtyOperationalSummaryDto> GetOperationalSummaryAsync(
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null,
        CancellationToken cancellationToken = default);
}
