using System.Text.RegularExpressions;

namespace HospitalManagement.BuildingBlocks.Authorization;

/// <summary>
/// Validates OAuth 2.0 / OIDC redirect/callback URIs for native clients (Windows Desktop & Android Mobile)
/// according to RFC 8252 (BCP 212). Protects against Open Redirect, scheme hijacking, and path traversal.
/// </summary>
public static partial class NativeCallbackUriValidator
{
    public const string CustomScheme = "hospitalmanagement";
    public const string DefaultCustomHost = "auth-callback";
    public const string AlternativeCustomHost = "oauth-callback";

    private static readonly HashSet<string> AllowedCustomHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        DefaultCustomHost,
        AlternativeCustomHost
    };

    private static readonly HashSet<string> AllowedLoopbackHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "127.0.0.1",
        "::1",
        "[::1]",
        "localhost"
    };

    private static readonly HashSet<string> AllowedAppDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "hospital.example.com",
        "demo-hospital.local"
    };

    /// <summary>
    /// Validates whether the given URI is an authorized callback URI for Windows Desktop or Android native clients.
    /// </summary>
    /// <param name="redirectUri">The URI string to validate.</param>
    /// <returns>True if the URI complies with native security specifications; otherwise false.</returns>
    public static bool IsValidNativeCallbackUri(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return false;
        }

        // Reject relative schemes and protocol-relative URIs like "//evil.com"
        if (redirectUri.StartsWith("//", StringComparison.Ordinal) ||
            redirectUri.StartsWith("/\\", StringComparison.Ordinal) ||
            redirectUri.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return false;
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        // Never allow userinfo (e.g. http://attacker:password@127.0.0.1/)
        if (!string.IsNullOrEmpty(parsedUri.UserInfo))
        {
            return false;
        }

        // 1. Loopback IP redirect URI (Windows Desktop RFC 8252 Section 7.3)
        if (IsLoopbackUri(parsedUri))
        {
            return true;
        }

        // 2. Custom URI scheme (Android / Windows Protocol Handler RFC 8252 Section 7.1)
        if (IsCustomSchemeUri(parsedUri))
        {
            return true;
        }

        // 3. Android App Links / Universal Links (HTTPS with allowlisted domains)
        if (IsAppLinksUri(parsedUri))
        {
            return true;
        }

        return false;
    }

    private static bool IsLoopbackUri(Uri uri)
    {
        // Must use HTTP scheme on loopback (RFC 8252 Section 7.3)
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host;
        if (!AllowedLoopbackHosts.Contains(host))
        {
            return false;
        }

        // Port must be a valid TCP port (1-65535) or standard default
        if (uri.Port is < 1 or > 65535)
        {
            return false;
        }

        // Path must be a normalized callback path (not navigating away)
        var path = uri.AbsolutePath.TrimEnd('/');
        return path is "" or "/callback" or "/auth-callback" or "/oauth-callback";
    }

    private static bool IsCustomSchemeUri(Uri uri)
    {
        if (!string.Equals(uri.Scheme, CustomScheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host;
        if (!AllowedCustomHosts.Contains(host))
        {
            return false;
        }

        return true;
    }

    private static bool IsAppLinksUri(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!AllowedAppDomains.Contains(uri.Host))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        return path is "/auth-callback" or "/oauth-callback";
    }
}
