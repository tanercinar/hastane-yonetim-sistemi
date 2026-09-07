using System.Net.Http.Json;
using HospitalManagement.Contracts.Specialty;

namespace HospitalManagement.Web.Client.Specialty;

public sealed class PatientSpecialtyPortalApiClient(HttpClient httpClient)
    : IPatientSpecialtyPortalApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PatientSpecialtyPortalResponse> GetMyPublishedRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<PatientSpecialtyPortalResponse>(
            "api/v1/specialty/patient-portal/my-records",
            cancellationToken);

        return response ?? new PatientSpecialtyPortalResponse([], [], [], [], [], DateTime.UtcNow);
    }
}
