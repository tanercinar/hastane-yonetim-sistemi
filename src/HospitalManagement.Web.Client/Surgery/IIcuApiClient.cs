using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public interface IIcuApiClient
{
    Task<List<IcuBedResponse>> GetIcuBedsAsync(CancellationToken cancellationToken = default);

    Task<List<IcuAdmissionResponse>> GetActiveAdmissionsAsync(CancellationToken cancellationToken = default);

    Task<IcuAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IcuAdmissionResponse?> AdmitToIcuAsync(CreateIcuAdmissionRequest request, CancellationToken cancellationToken = default);

    Task<IcuAdmissionResponse?> UpdateCarePlanAsync(Guid id, UpdateIcuCarePlanRequest request, CancellationToken cancellationToken = default);

    Task<IcuAdmissionResponse?> DischargeOrTransferAsync(Guid id, IcuDischargeOrTransferRequest request, CancellationToken cancellationToken = default);

    Task<IcuFlowsheetEntryResponse?> AddFlowsheetEntryAsync(Guid admissionId, CreateIcuFlowsheetEntryRequest request, CancellationToken cancellationToken = default);

    Task<List<IcuFlowsheetEntryResponse>> GetFlowsheetEntriesAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);

    Task<IcuFluidBalanceSummaryResponse?> GetFluidBalanceSummaryAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);
}
