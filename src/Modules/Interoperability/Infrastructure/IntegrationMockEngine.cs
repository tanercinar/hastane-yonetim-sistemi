using System.Diagnostics;
using HospitalManagement.Modules.Interoperability.Application;
using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Interoperability.Infrastructure;

public sealed class IntegrationMockEngine : IIntegrationMockEngine
{
    private readonly InteroperabilityDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public IntegrationMockEngine(
        InteroperabilityDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<MockExecutionResult<T>> ExecuteAsync<T>(
        ExternalSystemType systemType,
        string actionName,
        Func<Task<T>> operation,
        string payloadSummary = "{}",
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var corrId = SanitizeTechnicalIdentifier(correlationId, $"CORR-{Guid.NewGuid():N}");
        var safeActionName = SanitizeTechnicalIdentifier(actionName, "MockOperation");
        const string safePayloadSummary = "{\"classification\":\"MOCK_TECHNICAL_METADATA\"}";
        var stopwatch = Stopwatch.StartNew();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var config = await GetOrCreateConfigAsync(systemType, cancellationToken);
        var circuit = await GetOrCreateCircuitStateAsync(systemType, cancellationToken);

        if (!config.IsEnabled)
        {
            stopwatch.Stop();
            var log = new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                IntegrationMessageStatus.Failed,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                "Dış mock sistem devre dışı bırakılmıştır.");
            _dbContext.IntegrationMessageLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(false, default, "Dış mock sistem devre dışı bırakılmıştır.", (int)stopwatch.ElapsedMilliseconds, 0, corrId);
        }

        if (!circuit.CanExecute(now))
        {
            stopwatch.Stop();
            var log = new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                IntegrationMessageStatus.DeadLetter,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                "Devre kesici (Circuit Breaker) AÇIK: Dış sistem yanıt vermiyor.");
            _dbContext.IntegrationMessageLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(false, default, "Devre kesici AÇIK: Dış servis geçici olarak kapalı.", (int)stopwatch.ElapsedMilliseconds, 0, corrId);
        }

        if (config.FaultMode == FaultInjectionMode.Offline)
        {
            stopwatch.Stop();
            circuit.RecordFailure();
            var log = new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                IntegrationMessageStatus.Failed,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                "Simüle edilmiş hata: Dış sistem çevrimdışı (Offline).");
            _dbContext.IntegrationMessageLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(false, default, "Dış sistem çevrimdışı (Offline).", (int)stopwatch.ElapsedMilliseconds, 0, corrId);
        }

        if (config.FaultMode == FaultInjectionMode.CircuitBroken)
        {
            stopwatch.Stop();
            circuit.RecordFailure();
            var log = new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                IntegrationMessageStatus.DeadLetter,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                "Simüle edilmiş hata: Devre kesici zorunlu AÇIK konumunda.");
            _dbContext.IntegrationMessageLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(false, default, "Devre kesici zorunlu AÇIK konumunda.", (int)stopwatch.ElapsedMilliseconds, 0, corrId);
        }

        if (config.FaultMode == FaultInjectionMode.CorruptPayload)
        {
            stopwatch.Stop();
            circuit.RecordFailure();
            const string errorCode = "MOCK_INTEGRATION_CORRUPT_PAYLOAD";
            _dbContext.IntegrationMessageLogs.Add(new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                IntegrationMessageStatus.Failed,
                0,
                (int)stopwatch.ElapsedMilliseconds,
                errorCode));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(false, default, errorCode, (int)stopwatch.ElapsedMilliseconds, 0, corrId);
        }

        int retryCount = 0;
        string lastErrorCode = "MOCK_INTEGRATION_OPERATION_FAILED";
        T? successfulResult = default;
        var succeeded = false;

        while (retryCount <= config.MaxRetryAttempts)
        {
            using var attemptTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attemptTimeout.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

            try
            {
                if (config.LatencyMilliseconds > 0)
                {
                    await Task.Delay(config.LatencyMilliseconds, attemptTimeout.Token);
                }

                // Simulate failure rate percentage
                if (config.FailureRatePercentage > 0)
                {
                    var roll = Random.Shared.Next(1, 101);
                    if (roll <= config.FailureRatePercentage)
                    {
                        throw new TimeoutException($"Simüle edilmiş geçici bağlantı hatası (Hata oranı: %{config.FailureRatePercentage})");
                    }
                }

                if (config.FaultMode == FaultInjectionMode.TransientError && retryCount == 0)
                {
                    throw new TimeoutException("Simüle edilmiş ilk çağrı geçici arızası.");
                }

                // Execute actual mock operation
                successfulResult = await operation().WaitAsync(attemptTimeout.Token);
                succeeded = true;
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                lastErrorCode = "MOCK_INTEGRATION_TIMEOUT";
                retryCount++;

                if (retryCount <= config.MaxRetryAttempts)
                {
                    await Task.Delay(50 * retryCount, cancellationToken);
                }
            }
            catch (TimeoutException)
            {
                lastErrorCode = "MOCK_INTEGRATION_TRANSIENT_FAILURE";
                retryCount++;

                if (retryCount <= config.MaxRetryAttempts)
                {
                    // Brief backoff before retry
                    await Task.Delay(50 * retryCount, cancellationToken);
                }
            }
            catch (Exception)
            {
                lastErrorCode = "MOCK_INTEGRATION_OPERATION_FAILED";
                retryCount++;
                break;
            }
        }

        if (succeeded)
        {
            stopwatch.Stop();
            circuit.RecordSuccess();

            var status = retryCount > 0 ? IntegrationMessageStatus.Retried : IntegrationMessageStatus.Success;
            _dbContext.IntegrationMessageLogs.Add(new IntegrationMessageLog(
                corrId,
                systemType,
                IntegrationMessageDirection.Outbound,
                safeActionName,
                safePayloadSummary,
                status,
                retryCount,
                (int)stopwatch.ElapsedMilliseconds));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new MockExecutionResult<T>(
                true,
                successfulResult,
                null,
                (int)stopwatch.ElapsedMilliseconds,
                retryCount,
                corrId);
        }

        stopwatch.Stop();
        circuit.RecordFailure();

        var failureLog = new IntegrationMessageLog(
            corrId,
            systemType,
            IntegrationMessageDirection.Outbound,
            safeActionName,
            safePayloadSummary,
            IntegrationMessageStatus.DeadLetter,
            retryCount - 1,
            (int)stopwatch.ElapsedMilliseconds,
            lastErrorCode);
        _dbContext.IntegrationMessageLogs.Add(failureLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MockExecutionResult<T>(
            false,
            default,
            lastErrorCode,
            (int)stopwatch.ElapsedMilliseconds,
            retryCount - 1,
            corrId);
    }

    private static string SanitizeTechnicalIdentifier(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 80 && trimmed.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            ? trimmed
            : fallback;
    }

    public async Task<MockServerConfigDto> GetConfigurationAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken = default)
    {
        var config = await GetOrCreateConfigAsync(systemType, cancellationToken);
        return MapToDto(config);
    }

    public async Task<List<MockServerConfigDto>> GetAllConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        // Ensure all system types have a config row
        foreach (var type in Enum.GetValues<ExternalSystemType>())
        {
            await GetOrCreateConfigAsync(type, cancellationToken);
        }

        var list = await _dbContext.MockServerConfigurations
            .AsNoTracking()
            .OrderBy(c => c.SystemType)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<MockServerConfigDto> UpdateConfigurationAsync(
        UpdateMockServerConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<ExternalSystemType>(request.SystemType, true, out var systemType))
        {
            throw new ArgumentException($"Geçersiz dış sistem tipi: {request.SystemType}", nameof(request));
        }

        if (!Enum.TryParse<FaultInjectionMode>(request.FaultMode, true, out var faultMode))
        {
            faultMode = FaultInjectionMode.None;
        }

        var config = await GetOrCreateConfigAsync(systemType, cancellationToken);
        config.UpdateSettings(
            request.IsEnabled,
            faultMode,
            request.LatencyMilliseconds,
            request.FailureRatePercentage,
            request.MaxRetryAttempts,
            request.TimeoutSeconds);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(config);
    }

    public async Task<List<IntegrationMessageLogDto>> GetRecentLogsAsync(
        int count = 50,
        ExternalSystemType? filterSystem = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IntegrationMessageLogs.AsNoTracking();

        if (filterSystem.HasValue)
        {
            query = query.Where(l => l.SystemType == filterSystem.Value);
        }

        var logs = await query
            .OrderByDescending(l => l.TimestampUtc)
            .Take(Math.Clamp(count, 1, 200))
            .ToListAsync(cancellationToken);

        return logs.Select(l => new IntegrationMessageLogDto(
            l.Id,
            l.CorrelationId,
            l.SystemType.ToString(),
            l.Direction.ToString(),
            l.ActionName,
            l.PayloadSummary,
            l.Status.ToString(),
            l.RetryCount,
            l.DurationMs,
            l.ErrorMessage,
            l.TimestampUtc)).ToList();
    }

    public async Task ResetCircuitBreakerAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken = default)
    {
        var circuit = await GetOrCreateCircuitStateAsync(systemType, cancellationToken);
        circuit.Reset();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<MockServerConfiguration> GetOrCreateConfigAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken)
    {
        var config = await _dbContext.MockServerConfigurations
            .FirstOrDefaultAsync(c => c.SystemType == systemType, cancellationToken);

        if (config is null)
        {
            config = new MockServerConfiguration(systemType);
            _dbContext.MockServerConfigurations.Add(config);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return config;
    }

    private async Task<IntegrationCircuitState> GetOrCreateCircuitStateAsync(
        ExternalSystemType systemType,
        CancellationToken cancellationToken)
    {
        var circuit = await _dbContext.IntegrationCircuitStates
            .FirstOrDefaultAsync(s => s.SystemType == systemType, cancellationToken);

        if (circuit is null)
        {
            circuit = new IntegrationCircuitState(systemType);
            _dbContext.IntegrationCircuitStates.Add(circuit);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return circuit;
    }

    private static MockServerConfigDto MapToDto(MockServerConfiguration c) =>
        new(
            c.Id,
            c.SystemType.ToString(),
            c.IsEnabled,
            c.FaultMode.ToString(),
            c.LatencyMilliseconds,
            c.FailureRatePercentage,
            c.MaxRetryAttempts,
            c.TimeoutSeconds,
            c.UpdatedAtUtc);
}
