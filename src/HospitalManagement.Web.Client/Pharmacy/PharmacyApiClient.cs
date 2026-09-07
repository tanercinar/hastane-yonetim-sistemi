using System.Net.Http.Json;
using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;

namespace HospitalManagement.Web.Client.Pharmacy;

public sealed class PharmacyApiClient(HttpClient httpClient) : IPharmacyApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<EncounterDetailResponse?> GetEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/clinical-records/encounters/{encounterId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EncounterDetailResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicationCatalogItemResponse>?> SearchMedicationsAsync(
        string? query,
        string? route = null,
        string? form = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/pharmacy/medications?maxResults={maxResults}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            uri += $"&query={Uri.EscapeDataString(query.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(route))
        {
            uri += $"&route={Uri.EscapeDataString(route.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(form))
        {
            uri += $"&form={Uri.EscapeDataString(form.Trim())}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<MedicationCatalogItemResponse>>(cancellationToken);
    }

    public async Task<MedicationCatalogItemResponse?> GetMedicationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/pharmacy/medications/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MedicationCatalogItemResponse>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> CreatePrescriptionDraftAsync(
        CreatePrescriptionDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/pharmacy/prescriptions")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> UpdatePrescriptionDraftAsync(
        Guid id,
        UpdatePrescriptionDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"api/v1/pharmacy/prescriptions/{id}")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> SignPrescriptionAsync(
        Guid id,
        SignPrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/pharmacy/prescriptions/{id}/sign")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> CancelPrescriptionAsync(
        Guid id,
        CancelPrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/pharmacy/prescriptions/{id}/cancel")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> GetPrescriptionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/pharmacy/prescriptions/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionsByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/pharmacy/prescriptions/by-encounter/{encounterId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PrescriptionSummaryResponse>>(cancellationToken);
    }

    public async Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionsByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/pharmacy/prescriptions/by-patient/{patientId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PrescriptionSummaryResponse>>(cancellationToken);
    }

    public async Task<MedicationSafetyCheckResponse?> CheckMedicationSafetyAsync(
        MedicationSafetyCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/pharmacy/prescriptions/safety-check")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MedicationSafetyCheckResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<PrescriptionSummaryResponse>?> GetPrescriptionWorklistAsync(
        string? status = null,
        string? prescriptionNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (!string.IsNullOrWhiteSpace(prescriptionNumber))
        {
            queryParams.Add($"prescriptionNumber={Uri.EscapeDataString(prescriptionNumber)}");
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            queryParams.Add($"patientId={patientId.Value}");
        }

        queryParams.Add($"maxResults={maxResults}");

        var url = "api/v1/pharmacy/prescriptions/worklist?" + string.Join("&", queryParams);
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<PrescriptionSummaryResponse>>(cancellationToken);
    }

    public async Task<PrescriptionDetailResponse?> DispensePrescriptionAsync(
        Guid id,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/pharmacy/prescriptions/{id}/dispense")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PrescriptionDetailResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<FefoCandidateStockResponse>?> GetFefoCandidatesAsync(
        Guid medicationCatalogItemId,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/pharmacy/inventory/fefo-candidates/{medicationCatalogItemId}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<FefoCandidateStockResponse>>(cancellationToken);
    }

    private async Task<string?> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/v1/identity/antiforgery", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var token = await response.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(cancellationToken);
            return token?.Token;
        }
        catch
        {
            return null;
        }
    }
}
