using System.Net.Http.Json;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Contracts.Identity;

namespace HospitalManagement.Web.Client.Emergency;

public sealed class EmergencyApiClient : IEmergencyApiClient
{
    private readonly HttpClient _httpClient;

    public EmergencyApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<EmergencyAdmissionResponse?> CreateAdmissionAsync(
        CreateEmergencyAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/emergency/admissions")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            message.Headers.Add("X-HMS-CSRF", csrf);
        }

        var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<EmergencyAdmissionResponse?> RecordTriageAsync(
        Guid id,
        RecordTriageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/admissions/{id}/triage")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            message.Headers.Add("X-HMS-CSRF", csrf);
        }

        var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<EmergencyAdmissionResponse?> AssignDoctorAsync(
        Guid id,
        AssignEmergencyDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/admissions/{id}/assign-doctor")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            message.Headers.Add("X-HMS-CSRF", csrf);
        }

        var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<EmergencyAdmissionResponse?> UpdateStatusAsync(
        Guid id,
        UpdateEmergencyAdmissionStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/admissions/{id}/status")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            message.Headers.Add("X-HMS-CSRF", csrf);
        }

        var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<EmergencyAdmissionResponse?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmergencyAdmissionResponse>(
                $"api/v1/emergency/admissions/{id}",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyAdmissionResponse?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmergencyAdmissionResponse>(
                $"api/v1/emergency/admissions/active/by-patient/{patientId}",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<EmergencyAdmissionSummaryResponse>> GetAdmissionsAsync(
        string? status = null,
        string? triageLevel = null,
        Guid? patientId = null,
        Guid? assignedDoctorId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (!string.IsNullOrWhiteSpace(triageLevel))
            {
                queryParams.Add($"triageLevel={Uri.EscapeDataString(triageLevel)}");
            }

            if (patientId.HasValue && patientId.Value != Guid.Empty)
            {
                queryParams.Add($"patientId={patientId.Value}");
            }

            if (assignedDoctorId.HasValue && assignedDoctorId.Value != Guid.Empty)
            {
                queryParams.Add($"assignedDoctorId={assignedDoctorId.Value}");
            }

            if (fromDateUtc.HasValue)
            {
                queryParams.Add($"fromDate={fromDateUtc.Value:O}");
            }

            if (toDateUtc.HasValue)
            {
                queryParams.Add($"toDate={toDateUtc.Value:O}");
            }

            var uri = "api/v1/emergency/admissions" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<EmergencyAdmissionSummaryResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyBoardSummaryResponse?> GetBoardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<EmergencyBoardSummaryResponse>(
                "api/v1/emergency/board/summary",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<EmergencyBoardWorklistItemResponse>> GetBoardWorklistAsync(
        string? status = null,
        string? triageLevel = null,
        string? zone = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (!string.IsNullOrWhiteSpace(triageLevel))
            {
                queryParams.Add($"triageLevel={Uri.EscapeDataString(triageLevel)}");
            }

            if (!string.IsNullOrWhiteSpace(zone))
            {
                queryParams.Add($"zone={Uri.EscapeDataString(zone)}");
            }

            var uri = "api/v1/emergency/board/worklist" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<EmergencyBoardWorklistItemResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyAdmissionResponse?> RecordDispositionAsync(
        Guid id,
        RecordEmergencyDispositionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/admissions/{id}/disposition");
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

            return await response.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyOrderResponse?> CreateOrderAsync(
        CreateEmergencyOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/emergency/orders");
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

            return await response.Content.ReadFromJsonAsync<EmergencyOrderResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyOrderResponse?> CompleteOrderAsync(
        Guid id,
        CompleteEmergencyOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/orders/{id}/complete");
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

            return await response.Content.ReadFromJsonAsync<EmergencyOrderResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyOrderResponse?> CancelOrderAsync(
        Guid id,
        CancelEmergencyOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/orders/{id}/cancel");
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

            return await response.Content.ReadFromJsonAsync<EmergencyOrderResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<EmergencyOrderResponse>> GetOrdersByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<EmergencyOrderResponse>>(
                $"api/v1/emergency/orders/by-admission/{admissionId}",
                cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyConsultationResponse?> RequestConsultationAsync(
        RequestEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/emergency/consultations");
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

            return await response.Content.ReadFromJsonAsync<EmergencyConsultationResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyConsultationResponse?> AcceptConsultationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/consultations/{id}/accept");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EmergencyConsultationResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyConsultationResponse?> RespondConsultationAsync(
        Guid id,
        RespondEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/consultations/{id}/respond");
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

            return await response.Content.ReadFromJsonAsync<EmergencyConsultationResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<EmergencyConsultationResponse?> CancelConsultationAsync(
        Guid id,
        CancelEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/emergency/consultations/{id}/cancel");
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

            return await response.Content.ReadFromJsonAsync<EmergencyConsultationResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<EmergencyConsultationResponse>> GetConsultationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<EmergencyConsultationResponse>>(
                $"api/v1/emergency/consultations/by-admission/{admissionId}",
                cancellationToken);
            return result ?? [];
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
            var response = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
                "api/v1/identity/antiforgery",
                cancellationToken);
            return response?.Token;
        }
        catch
        {
            throw;
        }
    }
}
