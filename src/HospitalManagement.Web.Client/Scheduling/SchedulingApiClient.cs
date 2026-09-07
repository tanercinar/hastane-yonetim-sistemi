using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Scheduling;

namespace HospitalManagement.Web.Client.Scheduling;

public sealed class SchedulingApiClient(HttpClient httpClient) : ISchedulingApiClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<IReadOnlyList<DoctorScheduleResponse>?> GetDoctorSchedulesAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/scheduling/schedules/by-doctor/{doctorId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<DoctorScheduleResponse>>(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorAvailabilityDayResponse>?> GetDoctorAvailabilityAsync(
        Guid doctorId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? timeZoneId = null,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/scheduling/availability/by-doctor/{doctorId}";
        var queryParams = new List<string>();

        if (startDate.HasValue)
        {
            queryParams.Add($"startDate={startDate.Value:O}");
        }

        if (endDate.HasValue)
        {
            queryParams.Add($"endDate={endDate.Value:O}");
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            queryParams.Add($"timeZoneId={Uri.EscapeDataString(timeZoneId)}");
        }

        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<DoctorAvailabilityDayResponse>>(cancellationToken);
    }

    public async Task<AppointmentDetailResponse?> BookAppointmentAsync(
        BookAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/scheduling/appointments/book")
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

        return await response.Content.ReadFromJsonAsync<AppointmentDetailResponse>(cancellationToken);
    }

    public async Task<AppointmentDetailResponse?> CancelAppointmentAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/scheduling/appointments/{appointmentId}/cancel")
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

        return await response.Content.ReadFromJsonAsync<AppointmentDetailResponse>(cancellationToken);
    }

    public async Task<AppointmentDetailResponse?> CheckInAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/scheduling/appointments/{appointmentId}/check-in");

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AppointmentDetailResponse>(cancellationToken);
    }

    public async Task<AppointmentDetailResponse?> MarkNoShowAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/scheduling/appointments/{appointmentId}/no-show");

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AppointmentDetailResponse>(cancellationToken);
    }

    public async Task<AppointmentDetailResponse?> GetAppointmentByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/scheduling/appointments/{appointmentId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AppointmentDetailResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDetailResponse>?> GetPatientAppointmentsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/scheduling/appointments/by-patient/{patientId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentDetailResponse>>(cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDetailResponse>?> GetDailyAppointmentsAsync(
        DateOnly? appointmentDate = null,
        Guid? doctorId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var uri = "api/v1/scheduling/appointments/daily";
        var queryParams = new List<string>();

        if (appointmentDate.HasValue)
        {
            queryParams.Add($"date={appointmentDate.Value:O}");
        }

        if (doctorId.HasValue && doctorId.Value != Guid.Empty)
        {
            queryParams.Add($"doctorId={doctorId.Value}");
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            queryParams.Add($"departmentId={departmentId.Value}");
        }

        if (queryParams.Count > 0)
        {
            uri += "?" + string.Join("&", queryParams);
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<AppointmentDetailResponse>>(cancellationToken);
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
