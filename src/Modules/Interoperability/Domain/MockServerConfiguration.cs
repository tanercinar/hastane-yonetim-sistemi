namespace HospitalManagement.Modules.Interoperability.Domain;

public sealed class MockServerConfiguration
{
    private MockServerConfiguration()
    {
    }

    public MockServerConfiguration(
        ExternalSystemType systemType,
        bool isEnabled = true,
        FaultInjectionMode faultMode = FaultInjectionMode.None,
        int latencyMilliseconds = 50,
        int failureRatePercentage = 0,
        int maxRetryAttempts = 3,
        int timeoutSeconds = 10)
    {
        Id = Guid.NewGuid();
        SystemType = systemType;
        IsEnabled = isEnabled;
        FaultMode = faultMode;
        LatencyMilliseconds = Math.Clamp(latencyMilliseconds, 0, 30000);
        FailureRatePercentage = Math.Clamp(failureRatePercentage, 0, 100);
        MaxRetryAttempts = Math.Clamp(maxRetryAttempts, 0, 10);
        TimeoutSeconds = Math.Clamp(timeoutSeconds, 1, 120);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id
    {
        get; private set;
    }
    public ExternalSystemType SystemType
    {
        get; private set;
    }
    public bool IsEnabled
    {
        get; private set;
    }
    public FaultInjectionMode FaultMode
    {
        get; private set;
    }
    public int LatencyMilliseconds
    {
        get; private set;
    }
    public int FailureRatePercentage
    {
        get; private set;
    }
    public int MaxRetryAttempts
    {
        get; private set;
    }
    public int TimeoutSeconds
    {
        get; private set;
    }
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    public void UpdateSettings(
        bool isEnabled,
        FaultInjectionMode faultMode,
        int latencyMilliseconds,
        int failureRatePercentage,
        int maxRetryAttempts,
        int timeoutSeconds)
    {
        IsEnabled = isEnabled;
        FaultMode = faultMode;
        LatencyMilliseconds = Math.Clamp(latencyMilliseconds, 0, 30000);
        FailureRatePercentage = Math.Clamp(failureRatePercentage, 0, 100);
        MaxRetryAttempts = Math.Clamp(maxRetryAttempts, 0, 10);
        TimeoutSeconds = Math.Clamp(timeoutSeconds, 1, 120);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
