using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Patients;

namespace HospitalManagement.Web.Client.Patients;

public sealed class PatientApiClient(HttpClient httpClient) : IPatientApiClient
{
    private const string AntiforgeryHeaderName = "X-HMS-CSRF";
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<PatientListResponse?> SearchPatientsAsync(
        string? query,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(query)
            ? $"api/v1/patients?page={page}&pageSize={pageSize}"
            : $"api/v1/patients?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PatientListResponse>(cancellationToken);
    }

    public async Task<PatientDetailResponse?> GetPatientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/patients/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PatientDetailResponse>(cancellationToken);
    }

    public async Task<PatientDetailResponse?> GetPatientByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/patients/by-person/{personId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PatientDetailResponse>(cancellationToken);
    }

    public async Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> RegisterPatientAsync(
        PatientRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/patients")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, token);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var patient = await response.Content.ReadFromJsonAsync<PatientDetailResponse>(cancellationToken);
            return (true, patient, null);
        }

        var errorMsg = await ExtractErrorMessageAsync(response, cancellationToken);
        return (false, null, errorMsg);
    }

    public async Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> UpdatePatientAsync(
        Guid id,
        PatientUpdateRequest request,
        long version,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/v1/patients/{id}")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, token);
        httpRequest.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var patient = await response.Content.ReadFromJsonAsync<PatientDetailResponse>(cancellationToken);
            return (true, patient, null);
        }

        var errorMsg = await ExtractErrorMessageAsync(response, cancellationToken);
        return (false, null, errorMsg);
    }

    public async Task<DuplicateCheckResponse?> CheckDuplicateAsync(
        DuplicatePatientCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/patients/duplicate-check")
        {
            Content = JsonContent.Create(request),
        };
        httpRequest.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, token);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DuplicateCheckResponse>(cancellationToken);
    }

    private async Task<string> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("api/v1/identity/antiforgery", cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(cancellationToken);
        return payload?.Token ?? string.Empty;
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return $"İşlem başarısız oldu (HTTP {response.StatusCode}).";
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("detail", out var detailProp) && !string.IsNullOrWhiteSpace(detailProp.GetString()))
            {
                return detailProp.GetString()!;
            }

            if (doc.RootElement.TryGetProperty("title", out var titleProp) && !string.IsNullOrWhiteSpace(titleProp.GetString()))
            {
                return titleProp.GetString()!;
            }

            if (doc.RootElement.TryGetProperty("errors", out var errorsProp))
            {
                var errorList = new List<string>();
                foreach (var prop in errorsProp.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var err in prop.Value.EnumerateArray())
                        {
                            var s = err.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                errorList.Add(s);
                            }
                        }
                    }
                }

                if (errorList.Count > 0)
                {
                    return string.Join("; ", errorList);
                }
            }

            return content;
        }
        catch
        {
            return $"İşlem başarısız oldu (HTTP {response.StatusCode}).";
        }
    }
}
