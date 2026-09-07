using HospitalManagement.Modules.Interoperability.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class MockEngineDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G01")]
    public void MockServerConfigurationInitializesAndClampsValues()
    {
        var config = new MockServerConfiguration(
            ExternalSystemType.Fhir,
            isEnabled: true,
            faultMode: FaultInjectionMode.Latency,
            latencyMilliseconds: 40000, // Should clamp to 30000
            failureRatePercentage: 150, // Should clamp to 100
            maxRetryAttempts: 25,       // Should clamp to 10
            timeoutSeconds: 200);       // Should clamp to 120

        Assert.Equal(ExternalSystemType.Fhir, config.SystemType);
        Assert.True(config.IsEnabled);
        Assert.Equal(FaultInjectionMode.Latency, config.FaultMode);
        Assert.Equal(30000, config.LatencyMilliseconds);
        Assert.Equal(100, config.FailureRatePercentage);
        Assert.Equal(10, config.MaxRetryAttempts);
        Assert.Equal(120, config.TimeoutSeconds);

        config.UpdateSettings(false, FaultInjectionMode.None, -50, -10, -5, 0);
        Assert.False(config.IsEnabled);
        Assert.Equal(FaultInjectionMode.None, config.FaultMode);
        Assert.Equal(0, config.LatencyMilliseconds);
        Assert.Equal(0, config.FailureRatePercentage);
        Assert.Equal(0, config.MaxRetryAttempts);
        Assert.Equal(1, config.TimeoutSeconds);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G01")]
    public void IntegrationCircuitStateTransitionsCorrectly()
    {
        var circuit = new IntegrationCircuitState(
            ExternalSystemType.Mhrs,
            failureThreshold: 3,
            recoveryTimeoutSeconds: 10);

        var now = DateTime.UtcNow;

        Assert.Equal(CircuitBreakerState.Closed, circuit.State);
        Assert.True(circuit.CanExecute(now));

        // 1st failure
        circuit.RecordFailure();
        Assert.Equal(CircuitBreakerState.Closed, circuit.State);
        Assert.Equal(1, circuit.ConsecutiveFailures);

        // 2nd failure
        circuit.RecordFailure();
        Assert.Equal(CircuitBreakerState.Closed, circuit.State);

        // 3rd failure -> Opens circuit
        circuit.RecordFailure();
        Assert.Equal(CircuitBreakerState.Open, circuit.State);
        Assert.Equal(3, circuit.ConsecutiveFailures);
        Assert.NotNull(circuit.NextAttemptAllowedUtc);

        // Cannot execute before timeout
        Assert.False(circuit.CanExecute(now.AddSeconds(5)));

        // Can execute after timeout -> transitions to HalfOpen
        Assert.True(circuit.CanExecute(now.AddSeconds(15)));
        Assert.Equal(CircuitBreakerState.HalfOpen, circuit.State);

        // Success resets to Closed
        circuit.RecordSuccess();
        Assert.Equal(CircuitBreakerState.Closed, circuit.State);
        Assert.Equal(0, circuit.ConsecutiveFailures);
        Assert.Null(circuit.NextAttemptAllowedUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G01")]
    public void IntegrationMessageLogStoresFieldsAndGeneratesCorrelationIdIfMissing()
    {
        var log = new IntegrationMessageLog(
            correlationId: "",
            systemType: ExternalSystemType.ENabiz,
            direction: IntegrationMessageDirection.Outbound,
            actionName: "SendSysPackage101",
            payloadSummary: "{\"packageCode\": 101}",
            status: IntegrationMessageStatus.Success,
            retryCount: 1,
            durationMs: 120);

        Assert.StartsWith("CORR-", log.CorrelationId, StringComparison.Ordinal);
        Assert.Equal(ExternalSystemType.ENabiz, log.SystemType);
        Assert.Equal(IntegrationMessageDirection.Outbound, log.Direction);
        Assert.Equal("SendSysPackage101", log.ActionName);
        Assert.Equal("{\"packageCode\": 101}", log.PayloadSummary);
        Assert.Equal(IntegrationMessageStatus.Success, log.Status);
        Assert.Equal(1, log.RetryCount);
        Assert.Equal(120, log.DurationMs);
    }
}
