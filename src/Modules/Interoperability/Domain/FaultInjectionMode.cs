namespace HospitalManagement.Modules.Interoperability.Domain;

public enum FaultInjectionMode
{
    None = 0,
    Latency = 1,
    TransientError = 2,
    CorruptPayload = 3,
    CircuitBroken = 4,
    Offline = 5,
}
