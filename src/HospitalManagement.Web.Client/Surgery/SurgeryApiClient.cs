using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public sealed class SurgeryApiClient : ISurgeryApiClient
{
    private readonly HttpClient _httpClient;

    public SurgeryApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<List<OperatingRoomResponse>> GetOperatingRoomsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<OperatingRoomResponse>>(
                "api/v1/surgery/operating-rooms",
                cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<SurgeryBookingResponse?> CreateBookingAsync(
        CreateSurgeryBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/surgery/bookings");
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

            return await response.Content.ReadFromJsonAsync<SurgeryBookingResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<SurgeryBookingResponse?> RescheduleBookingAsync(
        Guid id,
        RescheduleSurgeryBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/surgery/bookings/{id}/reschedule");
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

            return await response.Content.ReadFromJsonAsync<SurgeryBookingResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<SurgeryBookingResponse?> RecordPreOpChecklistAsync(
        Guid id,
        RecordPreOpChecklistRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/surgery/bookings/{id}/pre-op-checklist");
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

            return await response.Content.ReadFromJsonAsync<SurgeryBookingResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<SurgeryBookingResponse?> CancelBookingAsync(
        Guid id,
        CancelSurgeryBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/surgery/bookings/{id}/cancel");
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

            return await response.Content.ReadFromJsonAsync<SurgeryBookingResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<SurgeryBookingResponse?> GetBookingByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<SurgeryBookingResponse>(
                $"api/v1/surgery/bookings/{id}",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<List<SurgeryBookingResponse>> GetBookingsAsync(
        Guid? operatingRoomId = null,
        Guid? leadSurgeonId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (operatingRoomId.HasValue)
            {
                queryParams.Add($"operatingRoomId={operatingRoomId.Value}");
            }
            if (leadSurgeonId.HasValue)
            {
                queryParams.Add($"leadSurgeonId={leadSurgeonId.Value}");
            }
            if (fromDate.HasValue)
            {
                queryParams.Add($"fromDate={Uri.EscapeDataString(fromDate.Value.ToString("O"))}");
            }
            if (toDate.HasValue)
            {
                queryParams.Add($"toDate={Uri.EscapeDataString(toDate.Value.ToString("O"))}");
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            var uri = "api/v1/surgery/bookings" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var result = await _httpClient.GetFromJsonAsync<List<SurgeryBookingResponse>>(uri, cancellationToken);
            return result ?? [];
        }
        catch
        {
            throw;
        }
    }

    public async Task<PerioperativeRecordResponse?> SavePerioperativeRecordAsync(
        SavePerioperativeRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/surgery/perioperative-records");
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

            return await response.Content.ReadFromJsonAsync<PerioperativeRecordResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<PerioperativeRecordResponse?> GetPerioperativeRecordByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PerioperativeRecordResponse>(
                $"api/v1/surgery/perioperative-records/by-booking/{bookingId}",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<PerioperativeRecordResponse?> GetPerioperativeRecordByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PerioperativeRecordResponse>(
                $"api/v1/surgery/perioperative-records/{id}",
                cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<PerioperativeRecordResponse?> SignPerioperativeRecordAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/surgery/perioperative-records/{id}/sign");
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("X-HMS-CSRF", token);
            }
            req.Content = JsonContent.Create(new SignPerioperativeRecordRequest());

            var response = await _httpClient.SendAsync(req, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PerioperativeRecordResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    public async Task<PerioperativeCorrectionResponse?> AddPerioperativeCorrectionAsync(
        Guid id,
        AddPerioperativeCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var token = await GetAntiforgeryTokenAsync(cancellationToken);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"api/v1/surgery/perioperative-records/{id}/corrections");
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

            return await response.Content.ReadFromJsonAsync<PerioperativeCorrectionResponse>(cancellationToken: cancellationToken);
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
