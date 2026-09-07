using System.Net.Http.Json;
using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Contracts.Identity;

namespace HospitalManagement.Web.Client.Diagnostics;

public sealed class DiagnosticsApiClient(HttpClient httpClient) : IDiagnosticsApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<EncounterDetailResponse?> GetEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/clinical-records/encounters/{encounterId}",
            cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<EncounterDetailResponse>(cancellationToken)
            : null;
    }

    public async Task<List<LabCatalogSummaryResponse>> SearchLabCatalogAsync(
        string? query = null,
        string? category = null,
        bool? isActive = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/diagnostics/lab-catalog?maxResults={maxResults}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            uri += $"&query={Uri.EscapeDataString(query.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            uri += $"&category={Uri.EscapeDataString(category.Trim())}";
        }

        if (isActive.HasValue)
        {
            uri += $"&isActive={isActive.Value}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<LabCatalogSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<LabCatalogItemResponse?> GetLabCatalogItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/lab-catalog/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabCatalogItemResponse>(cancellationToken);
    }

    public async Task<DiagnosticOrderDetailResponse?> CreateDiagnosticOrderDraftAsync(
        CreateDiagnosticOrderDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/diagnostics/orders")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>(cancellationToken);
    }

    public async Task<DiagnosticOrderDetailResponse?> UpdateDiagnosticOrderDraftAsync(
        Guid id,
        UpdateDiagnosticOrderDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Put, $"api/v1/diagnostics/orders/{id}")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>(cancellationToken);
    }

    public async Task<DiagnosticOrderDetailResponse?> PlaceDiagnosticOrderAsync(
        Guid id,
        PlaceDiagnosticOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/orders/{id}/place")
        {
            Content = JsonContent.Create(request ?? new PlaceDiagnosticOrderRequest()),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>(cancellationToken);
    }

    public async Task<DiagnosticOrderDetailResponse?> CancelDiagnosticOrderAsync(
        Guid id,
        CancelDiagnosticOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/orders/{id}/cancel")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>(cancellationToken);
    }

    public async Task<DiagnosticOrderDetailResponse?> GetDiagnosticOrderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/orders/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DiagnosticOrderDetailResponse>(cancellationToken);
    }

    public async Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/diagnostics/orders/by-encounter/{encounterId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<DiagnosticOrderSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/diagnostics/orders/by-patient/{patientId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<DiagnosticOrderSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrderWorklistAsync(
        string? orderType = null,
        string? status = null,
        string? orderNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/diagnostics/orders/worklist?maxResults={maxResults}";
        if (!string.IsNullOrWhiteSpace(orderType))
        {
            uri += $"&orderType={Uri.EscapeDataString(orderType.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            uri += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            uri += $"&orderNumber={Uri.EscapeDataString(orderNumber.Trim())}";
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            uri += $"&patientId={patientId.Value}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<DiagnosticOrderSummaryResponse>>(cancellationToken) ?? [];
    }

    // Specimen Methods
    public async Task<SpecimenDetailResponse?> CollectSpecimenAsync(
        CollectSpecimenRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/diagnostics/specimens/collect")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<SpecimenDetailResponse?> TransitSpecimenAsync(
        Guid id,
        TransitSpecimenRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/specimens/{id}/transit")
        {
            Content = JsonContent.Create(request ?? new TransitSpecimenRequest()),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<SpecimenDetailResponse?> ReceiveSpecimenAsync(
        Guid id,
        ReceiveSpecimenRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/specimens/{id}/receive")
        {
            Content = JsonContent.Create(request ?? new ReceiveSpecimenRequest()),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<SpecimenDetailResponse?> RejectSpecimenAsync(
        Guid id,
        RejectSpecimenRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/specimens/{id}/reject")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<SpecimenDetailResponse?> GetSpecimenByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/specimens/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<SpecimenDetailResponse?> GetSpecimenByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/specimens/by-barcode/{Uri.EscapeDataString(barcode.Trim())}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SpecimenDetailResponse>(cancellationToken);
    }

    public async Task<List<SpecimenSummaryResponse>> GetSpecimensByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/specimens/by-order/{orderId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<SpecimenSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<SpecimenSummaryResponse>> GetSpecimenWorklistAsync(
        string? status = null,
        string? barcode = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/diagnostics/specimens/worklist?maxResults={maxResults}";
        if (!string.IsNullOrWhiteSpace(status))
        {
            uri += $"&status={Uri.EscapeDataString(status.Trim())}";
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            uri += $"&barcode={Uri.EscapeDataString(barcode.Trim())}";
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            uri += $"&patientId={patientId.Value}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<SpecimenSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<LabResultDetailResponse?> CreateDraftLabResultAsync(
        CreateDraftLabResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/diagnostics/lab-results")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<LabResultDetailResponse?> UpdateLabResultItemsAsync(
        Guid id,
        UpdateLabResultItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Put, $"api/v1/diagnostics/lab-results/{id}/items")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<LabResultDetailResponse?> ApproveLabResultTechnicallyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/lab-results/{id}/technical-approve");

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<LabResultDetailResponse?> ApproveLabResultClinicallyAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/lab-results/{id}/clinical-approve");

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<LabResultDetailResponse?> CorrectLabResultAsync(
        Guid id,
        CorrectLabResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/lab-results/{id}/correct")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<LabResultDetailResponse?> GetLabResultByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/lab-results/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LabResultDetailResponse>(cancellationToken);
    }

    public async Task<List<LabResultDetailResponse>> GetLabResultsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/lab-results/by-order/{orderId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<LabResultDetailResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<LabResultSummaryResponse>> GetLabResultWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var uri = "api/v1/diagnostics/lab-results/worklist";
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            queryParams.Add($"patientId={patientId.Value}");
        }

        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<LabResultSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<CriticalResultNotificationResponse>> GetActiveCriticalNotificationsAsync(
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var uri = "api/v1/diagnostics/critical-notifications/active";
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            uri += $"?patientId={patientId.Value}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<CriticalResultNotificationResponse>>(cancellationToken) ?? [];
    }

    public async Task<CriticalResultNotificationResponse?> GetCriticalNotificationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/critical-notifications/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CriticalResultNotificationResponse>(cancellationToken);
    }

    public async Task<CriticalResultNotificationResponse?> AcknowledgeCriticalNotificationAsync(
        Guid id,
        AcknowledgeCriticalResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/critical-notifications/{id}/acknowledge")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CriticalResultNotificationResponse>(cancellationToken);
    }

    public async Task<CriticalResultNotificationResponse?> EscalateCriticalNotificationAsync(
        Guid id,
        EscalateCriticalResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/critical-notifications/{id}/escalate")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CriticalResultNotificationResponse>(cancellationToken);
    }

    public async Task<List<RadiologyCatalogItemResponse>> GetRadiologyCatalogAsync(
        string? modality = null,
        CancellationToken cancellationToken = default)
    {
        var uri = "api/v1/diagnostics/radiology/catalog";
        if (!string.IsNullOrWhiteSpace(modality))
        {
            uri += $"?modality={Uri.EscapeDataString(modality.Trim())}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<RadiologyCatalogItemResponse>>(cancellationToken) ?? [];
    }

    public async Task<List<RadiologyStudySummaryResponse>> GetRadiologyWorklistAsync(
        string? modality = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(modality))
        {
            queryParams.Add($"modality={Uri.EscapeDataString(modality.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            queryParams.Add($"patientId={patientId.Value}");
        }

        var uri = "api/v1/diagnostics/radiology/studies/worklist";
        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<RadiologyStudySummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<RadiologyStudyDetailResponse?> GetRadiologyStudyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/radiology/studies/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> EnsureRadiologyStudyAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/ensure?orderId={orderId}&orderItemId={orderItemId}");

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> ScheduleRadiologyStudyAsync(
        Guid id,
        ScheduleRadiologyStudyRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/schedule")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> CompleteRadiologyAcquisitionAsync(
        Guid id,
        CompleteAcquisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/complete-acquisition")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> DraftRadiologyReportAsync(
        Guid id,
        DraftRadiologyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/draft-report")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> FinalizeRadiologyReportAsync(
        Guid id,
        FinalizeRadiologyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/finalize-report")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> AddRadiologyAddendumAsync(
        Guid id,
        AddRadiologyAddendumRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/add-addendum")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<RadiologyStudyDetailResponse?> CancelRadiologyStudyAsync(
        Guid id,
        CancelRadiologyStudyRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{id}/cancel")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<RadiologyStudyDetailResponse>(cancellationToken);
    }

    public async Task<DicomStudyMetadataResponse?> GetDicomStudyMetadataAsync(
        Guid studyId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/radiology/studies/{studyId}/dicom-metadata", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DicomStudyMetadataResponse>(cancellationToken);
    }

    public async Task<DicomPreviewTokenResponse?> GenerateDicomPreviewTokenAsync(
        Guid studyId,
        GenerateDicomPreviewTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/radiology/studies/{studyId}/dicom-preview-token")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DicomPreviewTokenResponse>(cancellationToken);
    }

    public async Task<string?> GetDicomPreviewDataUrlAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var antiforgeryToken = await GetAntiforgeryTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/diagnostics/radiology/dicom-preview")
        {
            Content = JsonContent.Create(new DicomPreviewImageRequest { Token = token }),
        };

        if (!string.IsNullOrWhiteSpace(antiforgeryToken))
        {
            request.Headers.Add("X-HMS-CSRF", antiforgeryToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/svg+xml";
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return $"data:{contentType};base64,{Convert.ToBase64String(content)}";
    }

    public async Task<List<PathologyCaseSummaryResponse>> GetPathologyWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/v1/diagnostics/pathology/cases/worklist";
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (patientId.HasValue && patientId.Value != Guid.Empty)
            queryParams.Add($"patientId={patientId.Value}");
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<PathologyCaseSummaryResponse>>(cancellationToken) ?? [];
    }

    public async Task<PathologyCaseDetailResponse?> GetPathologyCaseByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/diagnostics/pathology/cases/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>(cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> EnsurePathologyCaseAsync(
        Guid orderId,
        Guid orderItemId,
        string? specimenType = null,
        string? anatomicSite = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        var url = $"api/v1/diagnostics/pathology/cases/ensure?orderId={orderId}&orderItemId={orderItemId}";
        if (!string.IsNullOrWhiteSpace(specimenType))
            url += $"&specimenType={Uri.EscapeDataString(specimenType)}";
        if (!string.IsNullOrWhiteSpace(anatomicSite))
            url += $"&anatomicSite={Uri.EscapeDataString(anatomicSite)}";

        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>(cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> ReceivePathologySpecimenAsync(
        Guid id,
        ReceivePathologySpecimenRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/receive-specimen", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> RecordPathologyGrossExamAsync(
        Guid id,
        RecordGrossExamRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/gross-exam", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> RecordPathologyMicroscopicExamAsync(
        Guid id,
        RecordMicroscopicExamRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/microscopic-exam", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> DraftPathologyReportAsync(
        Guid id,
        DraftPathologyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/draft-report", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> FinalizePathologyReportAsync(
        Guid id,
        FinalizePathologyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/finalize-report", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> CorrectPathologyReportAsync(
        Guid id,
        CorrectPathologyReportRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/correct-report", request, cancellationToken);
    }

    public async Task<PathologyCaseDetailResponse?> CancelPathologyCaseAsync(
        Guid id,
        CancelPathologyCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendPostWithPathologyResponseAsync($"api/v1/diagnostics/pathology/cases/{id}/cancel", request, cancellationToken);
    }

    private async Task<PathologyCaseDetailResponse?> SendPostWithPathologyResponseAsync<T>(
        string url,
        T body,
        CancellationToken cancellationToken)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PathologyCaseDetailResponse>(cancellationToken);
    }

    // Blood Bank Methods
    public async Task<List<BloodUnitResponse>> GetBloodInventoryAsync(
        string? productType = null,
        string? bloodGroup = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(productType))
            queryParams.Add($"productType={Uri.EscapeDataString(productType)}");
        if (!string.IsNullOrWhiteSpace(bloodGroup))
            queryParams.Add($"bloodGroup={Uri.EscapeDataString(bloodGroup)}");
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status)}");

        var url = "api/v1/diagnostics/blood-bank/inventory";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        var result = await _httpClient.GetFromJsonAsync<List<BloodUnitResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<BloodInventorySummaryResponse?> GetBloodInventorySummaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<BloodInventorySummaryResponse>("api/v1/diagnostics/blood-bank/inventory/summary", cancellationToken);
    }

    public async Task<List<CrossmatchSummaryResponse>> GetCrossmatchWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (patientId.HasValue)
            queryParams.Add($"patientId={patientId.Value}");

        var url = "api/v1/diagnostics/blood-bank/crossmatch/worklist";
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        var result = await _httpClient.GetFromJsonAsync<List<CrossmatchSummaryResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<CrossmatchDetailResponse?> GetCrossmatchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CrossmatchDetailResponse>($"api/v1/diagnostics/blood-bank/crossmatch/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<CrossmatchDetailResponse?> CreateCrossmatchRequestAsync(
        CreateCrossmatchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/diagnostics/blood-bank/crossmatch")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CrossmatchDetailResponse>(cancellationToken);
    }

    public async Task<CrossmatchDetailResponse?> PerformCrossmatchTestAsync(
        Guid id,
        PerformCrossmatchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/blood-bank/crossmatch/{id}/test")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<CrossmatchDetailResponse>(cancellationToken);
    }

    public async Task<BloodUnitResponse?> IssueBloodUnitAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/blood-bank/units/{id}/issue")
        {
            Content = JsonContent.Create(new { }),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BloodUnitResponse>(cancellationToken);
    }

    public async Task<BloodUnitResponse?> RecordTransfusionAsync(
        Guid id,
        RecordTransfusionRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/diagnostics/blood-bank/units/{id}/transfuse")
        {
            Content = JsonContent.Create(request ?? new RecordTransfusionRequestDto()),
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            reqMsg.Headers.Add("X-HMS-CSRF", token);
        }

        using var response = await _httpClient.SendAsync(reqMsg, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BloodUnitResponse>(cancellationToken);
    }

    // Timeline and Patient Portal Methods
    public async Task<PatientDiagnosticTimelineResponse?> GetPatientDiagnosticTimelineAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PatientDiagnosticTimelineResponse>($"api/v1/diagnostics/timeline/patient/{patientId}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PatientPortalResultSummaryResponse>> GetMyPatientPortalResultsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<PatientPortalResultSummaryResponse>>("api/v1/diagnostics/results/my", cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
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
