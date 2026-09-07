using HospitalManagement.Contracts.Patients;

namespace HospitalManagement.Web.Client.Patients;

public interface IPatientApiClient
{
    Task<PatientListResponse?> SearchPatientsAsync(
        string? query,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<PatientDetailResponse?> GetPatientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PatientDetailResponse?> GetPatientByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> RegisterPatientAsync(
        PatientRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> UpdatePatientAsync(
        Guid id,
        PatientUpdateRequest request,
        long version,
        CancellationToken cancellationToken = default);

    Task<DuplicateCheckResponse?> CheckDuplicateAsync(
        DuplicatePatientCheckRequest request,
        CancellationToken cancellationToken = default);
}
