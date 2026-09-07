using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public interface IClinicalHandoffApiClient
{
    Task<List<ClinicalHandoffResponse>> GetPendingHandoffsAsync(string? destinationArea = null, CancellationToken cancellationToken = default);

    Task<List<ClinicalHandoffResponse>> GetHandoffsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    Task<ClinicalHandoffResponse?> GetHandoffByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClinicalHandoffResponse?> InitiateHandoffAsync(InitiateClinicalHandoffRequest request, CancellationToken cancellationToken = default);

    Task<ClinicalHandoffResponse?> AcceptHandoffAsync(Guid id, AcceptClinicalHandoffRequest request, CancellationToken cancellationToken = default);

    Task<ClinicalHandoffResponse?> RejectHandoffAsync(Guid id, RejectClinicalHandoffRequest request, CancellationToken cancellationToken = default);

    Task<ClinicalHandoffResponse?> CancelHandoffAsync(Guid id, CancelClinicalHandoffRequest request, CancellationToken cancellationToken = default);
}
