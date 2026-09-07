namespace HospitalManagement.Host.Observability;

internal static partial class ObservabilityLog
{
    internal const int RequestCompletedEventId = 2100;
    internal const int RedactionProbeEventId = 2199;

    [LoggerMessage(
        EventId = RequestCompletedEventId,
        Message = "HTTP request completed. Method={RequestMethod} Route={RouteTemplate} StatusCode={StatusCode} ElapsedMilliseconds={ElapsedMilliseconds} CorrelationId={CorrelationId} TraceId={TraceId}")]
    internal static partial void LogRequestCompleted(
        ILogger logger,
        LogLevel logLevel,
        string requestMethod,
        string routeTemplate,
        int statusCode,
        double elapsedMilliseconds,
        string correlationId,
        string traceId);

    [LoggerMessage(
        EventId = RedactionProbeEventId,
        Level = LogLevel.Information,
        Message = "Classified telemetry redaction probe completed. ClinicalContent={ClinicalContent}")]
    internal static partial void LogRedactionProbe(
        ILogger logger,
        [ClinicalData] string clinicalContent);
}
