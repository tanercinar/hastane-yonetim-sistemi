namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum DiagnosticOrderStatus
{
    Draft = 1,
    Placed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    EnteredInError = 6,
}
