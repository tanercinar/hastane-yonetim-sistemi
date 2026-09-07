namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum PathologyCaseStatus
{
    Ordered = 1,
    SpecimenReceived = 2,
    GrossExamCompleted = 3,
    MicroscopicExamCompleted = 4,
    ReportDrafted = 5,
    ReportFinalized = 6,
    Corrected = 7,
    Cancelled = 8,
}
