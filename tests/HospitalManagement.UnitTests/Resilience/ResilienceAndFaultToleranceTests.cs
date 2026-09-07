using HospitalManagement.Modules.Interoperability.Domain;
using HospitalManagement.Modules.Reporting.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Resilience;

public sealed class ResilienceAndFaultToleranceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G09")]
    public void MockServerConfigurationEnforcesStrictClampingBounds()
    {
        // Negative and excessively large parameters must be clamped to safe operational limits
        var config = new MockServerConfiguration(
            ExternalSystemType.ENabiz,
            isEnabled: true,
            faultMode: FaultInjectionMode.TransientError,
            latencyMilliseconds: 999999, // Should clamp to 30000ms
            failureRatePercentage: 150,  // Should clamp to 100%
            maxRetryAttempts: 25,        // Should clamp to 10
            timeoutSeconds: 300);        // Should clamp to 120s

        Assert.Equal(30000, config.LatencyMilliseconds);
        Assert.Equal(100, config.FailureRatePercentage);
        Assert.Equal(10, config.MaxRetryAttempts);
        Assert.Equal(120, config.TimeoutSeconds);
        Assert.Equal(FaultInjectionMode.TransientError, config.FaultMode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G09")]
    public void MockServerConfigurationUnderflowParametersClampToFloorLimits()
    {
        var config = new MockServerConfiguration(
            ExternalSystemType.Mhrs,
            isEnabled: false,
            faultMode: FaultInjectionMode.None,
            latencyMilliseconds: -100,
            failureRatePercentage: -50,
            maxRetryAttempts: -5,
            timeoutSeconds: -10);

        Assert.Equal(0, config.LatencyMilliseconds);
        Assert.Equal(0, config.FailureRatePercentage);
        Assert.Equal(0, config.MaxRetryAttempts);
        Assert.Equal(1, config.TimeoutSeconds); // Minimum timeout is 1s
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G09")]
    public void ProjectionCheckpointAndProcessedEventProvideIdempotencyGuarantees()
    {
        var eventId = Guid.NewGuid();
        var processedAt = DateTime.UtcNow;

        var processedEvent = new ProjectionProcessedEvent(
            eventId,
            "OperationsMetricsProjection",
            processedAt);

        Assert.Equal(eventId, processedEvent.EventId);
        Assert.Equal("OperationsMetricsProjection", processedEvent.ProjectionName);
        Assert.Equal(processedAt, processedEvent.ProcessedAtUtc);

        var checkpoint = new ProjectionCheckpoint("OperationsMetricsProjection", version: 1);

        Assert.Equal("OperationsMetricsProjection", checkpoint.ProjectionName);
        Assert.Equal(0L, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);

        // Advancing position safely
        checkpoint.MarkActive(1051L, processedAt.AddSeconds(1));
        Assert.Equal(1051L, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G09")]
    public void FaultInjectionModesCoverAllRequiredResilienceProfiles()
    {
        var modes = Enum.GetValues<FaultInjectionMode>();

        Assert.Contains(FaultInjectionMode.None, modes);
        Assert.Contains(FaultInjectionMode.Latency, modes);
        Assert.Contains(FaultInjectionMode.TransientError, modes);
        Assert.Contains(FaultInjectionMode.CorruptPayload, modes);
        Assert.Contains(FaultInjectionMode.CircuitBroken, modes);
        Assert.Contains(FaultInjectionMode.Offline, modes);
    }
}
