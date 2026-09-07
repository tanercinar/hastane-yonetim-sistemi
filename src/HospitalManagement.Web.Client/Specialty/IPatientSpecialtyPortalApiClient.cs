using HospitalManagement.Contracts.Specialty;

namespace HospitalManagement.Web.Client.Specialty;

public interface IPatientSpecialtyPortalApiClient
{
    Task<PatientSpecialtyPortalResponse> GetMyPublishedRecordsAsync(
        CancellationToken cancellationToken = default);
}
