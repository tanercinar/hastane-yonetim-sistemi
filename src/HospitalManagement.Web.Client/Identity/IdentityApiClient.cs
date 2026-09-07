using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HospitalManagement.Contracts.Identity;

namespace HospitalManagement.Web.Client.Identity;

public sealed class IdentityApiClient(HttpClient httpClient)
{
    private const string AntiforgeryHeaderName = "X-HMS-CSRF";
    private readonly HttpClient _httpClient = httpClient;

    public Task<IdentityApiResult> RegisterPatientAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/patient-registrations", request, cancellationToken);

    public Task<IdentityApiResult> ConfirmPatientEmailAsync(
        ConfirmPatientEmailRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/patient-email-confirmations", request, cancellationToken);

    public Task<IdentityApiResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/sessions", request, cancellationToken);

    public Task<IdentityApiResult> TwoFactorLoginAsync(
        TwoFactorLoginRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/two-factor-sessions", request, cancellationToken);

    public Task<IdentityApiResult> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/password-reset-requests", request, cancellationToken);

    public Task<IdentityApiResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/password-resets", request, cancellationToken);

    public Task<IdentityApiResult> AcceptStaffInvitationAsync(
        AcceptStaffInvitationRequest request,
        CancellationToken cancellationToken = default) =>
        PostAsync("api/v1/identity/staff-invitation-acceptances", request, cancellationToken);

    public async Task<IdentityApiResult> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/identity/sessions/logout");
        request.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await CreateResultAsync(response, cancellationToken);
    }

    public async Task<CurrentAccountResponse?> GetCurrentAccountAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/v1/identity/session", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CurrentAccountResponse>(
            cancellationToken: cancellationToken);
    }

    public async Task<UserListResponse?> GetUsersAsync(
        string? query = null,
        bool? isEnabled = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var uri = $"api/v1/identity/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            uri += $"&query={Uri.EscapeDataString(query)}";
        }
        if (isEnabled.HasValue)
        {
            uri += $"&isEnabled={isEnabled.Value}";
        }

        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserListResponse>(cancellationToken: cancellationToken);
    }

    public Task<IdentityApiResult> UpdateUserStatusAsync(
        Guid userId,
        bool isEnabled,
        CancellationToken cancellationToken = default) =>
        PostAsync($"api/v1/identity/users/{userId}/status", new UpdateUserStatusRequest { IsEnabled = isEnabled }, cancellationToken);

    public Task<IdentityApiResult> UpdateUserRolesAsync(
        Guid userId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default) =>
        PostAsync($"api/v1/identity/users/{userId}/roles", new UpdateUserRolesRequest { Roles = roles }, cancellationToken);

    private async Task<IdentityApiResult> PostAsync<TRequest>(
        string requestUri,
        TRequest body,
        CancellationToken cancellationToken)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.TryAddWithoutValidation(AntiforgeryHeaderName, token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await CreateResultAsync(response, cancellationToken);
    }

    private async Task<string> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/v1/identity/antiforgery",
            cancellationToken);
        return response?.Token
            ?? throw new InvalidOperationException("CSRF doğrulama kodu alınamadı.");
    }

    private static async Task<IdentityApiResult> CreateResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return IdentityApiResult.Success("İşlem tamamlandı.");
            }

            var success = await response.Content.ReadFromJsonAsync<LoginResponse>(
                cancellationToken: cancellationToken);
            return IdentityApiResult.Success(
                success?.Message ?? "İşlem tamamlandı.",
                success?.RequiresTwoFactor ?? false);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(content))
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                var firstError = errors.EnumerateObject()
                    .SelectMany(property => property.Value.EnumerateArray())
                    .Select(item => item.GetString())
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                if (firstError is not null)
                {
                    return IdentityApiResult.Failure(firstError);
                }
            }
            if (document.RootElement.TryGetProperty("title", out var title)
                && !string.IsNullOrWhiteSpace(title.GetString()))
            {
                return IdentityApiResult.Failure(title.GetString()!);
            }
        }

        return IdentityApiResult.Failure("İşlem tamamlanamadı. Lütfen tekrar deneyin.");
    }
}

public sealed record IdentityApiResult(bool Succeeded, string Message, bool RequiresTwoFactor = false)
{
    public static IdentityApiResult Success(string message, bool requiresTwoFactor = false) =>
        new(true, message, requiresTwoFactor);

    public static IdentityApiResult Failure(string message) => new(false, message);
}
