namespace HospitalManagement.UI.Client;

/// <summary>
/// Service contract for handling OAuth 2.0 / OIDC Bearer token lifecycle and refresh for native clients.
/// </summary>
public interface INativeTokenRefreshService
{
    Task<string?> GetCurrentAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);

    Task InvalidateSessionAsync(CancellationToken cancellationToken = default);
}
