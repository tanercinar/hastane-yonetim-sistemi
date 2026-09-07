using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HospitalManagement.Host.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.Headers.Pragma = "no-cache";

        var response = new HealthEndpointResponse(
            report.Status.ToString(),
            report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new HealthCheckResponse(entry.Key, entry.Value.Status.ToString()))
                .ToArray());

        return context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    }

    private sealed record HealthEndpointResponse(
        string Status,
        IReadOnlyList<HealthCheckResponse> Checks);

    private sealed record HealthCheckResponse(string Name, string Status);
}
