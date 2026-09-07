namespace HospitalManagement.Host.Api;

public static class ApiConstants
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string ProblemContentType = "application/problem+json";
    public const string RateLimitPolicyName = "api-v1-fixed-window";
    public const string ReadinessTag = "ready";
    public const string Version = "v1";
    public const string VersionOnePrefix = "/api/v1";
}
