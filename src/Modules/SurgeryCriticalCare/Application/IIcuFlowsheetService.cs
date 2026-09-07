namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public interface IIcuFlowsheetService
{
    Task<SurgeryOperationResult<IcuFlowsheetEntryDto>> AddFlowsheetEntryAsync(
        CreateIcuFlowsheetEntryDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<List<IcuFlowsheetEntryDto>> GetFlowsheetEntriesAsync(
        Guid icuAdmissionId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    Task<IcuFluidBalanceSummaryDto> GetFluidBalanceSummaryAsync(
        Guid icuAdmissionId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
