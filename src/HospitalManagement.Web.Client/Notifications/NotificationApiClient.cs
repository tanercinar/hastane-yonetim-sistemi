using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Notifications;

namespace HospitalManagement.Web.Client.Notifications;

public sealed class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;

    public NotificationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<NotificationDetailResponse>?> GetMyNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/v1/notifications/my", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<NotificationDetailResponse>>(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var csrf = await GetAntiforgeryTokenAsync(cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"api/v1/notifications/{notificationId}/read");

        if (!string.IsNullOrWhiteSpace(csrf))
        {
            httpRequest.Headers.Add("X-HMS-CSRF", csrf);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<string?> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/v1/identity/antiforgery", cancellationToken);
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
