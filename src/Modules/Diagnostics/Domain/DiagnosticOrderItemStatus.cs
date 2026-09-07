namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum DiagnosticOrderItemStatus
{
    Pending = 1,
    SampleCollected = 2,
    SampleReceived = 3,
    InAnalysis = 4,
    Reported = 5,
    Cancelled = 6,
}
