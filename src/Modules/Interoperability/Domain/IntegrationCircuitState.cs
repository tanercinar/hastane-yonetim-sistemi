namespace HospitalManagement.Modules.Interoperability.Domain;

public sealed class IntegrationCircuitState
{
    private IntegrationCircuitState()
    {
    }

    public IntegrationCircuitState(
        ExternalSystemType systemType,
        CircuitBreakerState state = CircuitBreakerState.Closed,
        int failureThreshold = 5,
        int recoveryTimeoutSeconds = 30)
    {
        Id = Guid.NewGuid();
        SystemType = systemType;
        State = state;
        FailureThreshold = Math.Max(1, failureThreshold);
        RecoveryTimeoutSeconds = Math.Max(5, recoveryTimeoutSeconds);
        ConsecutiveFailures = 0;
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public Guid Id
    {
        get; private set;
    }
    public ExternalSystemType SystemType
    {
        get; private set;
    }
    public CircuitBreakerState State
    {
        get; private set;
    }
    public int ConsecutiveFailures
    {
        get; private set;
    }
    public int FailureThreshold
    {
        get; private set;
    }
    public int RecoveryTimeoutSeconds
    {
        get; private set;
    }
    public DateTime? LastFailureTimeUtc
    {
        get; private set;
    }
    public DateTime? NextAttemptAllowedUtc
    {
        get; private set;
    }
    public DateTime LastUpdatedUtc
    {
        get; private set;
    }

    public void RecordSuccess()
    {
        ConsecutiveFailures = 0;
        State = CircuitBreakerState.Closed;
        NextAttemptAllowedUtc = null;
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public void RecordFailure()
    {
        ConsecutiveFailures++;
        LastFailureTimeUtc = DateTime.UtcNow;

        if (ConsecutiveFailures >= FailureThreshold)
        {
            State = CircuitBreakerState.Open;
            NextAttemptAllowedUtc = DateTime.UtcNow.AddSeconds(RecoveryTimeoutSeconds);
        }

        LastUpdatedUtc = DateTime.UtcNow;
    }

    public bool CanExecute(DateTime nowUtc)
    {
        if (State == CircuitBreakerState.Closed)
        {
            return true;
        }

        if (State == CircuitBreakerState.Open)
        {
            if (NextAttemptAllowedUtc.HasValue && nowUtc >= NextAttemptAllowedUtc.Value)
            {
                State = CircuitBreakerState.HalfOpen;
                LastUpdatedUtc = nowUtc;
                return true;
            }

            return false;
        }

        // HalfOpen allows trial attempt
        return true;
    }

    public void Reset()
    {
        ConsecutiveFailures = 0;
        State = CircuitBreakerState.Closed;
        LastFailureTimeUtc = null;
        NextAttemptAllowedUtc = null;
        LastUpdatedUtc = DateTime.UtcNow;
    }
}
