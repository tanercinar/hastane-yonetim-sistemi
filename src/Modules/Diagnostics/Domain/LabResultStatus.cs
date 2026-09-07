namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum LabResultStatus
{
    Draft = 1,
    TechnicallyApproved = 2,
    FinalApproved = 3,
    Corrected = 4,
    Cancelled = 5,
    EnteredInError = 6,
}
