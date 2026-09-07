using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Inpatient;

namespace HospitalManagement.Web.Client.Inpatient;

public sealed class InpatientApiClient : IInpatientApiClient
{
    private readonly HttpClient _httpClient;

    public InpatientApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<WardResponse>> GetWardsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = isActive.HasValue
                ? $"api/v1/inpatient/wards?isActive={isActive.Value.ToString().ToLowerInvariant()}"
                : "api/v1/inpatient/wards";
            var result = await _httpClient.GetFromJsonAsync<List<WardResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<WardResponse?> GetWardByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WardResponse>($"api/v1/inpatient/wards/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<WardResponse>> GetTransferDestinationWardsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<WardResponse>>(
                "api/v1/inpatient/wards/transfer-destinations",
                cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<RoomResponse>> GetRoomsByWardIdAsync(
        Guid wardId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<RoomResponse>>($"api/v1/inpatient/wards/{wardId}/rooms", cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<BedResponse>> GetBedsAsync(
        Guid? wardId = null,
        Guid? roomId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            if (roomId.HasValue && roomId.Value != Guid.Empty)
            {
                queryParams.Add($"roomId={roomId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            var uri = "api/v1/inpatient/beds" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<BedResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<BedResponse?> GetBedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BedResponse>($"api/v1/inpatient/beds/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<BedResponse?> UpdateBedStatusAsync(
        Guid id,
        UpdateBedStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/beds/{id}/status")
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

        return await response.Content.ReadFromJsonAsync<BedResponse>(cancellationToken);
    }

    public async Task<BedOccupancySummaryResponse?> GetOccupancySummaryAsync(
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = wardId.HasValue && wardId.Value != Guid.Empty
                ? $"api/v1/inpatient/occupancy-summary?wardId={wardId.Value}"
                : "api/v1/inpatient/occupancy-summary";
            return await _httpClient.GetFromJsonAsync<BedOccupancySummaryResponse>(uri, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdmissionResponse?> RequestAdmissionAsync(
        CreateAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/admissions")
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

        return await response.Content.ReadFromJsonAsync<AdmissionResponse>(cancellationToken);
    }

    public async Task<AdmissionResponse?> AcceptAdmissionAsync(
        Guid id,
        AcceptAdmissionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/admissions/{id}/accept")
        {
            Content = request is not null ? JsonContent.Create(request) : null,
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

        return await response.Content.ReadFromJsonAsync<AdmissionResponse>(cancellationToken);
    }

    public async Task<AdmissionResponse?> AdmitPatientAsync(
        Guid id,
        AdmitPatientRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/admissions/{id}/admit")
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

        return await response.Content.ReadFromJsonAsync<AdmissionResponse>(cancellationToken);
    }

    public async Task<AdmissionResponse?> CancelAdmissionAsync(
        Guid id,
        CancelAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/admissions/{id}/cancel")
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

        return await response.Content.ReadFromJsonAsync<AdmissionResponse>(cancellationToken);
    }

    public async Task<AdmissionResponse?> UpdateCareDetailsAsync(
        Guid id,
        UpdateCareDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/admissions/{id}/care-details")
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

        return await response.Content.ReadFromJsonAsync<AdmissionResponse>(cancellationToken);
    }

    public async Task<AdmissionResponse?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdmissionResponse>($"api/v1/inpatient/admissions/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdmissionResponse?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AdmissionResponse>($"api/v1/inpatient/admissions/active/by-patient/{patientId}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<AdmissionSummaryResponse>> GetAdmissionsAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            if (departmentId.HasValue && departmentId.Value != Guid.Empty)
            {
                queryParams.Add($"departmentId={departmentId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (patientId.HasValue && patientId.Value != Guid.Empty)
            {
                queryParams.Add($"patientId={patientId.Value}");
            }

            var uri = "api/v1/inpatient/admissions" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<AdmissionSummaryResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<TransferResponse?> RequestTransferAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/transfers")
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

        return await response.Content.ReadFromJsonAsync<TransferResponse>(cancellationToken);
    }

    public async Task<TransferResponse?> AcceptTransferAsync(
        Guid id,
        AcceptTransferRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/transfers/{id}/accept")
        {
            Content = request is not null ? JsonContent.Create(request) : null,
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

        return await response.Content.ReadFromJsonAsync<TransferResponse>(cancellationToken);
    }

    public async Task<TransferResponse?> CompleteTransferAsync(
        Guid id,
        CompleteTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/transfers/{id}/complete")
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

        return await response.Content.ReadFromJsonAsync<TransferResponse>(cancellationToken);
    }

    public async Task<TransferResponse?> CancelTransferAsync(
        Guid id,
        CancelTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/transfers/{id}/cancel")
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

        return await response.Content.ReadFromJsonAsync<TransferResponse>(cancellationToken);
    }

    public async Task<TransferResponse?> GetTransferByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TransferResponse>($"api/v1/inpatient/transfers/{id}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TransferSummaryResponse>> GetTransfersAsync(
        Guid? admissionId = null,
        Guid? sourceWardId = null,
        Guid? targetWardId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (admissionId.HasValue && admissionId.Value != Guid.Empty)
            {
                queryParams.Add($"admissionId={admissionId.Value}");
            }

            if (sourceWardId.HasValue && sourceWardId.Value != Guid.Empty)
            {
                queryParams.Add($"sourceWardId={sourceWardId.Value}");
            }

            if (targetWardId.HasValue && targetWardId.Value != Guid.Empty)
            {
                queryParams.Add($"targetWardId={targetWardId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            var uri = "api/v1/inpatient/transfers" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<TransferSummaryResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<InpatientBoardItemResponse>> GetInpatientBoardAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? riskLevel = null,
        bool? isolationOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            if (departmentId.HasValue && departmentId.Value != Guid.Empty)
            {
                queryParams.Add($"departmentId={departmentId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(riskLevel))
            {
                queryParams.Add($"riskLevel={Uri.EscapeDataString(riskLevel)}");
            }

            if (isolationOnly == true)
            {
                queryParams.Add("isolationOnly=true");
            }

            var uri = "api/v1/inpatient/board" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<InpatientBoardItemResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<InpatientPatientSummaryResponse?> GetPatientSummaryAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InpatientPatientSummaryResponse>($"api/v1/inpatient/board/{admissionId}/summary", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<NursingObservationResponse?> RecordObservationAsync(
        RecordObservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/nursing/observations")
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

        return await response.Content.ReadFromJsonAsync<NursingObservationResponse>(cancellationToken);
    }

    public async Task<NursingObservationResponse?> RecordObservationCorrectionAsync(
        Guid id,
        CorrectObservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/nursing/observations/{id}/correct")
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

        return await response.Content.ReadFromJsonAsync<NursingObservationResponse>(cancellationToken);
    }

    public async Task<List<NursingObservationResponse>> GetObservationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<NursingObservationResponse>>($"api/v1/inpatient/nursing/observations?admissionId={admissionId}", cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<NursingCarePlanResponse?> CreateCarePlanAsync(
        CreateCarePlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/nursing/care-plans")
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

        return await response.Content.ReadFromJsonAsync<NursingCarePlanResponse>(cancellationToken);
    }

    public async Task<NursingCareTaskResponse?> AddTaskToCarePlanAsync(
        Guid carePlanId,
        AddCareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/nursing/care-plans/{carePlanId}/tasks")
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

        return await response.Content.ReadFromJsonAsync<NursingCareTaskResponse>(cancellationToken);
    }

    public async Task<NursingCareTaskResponse?> CompleteCareTaskAsync(
        Guid taskId,
        CompleteCareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/nursing/tasks/{taskId}/complete")
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

        return await response.Content.ReadFromJsonAsync<NursingCareTaskResponse>(cancellationToken);
    }

    public async Task<NursingCareTaskResponse?> CancelCareTaskAsync(
        Guid taskId,
        CancelCareTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/nursing/tasks/{taskId}/cancel")
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

        return await response.Content.ReadFromJsonAsync<NursingCareTaskResponse>(cancellationToken);
    }

    public async Task<List<NursingCarePlanResponse>> GetCarePlansByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<NursingCarePlanResponse>>($"api/v1/inpatient/nursing/care-plans?admissionId={admissionId}", cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<NursingCareTaskResponse>> GetOverdueTasksAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (admissionId.HasValue && admissionId.Value != Guid.Empty)
            {
                queryParams.Add($"admissionId={admissionId.Value}");
            }

            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            var uri = "api/v1/inpatient/nursing/tasks/overdue" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<NursingCareTaskResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<ActiveMedicationOrderResponse>> GetActiveMedicationOrdersAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<ActiveMedicationOrderResponse>>(
                $"api/v1/inpatient/emar/orders/admission/{admissionId}",
                cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<MedicationAdministrationResponse?> ScheduleMedicationAsync(
        ScheduleMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/emar/schedule")
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

        return await response.Content.ReadFromJsonAsync<MedicationAdministrationResponse>(cancellationToken);
    }

    public async Task<MedicationAdministrationResponse?> AdministerMedicationAsync(
        Guid id,
        AdministerMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/emar/{id}/administer")
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

        return await response.Content.ReadFromJsonAsync<MedicationAdministrationResponse>(cancellationToken);
    }

    public async Task<MedicationAdministrationResponse?> SkipMedicationAsync(
        Guid id,
        SkipMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/emar/{id}/skip")
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

        return await response.Content.ReadFromJsonAsync<MedicationAdministrationResponse>(cancellationToken);
    }

    public async Task<MedicationAdministrationResponse?> RefuseMedicationAsync(
        Guid id,
        RefuseMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/emar/{id}/refuse")
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

        return await response.Content.ReadFromJsonAsync<MedicationAdministrationResponse>(cancellationToken);
    }

    public async Task<MedicationAdministrationResponse?> DelayMedicationAsync(
        Guid id,
        DelayMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"api/v1/inpatient/emar/{id}/delay")
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

        return await response.Content.ReadFromJsonAsync<MedicationAdministrationResponse>(cancellationToken);
    }

    public async Task<List<MedicationAdministrationResponse>> GetMedicationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<MedicationAdministrationResponse>>($"api/v1/inpatient/emar/admission/{admissionId}", cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<MedicationAdministrationResponse>> GetDueMedicationsAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (admissionId.HasValue && admissionId.Value != Guid.Empty)
            {
                queryParams.Add($"admissionId={admissionId.Value}");
            }

            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            var uri = "api/v1/inpatient/emar/due" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<MedicationAdministrationResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<InpatientDischargeResponse?> ProcessDischargeAsync(
        DischargeAdmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "api/v1/inpatient/discharges")
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

        return await response.Content.ReadFromJsonAsync<InpatientDischargeResponse>(cancellationToken);
    }

    public async Task<InpatientDischargeResponse?> GetDischargeByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<InpatientDischargeResponse>($"api/v1/inpatient/discharges/{admissionId}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<InpatientDischargeResponse>> GetDischargesAsync(
        Guid? patientId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (patientId.HasValue && patientId.Value != Guid.Empty)
            {
                queryParams.Add($"patientId={patientId.Value}");
            }

            if (fromDateUtc.HasValue)
            {
                queryParams.Add($"fromDate={fromDateUtc.Value:O}");
            }

            if (toDateUtc.HasValue)
            {
                queryParams.Add($"toDate={toDateUtc.Value:O}");
            }

            var uri = "api/v1/inpatient/discharges" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<InpatientDischargeResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<InpatientDashboardResponse?> GetDashboardSummaryAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (wardId.HasValue && wardId.Value != Guid.Empty)
            {
                queryParams.Add($"wardId={wardId.Value}");
            }

            if (departmentId.HasValue && departmentId.Value != Guid.Empty)
            {
                queryParams.Add($"departmentId={departmentId.Value}");
            }

            var uri = "api/v1/inpatient/dashboard" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            return await _httpClient.GetFromJsonAsync<InpatientDashboardResponse>(uri, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>("api/v1/identity/antiforgery", cancellationToken);
            return result?.Token;
        }
        catch
        {
            return null;
        }
    }
}
