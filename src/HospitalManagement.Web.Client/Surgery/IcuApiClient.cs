using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public sealed class IcuApiClient : IIcuApiClient
{
    private readonly HttpClient _httpClient;

    public IcuApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<IcuBedResponse>> GetIcuBedsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<IcuBedResponse>>("api/v1/icu/beds", cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<IcuAdmissionResponse>> GetActiveAdmissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<IcuAdmissionResponse>>("api/v1/icu/admissions/active", cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<IcuAdmissionResponse>($"api/v1/icu/admissions/{id}", cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuAdmissionResponse?> AdmitToIcuAsync(CreateIcuAdmissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/icu/admissions");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IcuAdmissionResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuAdmissionResponse?> UpdateCarePlanAsync(Guid id, UpdateIcuCarePlanRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/icu/admissions/{id}/care-plan");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IcuAdmissionResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuAdmissionResponse?> DischargeOrTransferAsync(Guid id, IcuDischargeOrTransferRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/icu/admissions/{id}/discharge-or-transfer");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IcuAdmissionResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuFlowsheetEntryResponse?> AddFlowsheetEntryAsync(Guid admissionId, CreateIcuFlowsheetEntryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/icu/admissions/{admissionId}/flowsheet");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IcuFlowsheetEntryResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<IcuFlowsheetEntryResponse>> GetFlowsheetEntriesAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/v1/icu/admissions/{admissionId}/flowsheet";
            var queryParams = new List<string>();
            if (fromUtc.HasValue)
                queryParams.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
            if (toUtc.HasValue)
                queryParams.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var result = await _httpClient.GetFromJsonAsync<List<IcuFlowsheetEntryResponse>>(url, cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<IcuFluidBalanceSummaryResponse?> GetFluidBalanceSummaryAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"api/v1/icu/admissions/{admissionId}/flowsheet/fluid-balance";
            var queryParams = new List<string>();
            if (fromUtc.HasValue)
                queryParams.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
            if (toUtc.HasValue)
                queryParams.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            return await _httpClient.GetFromJsonAsync<IcuFluidBalanceSummaryResponse>(url, cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    private async Task<string?> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>("api/v1/identity/antiforgery", cancellationToken);
            return response?.Token;
        }
        catch
        {
            throw;
        }
    }
}
