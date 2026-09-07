namespace HospitalManagement.Host.Api;

public static class ApiRequestClassifier
{
    public static bool IsApiRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return IsApiPath(context.Request.Path);
    }

    public static bool IsApiPath(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);
    }
}
