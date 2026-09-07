using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public sealed class ClinicalHandoffApiClient : IClinicalHandoffApiClient
{
    private readonly HttpClient _httpClient;

    public ClinicalHandoffApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<ClinicalHandoffResponse>> GetPendingHandoffsAsync(string? destinationArea = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "api/v1/clinical-handoffs/pending";
            if (!string.IsNullOrWhiteSpace(destinationArea))
            {
                url += $"?destinationArea={Uri.EscapeDataString(destinationArea)}";
            }

            var result = await _httpClient.GetFromJsonAsync<List<ClinicalHandoffResponse>>(url, cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<ClinicalHandoffResponse>> GetHandoffsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ClinicalHandoffResponse>>($"api/v1/clinical-handoffs/patient/{patientId}", cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClinicalHandoffResponse?> GetHandoffByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ClinicalHandoffResponse>($"api/v1/clinical-handoffs/{id}", cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClinicalHandoffResponse?> InitiateHandoffAsync(InitiateClinicalHandoffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/clinical-handoffs");
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

            return await response.Content.ReadFromJsonAsync<ClinicalHandoffResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClinicalHandoffResponse?> AcceptHandoffAsync(Guid id, AcceptClinicalHandoffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/clinical-handoffs/{id}/accept");
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

            return await response.Content.ReadFromJsonAsync<ClinicalHandoffResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClinicalHandoffResponse?> RejectHandoffAsync(Guid id, RejectClinicalHandoffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/clinical-handoffs/{id}/reject");
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

            return await response.Content.ReadFromJsonAsync<ClinicalHandoffResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<ClinicalHandoffResponse?> CancelHandoffAsync(Guid id, CancelClinicalHandoffRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/clinical-handoffs/{id}/cancel");
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

            return await response.Content.ReadFromJsonAsync<ClinicalHandoffResponse>(cancellationToken: cancellationToken);
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
