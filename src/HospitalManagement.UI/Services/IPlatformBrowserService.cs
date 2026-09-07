namespace HospitalManagement.UI.Services;

/// <summary>
/// Provides system browser launching for RFC 8252 OAuth 2.0 PKCE authentication flows.
/// Ensures user credentials are not handled inside embedded webviews.
/// </summary>
public interface IPlatformBrowserService
{
    Task OpenSystemBrowserAsync(Uri uri, CancellationToken cancellationToken = default);
}
