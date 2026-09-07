using System.Text.RegularExpressions;

namespace HospitalManagement.UI.Notifications;

public sealed record DeepLinkRouteResult
{
    public bool IsValid
    {
        get;
        init;
    }

    public bool IsAuthorized
    {
        get;
        init;
    } = true;

    public string TargetRoute
    {
        get;
        init;
    } = "/";

    public string? ErrorMessage
    {
        get;
        init;
    }

    public string? ResourceId
    {
        get;
        init;
    }

    public static DeepLinkRouteResult Success(string route, string? resourceId = null) => new()
    {
        IsValid = true,
        IsAuthorized = true,
        TargetRoute = route,
        ResourceId = resourceId
    };

    public static DeepLinkRouteResult Invalid(string error) => new()
    {
        IsValid = false,
        IsAuthorized = false,
        TargetRoute = "/",
        ErrorMessage = error
    };

    public static DeepLinkRouteResult Forbidden(string error) => new()
    {
        IsValid = true,
        IsAuthorized = false,
        TargetRoute = "/forbidden",
        ErrorMessage = error
    };
}

/// <summary>
/// Validates and parses custom URL scheme deep links (e.g. hospitalapp://...)
/// ensuring strict protection against path traversal, arbitrary redirect, and role bypass.
/// </summary>
public static partial class NativeDeepLinkRouter
{
    public const string AllowedScheme = "hospitalapp";

    [GeneratedRegex(@"^[a-zA-Z0-9\-_]{1,64}$")]
    private static partial Regex SafeResourceIdRegex();

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "appointments",
        "prescriptions",
        "results",
        "staff-workspace",
        "home"
    };

    public static DeepLinkRouteResult Route(string? rawUri, string userRole, bool isAuthenticated)
    {
        if (string.IsNullOrWhiteSpace(rawUri))
        {
            return DeepLinkRouteResult.Invalid("Boş bağlantı adresi.");
        }

        // 1. Uri Parsing & Scheme Validation
        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
        {
            return DeepLinkRouteResult.Invalid("Geçersiz URI formatı.");
        }

        if (!string.Equals(uri.Scheme, AllowedScheme, StringComparison.OrdinalIgnoreCase))
        {
            return DeepLinkRouteResult.Invalid($"Yetkisiz URI şeması: {uri.Scheme}. Yalnızca {AllowedScheme}:// desteklenir.");
        }

        // 2. Traversal, Path & Host Validation
        if (rawUri.Contains("..", StringComparison.Ordinal) ||
            rawUri.Contains('\\', StringComparison.Ordinal) ||
            rawUri.IndexOf("//", AllowedScheme.Length + 3, StringComparison.Ordinal) >= 0)
        {
            return DeepLinkRouteResult.Invalid("Geçersiz veya güvenlik kuralına aykırı yol karakteri tespit edildi.");
        }

        var host = uri.Host.ToLowerInvariant();
        if (!AllowedHosts.Contains(host))
        {
            return DeepLinkRouteResult.Invalid($"Bilinmeyen veya desteklenmeyen hedef: {host}");
        }

        if (uri.AbsolutePath != "/" && uri.AbsolutePath != string.Empty)
        {
            return DeepLinkRouteResult.Invalid("Geçersiz veya güvenlik kuralına aykırı yol karakteri tespit edildi.");
        }

        // 3. Query Parameter Validation
        string? resourceId = null;
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var rawId = queryParams.Get("id");
            if (!string.IsNullOrWhiteSpace(rawId))
            {
                // Validate alphanumeric + hyphen format (e.g., GUID or DEMO-APT-01)
                if (!SafeResourceIdRegex().IsMatch(rawId))
                {
                    return DeepLinkRouteResult.Invalid("Geçersiz kaynak kimlik formatı.");
                }
                resourceId = rawId;
            }
        }

        // 4. Role-based Route Authorization
        if (host == "staff-workspace")
        {
            if (!isAuthenticated)
            {
                return DeepLinkRouteResult.Forbidden("Personel çalışma alanına erişmek için giriş yapmalısınız.");
            }

            var isStaff = userRole.Equals("Doctor", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("Nurse", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("RegistrationStaff", StringComparison.OrdinalIgnoreCase) ||
                          userRole.Equals("SystemAdministrator", StringComparison.OrdinalIgnoreCase);

            if (!isStaff)
            {
                return DeepLinkRouteResult.Forbidden("Bu alana erişim yetkiniz bulunmamaktadır.");
            }

            return DeepLinkRouteResult.Success("/staff/workspace", resourceId);
        }

        // Patient-accessible routes
        return host switch
        {
            "appointments" => DeepLinkRouteResult.Success(
                string.IsNullOrEmpty(resourceId) ? "/patient/appointments" : $"/patient/appointments/{resourceId}",
                resourceId),

            "prescriptions" => DeepLinkRouteResult.Success(
                string.IsNullOrEmpty(resourceId) ? "/patient/prescriptions" : $"/patient/prescriptions/{resourceId}",
                resourceId),

            "results" => DeepLinkRouteResult.Success(
                string.IsNullOrEmpty(resourceId) ? "/patient/diagnostic-results" : $"/patient/diagnostic-results/{resourceId}",
                resourceId),

            "home" => DeepLinkRouteResult.Success("/", resourceId),

            _ => DeepLinkRouteResult.Invalid("Desteklenmeyen rota.")
        };
    }
}
