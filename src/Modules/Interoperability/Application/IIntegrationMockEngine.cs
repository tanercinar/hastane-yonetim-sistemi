using HospitalManagement.Modules.Interoperability.Domain;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record MockServerConfigDto(
    Guid Id,
    string SystemType,
    bool IsEnabled,
    string FaultMode,
    int LatencyMilliseconds,
    int FailureRatePercentage,
    int MaxRetryAttempts,
    int TimeoutSeconds,
    DateTime UpdatedAtUtc);

public sealed record UpdateMockServerConfigRequest(
    string SystemType,
    bool IsEnabled,
    string FaultMode,
    int LatencyMilliseconds,
    int FailureRatePercentage,
    int MaxRetryAttempts,
    int TimeoutSeconds);

public sealed record IntegrationMessageLogDto(
    Guid Id,
    string CorrelationId,
    string SystemType,
    string Direction,
    string ActionName,
    string PayloadSummary,
    string Status,
    int RetryCount,
    int DurationMs,
    string? ErrorMessage,
    DateTime TimestampUtc);

public sealed record MockExecutionResult<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorMessage,
    int DurationMs,
    int RetryAttempts,
    string CorrelationId);

public interface IIntegrationMockEngine
{
    Task<MockExecutionResult<T>> ExecuteAsync<T>(
        ExternalSystemType systemType,
        string actionName,
        Func<Task<T>> operation,
        string payloadSummary = "{}",
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<MockServerConfigDto> GetConfigurationAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken = default);

    Task<List<MockServerConfigDto>> GetAllConfigurationsAsync(
        CancellationToken cancellationToken = default);

    Task<MockServerConfigDto> UpdateConfigurationAsync(
        UpdateMockServerConfigRequest request,
        CancellationToken cancellationToken = default);

    Task<List<IntegrationMessageLogDto>> GetRecentLogsAsync(
        int count = 50,
        ExternalSystemType? filterSystem = null,
        CancellationToken cancellationToken = default);

    Task ResetCircuitBreakerAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken = default);
}
