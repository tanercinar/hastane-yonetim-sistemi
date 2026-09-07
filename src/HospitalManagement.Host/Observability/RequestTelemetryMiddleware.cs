using System.Diagnostics;

using Microsoft.AspNetCore.Routing;

namespace HospitalManagement.Host.Observability;

public sealed class RequestTelemetryMiddleware(
    RequestDelegate next,
    ILogger<RequestTelemetryMiddleware> logger)
{
    private const string UnmatchedRoute = "unmatched";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startedAt = Stopwatch.GetTimestamp();
        var requestMethod = GetSafeRequestMethod(context.Request.Method);
        var routeTemplate = GetSafeRouteTemplate(context);

        using var activity = ObservabilityTelemetry.RequestActivitySource.StartActivity(
            ObservabilityTelemetry.RequestActivityName,
            ActivityKind.Internal);

        activity?.SetTag("http.request.method", requestMethod);
        activity?.SetTag("http.route", routeTemplate);
        activity?.SetTag("hospital.correlation_id", context.TraceIdentifier);

        try
        {
            await next(context);
        }
        finally
        {
            var statusCode = context.Response.StatusCode;
            var elapsedMilliseconds = Math.Round(
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                digits: 3);
            var traceId = activity?.TraceId.ToHexString()
                ?? Activity.Current?.TraceId.ToHexString()
                ?? "unavailable";

            activity?.SetTag("http.response.status_code", statusCode);
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            var metricTags = new TagList
            {
                { "http.request.method", requestMethod },
                { "http.route", routeTemplate },
                { "http.response.status_code", statusCode },
            };
            ObservabilityTelemetry.RequestDuration.Record(elapsedMilliseconds, metricTags);

            var logLevel = GetLogLevel(statusCode);
            if (logger.IsEnabled(logLevel))
            {
                ObservabilityLog.LogRequestCompleted(
                    logger,
                    logLevel,
                    requestMethod,
                    routeTemplate,
                    statusCode,
                    elapsedMilliseconds,
                    context.TraceIdentifier,
                    traceId);
            }
        }
    }

    private static string GetSafeRouteTemplate(HttpContext context)
    {
        return context.GetEndpoint() is RouteEndpoint routeEndpoint
            && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText)
                ? routeEndpoint.RoutePattern.RawText
                : UnmatchedRoute;
    }

    private static string GetSafeRequestMethod(string requestMethod)
    {
        return requestMethod.ToUpperInvariant() switch
        {
            "DELETE" => "DELETE",
            "GET" => "GET",
            "HEAD" => "HEAD",
            "OPTIONS" => "OPTIONS",
            "PATCH" => "PATCH",
            "POST" => "POST",
            "PUT" => "PUT",
            _ => "OTHER",
        };
    }

    private static LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= StatusCodes.Status500InternalServerError => LogLevel.Error,
            >= StatusCodes.Status400BadRequest => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}
