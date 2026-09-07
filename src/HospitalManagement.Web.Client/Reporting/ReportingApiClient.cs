using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Reporting;

namespace HospitalManagement.Web.Client.Reporting;

public sealed class ReportingApiClient : IReportingApiClient
{
    private readonly HttpClient _httpClient;

    public ReportingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<RebuildProjectionsResponse> RebuildProjectionsAsync(
        RebuildProjectionsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/reporting/projections/rebuild")
        {
            Content = JsonContent.Create(request ?? new RebuildProjectionsRequest()),
        };

        if (token is not null)
        {
            message.Headers.Add("X-HMS-CSRF", token.Token);
        }

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RebuildProjectionsResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Projeksiyon yeniden kurma yanıtı alınamadı.");
    }

    public async Task<List<ProjectionCheckpointResponse>> GetCheckpointsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ProjectionCheckpointResponse>>(
            "api/v1/reporting/projections/checkpoints",
            cancellationToken);

        return result ?? [];
    }

    public async Task<List<DailyOutpatientMetricResponse>> GetOutpatientMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        if (doctorId.HasValue)
        {
            queryParams.Add($"doctorId={doctorId.Value}");
        }

        var url = "api/v1/reporting/metrics/outpatient";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<DailyOutpatientMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<DiagnosticWorkloadMetricResponse>> GetDiagnosticMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(modalityOrSection))
        {
            queryParams.Add($"modalityOrSection={Uri.EscapeDataString(modalityOrSection)}");
        }

        var url = "api/v1/reporting/metrics/diagnostics";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<DiagnosticWorkloadMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<BedOccupancyMetricResponse>> GetBedOccupancyMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        string? wardType = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(wardType))
        {
            queryParams.Add($"wardType={Uri.EscapeDataString(wardType)}");
        }

        var url = "api/v1/reporting/metrics/occupancy";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<BedOccupancyMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<PharmacyDispensingMetricResponse>> GetPharmacyMetricsAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/metrics/pharmacy";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<PharmacyDispensingMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<OutpatientDashboardSummaryResponse?> GetOutpatientDashboardSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        Guid? doctorId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        if (doctorId.HasValue)
        {
            queryParams.Add($"doctorId={doctorId.Value}");
        }

        var url = "api/v1/reporting/dashboards/outpatient/summary";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await _httpClient.GetFromJsonAsync<OutpatientDashboardSummaryResponse>(url, cancellationToken);
    }

    public async Task<List<OutpatientDepartmentMetricResponse>> GetOutpatientDepartmentMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/outpatient/departments";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<OutpatientDepartmentMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<OutpatientDoctorMetricResponse>> GetDoctorMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        return await GetOutpatientDoctorMetricsAsync(targetDate, departmentId, cancellationToken);
    }

    public async Task<List<OutpatientDoctorMetricResponse>> GetOutpatientDoctorMetricsAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        var url = "api/v1/reporting/dashboards/outpatient/doctors";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<OutpatientDoctorMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<DiagnosticDashboardSummaryResponse?> GetDiagnosticDashboardSummaryAsync(
        DateOnly? targetDate = null,
        string? modalityOrSection = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(modalityOrSection))
        {
            queryParams.Add($"modalityOrSection={Uri.EscapeDataString(modalityOrSection)}");
        }

        var url = "api/v1/reporting/dashboards/diagnostics/summary";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await _httpClient.GetFromJsonAsync<DiagnosticDashboardSummaryResponse>(url, cancellationToken);
    }

    public async Task<List<DiagnosticModalityMetricResponse>> GetDiagnosticModalityMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/diagnostics/modalities";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<DiagnosticModalityMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<DiagnosticCriticalAlertMetricResponse>> GetDiagnosticCriticalAlertMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/diagnostics/critical-alerts";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<DiagnosticCriticalAlertMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<InpatientOperationsSummaryResponse?> GetInpatientOperationsSummaryAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        var url = "api/v1/reporting/dashboards/inpatient-operations/summary";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await _httpClient.GetFromJsonAsync<InpatientOperationsSummaryResponse>(url, cancellationToken);
    }

    public async Task<List<WardOccupancyDetailResponse>> GetWardOccupancyAsync(
        DateOnly? targetDate = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        if (departmentId.HasValue)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        var url = "api/v1/reporting/dashboards/inpatient-operations/wards";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<WardOccupancyDetailResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<EmergencyTriageQueueMetricResponse>> GetEmergencyTriageMetricsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/inpatient-operations/emergency-triage";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<EmergencyTriageQueueMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<PharmacyDashboardSummaryResponse?> GetPharmacyDashboardSummaryAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/pharmacy/summary";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return await _httpClient.GetFromJsonAsync<PharmacyDashboardSummaryResponse>(url, cancellationToken);
    }

    public async Task<List<PharmacyStockAlertMetricResponse>> GetPharmacyStockAlertsAsync(
        DateOnly? targetDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();

        if (targetDate.HasValue)
        {
            queryParams.Add($"date={targetDate.Value:yyyy-MM-dd}");
        }

        var url = "api/v1/reporting/dashboards/pharmacy/stock-alerts";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<PharmacyStockAlertMetricResponse>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<List<ProjectionLagInfo>> GetProjectionLagAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ProjectionLagInfo>>(
            "api/v1/reporting/projections/lag",
            cancellationToken);

        return result ?? [];
    }
}



