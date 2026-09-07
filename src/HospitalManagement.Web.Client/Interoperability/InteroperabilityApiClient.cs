using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Interoperability;

namespace HospitalManagement.Web.Client.Interoperability;

public sealed class InteroperabilityApiClient : IInteroperabilityApiClient
{
    private readonly HttpClient _httpClient;

    public InteroperabilityApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<MockServerConfigResponse>> GetAllMockConfigsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MockServerConfigResponse>>(
            "api/v1/interoperability/mock-engine/configs",
            cancellationToken);

        return result ?? [];
    }

    public async Task<MockServerConfigResponse> UpdateMockConfigAsync(
        string systemType,
        UpdateMockServerConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemType);
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Put, $"api/v1/interoperability/mock-engine/configs/{Uri.EscapeDataString(systemType)}")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<MockServerConfigResponse>(cancellationToken: cancellationToken);
        return updated ?? throw new InvalidOperationException("Yapılandırma güncellenemedi.");
    }

    public async Task<List<IntegrationMessageLogResponse>> GetRecentLogsAsync(
        string? systemType = null,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/interoperability/mock-engine/logs?count={count}";
        if (!string.IsNullOrWhiteSpace(systemType))
        {
            url += $"&systemType={Uri.EscapeDataString(systemType)}";
        }

        var result = await _httpClient.GetFromJsonAsync<List<IntegrationMessageLogResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task ResetCircuitBreakerAsync(string systemType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemType);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/mock-engine/reset-circuit/{Uri.EscapeDataString(systemType)}");

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<SimulateMockEngineResponse> SimulateOperationAsync(
        SimulateMockEngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/mock-engine/simulate")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SimulateMockEngineResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Simülasyon işlemi tamamlanamadı.");
    }

    public async Task<Hl7InboundResponse> ProcessHl7InboundAsync(
        Hl7InboundRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/hl7/inbound")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Hl7InboundResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("HL7 yanıtı okunamadı.");
    }

    public async Task<Hl7GenerateResponse> GenerateHl7MessageAsync(
        string messageType,
        Hl7GenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/hl7/generate/{Uri.EscapeDataString(messageType)}")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Hl7GenerateResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("HL7 mesajı üretilemedi.");
    }

    public async Task<List<Hl7DeadLetterResponse>> GetHl7DeadLettersAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<Hl7DeadLetterResponse>>(
            "api/v1/interoperability/hl7/dead-letter",
            cancellationToken);

        return result ?? [];
    }

    public async Task RetryHl7DeadLetterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/hl7/dead-letter/{id}/retry");

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DicomWorklistItemResponse>> QueryModalityWorklistAsync(
        string? modality = null,
        string? scheduledDate = null,
        string? aeTitle = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/v1/interoperability/dicom/worklist?";
        if (!string.IsNullOrWhiteSpace(modality))
        {
            url += $"modality={Uri.EscapeDataString(modality)}&";
        }
        if (!string.IsNullOrWhiteSpace(scheduledDate))
        {
            url += $"scheduledDate={Uri.EscapeDataString(scheduledDate)}&";
        }
        if (!string.IsNullOrWhiteSpace(aeTitle))
        {
            url += $"aeTitle={Uri.EscapeDataString(aeTitle)}&";
        }

        var result = await _httpClient.GetFromJsonAsync<List<DicomWorklistItemResponse>>(url.TrimEnd('&', '?'), cancellationToken);
        return result ?? [];
    }

    public async Task<DicomWorklistItemResponse> CreateDicomWorklistOrderAsync(
        CreateDicomWorklistOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/dicom/worklist")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DicomWorklistItemResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("DICOM MWL oluşturulamadı.");
    }

    public async Task<DicomStudyMetadataResponse?> QueryDicomStudyAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studyInstanceUid);

        return await _httpClient.GetFromJsonAsync<DicomStudyMetadataResponse>(
            $"api/v1/interoperability/dicom/studies/{Uri.EscapeDataString(studyInstanceUid)}",
            cancellationToken);
    }

    public async Task<List<DicomStudyMetadataResponse>> QueryPatientDicomStudiesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<DicomStudyMetadataResponse>>(
            $"api/v1/interoperability/dicom/patients/{patientId}/studies",
            cancellationToken);

        return result ?? [];
    }

    public async Task<List<MhrsSlotResponse>> QueryMhrsSlotsAsync(
        Guid? doctorId = null,
        string? clinicCode = null,
        DateTime? slotDate = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/v1/interoperability/mhrs/slots?";
        if (doctorId.HasValue)
        {
            url += $"doctorId={doctorId.Value}&";
        }
        if (!string.IsNullOrWhiteSpace(clinicCode))
        {
            url += $"clinicCode={Uri.EscapeDataString(clinicCode)}&";
        }
        if (slotDate.HasValue)
        {
            url += $"date={slotDate.Value:yyyy-MM-dd}&";
        }

        var result = await _httpClient.GetFromJsonAsync<List<MhrsSlotResponse>>(url.TrimEnd('&', '?'), cancellationToken);
        return result ?? [];
    }

    public async Task<MhrsAppointmentResponse> BookMhrsAppointmentAsync(
        MhrsBookAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/mhrs/appointments")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MhrsAppointmentResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("MHRS randevusu oluşturulamadı.");
    }

    public async Task<MhrsAppointmentResponse> CancelMhrsAppointmentAsync(
        string mhrsAppointmentId,
        MhrsCancelAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mhrsAppointmentId);
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/mhrs/appointments/{Uri.EscapeDataString(mhrsAppointmentId)}/cancel")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MhrsAppointmentResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("MHRS randevusu iptal edilemedi.");
    }

    public async Task<List<MhrsAppointmentResponse>> GetPatientMhrsAppointmentsAsync(
        string patientNationalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patientNationalId);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/mhrs/patient-appointments/search")
        {
            Content = JsonContent.Create(new MhrsPatientAppointmentsQueryRequest
            {
                PatientNationalId = patientNationalId,
            }),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MhrsAppointmentResponse>>(
            cancellationToken: cancellationToken);

        return result ?? [];
    }

    public async Task<MhrsSyncSummaryResponse> SyncMhrsScheduleAsync(
        DateTime? syncDate = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        var url = "api/v1/interoperability/mhrs/sync";
        if (syncDate.HasValue)
        {
            url += $"?syncDate={syncDate.Value:yyyy-MM-dd}";
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, url);

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MhrsSyncSummaryResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("MHRS senkronizasyonu tamamlanamadı.");
    }

    public async Task<List<ENabizTransmissionResponse>> QueryENabizQueueAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (patientId.HasValue)
        {
            queryParams.Add($"patientId={patientId.Value}");
        }

        var url = "api/v1/interoperability/enabiz/queue";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<ENabizTransmissionResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<ENabizTransmissionResponse> EnqueueENabizPackageAsync(
        EnqueueENabizPackageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/enabiz/queue")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ENabizTransmissionResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("e-Nabız paketi kuyruğa eklenemedi.");
    }

    public async Task<ENabizTransmissionResponse> SendENabizTransmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/enabiz/transmissions/{id}/send");

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ENabizTransmissionResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("e-Nabız gönderim işlemi tamamlanamadı.");
    }

    public async Task<ENabizTransmissionResponse> RetryENabizTransmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/v1/interoperability/enabiz/transmissions/{id}/retry");

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ENabizTransmissionResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("e-Nabız yeniden gönderim işlemi tamamlanamadı.");
    }

    public async Task<ENabizTransmissionResponse?> GetENabizTransmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ENabizTransmissionResponse>(
                $"api/v1/interoperability/enabiz/transmissions/{id}",
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<MedulaOperationResultResponse>> GetMedulaBoundaryInfoAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<MedulaOperationResultResponse>>(
            "api/v1/interoperability/medula/boundaries",
            cancellationToken);
        return result ?? [];
    }

    public async Task<MedulaOperationResultResponse> ExecuteMedulaDemoOperationAsync(
        MedulaDemoOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/medula/demo-operation")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MedulaOperationResultResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("MEDULA demo işlemi tamamlanamadı.");
    }

    public async Task<MedulaOperationResultResponse> RejectMedulaOutOfScopeAsync(
        MedulaOutOfScopeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/interoperability/medula/reject")
        {
            Content = JsonContent.Create(request),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MedulaOperationResultResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("MEDULA kapsam dışı red işlemi tamamlanamadı.");
    }
}
