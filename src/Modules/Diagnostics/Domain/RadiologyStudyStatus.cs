namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum RadiologyStudyStatus
{
    Ordered = 1,
    Scheduled = 2,
    InProgress = 3,
    Acquired = 4,
    ReportDrafted = 5,
    ReportFinalized = 6,
    AddendumAdded = 7,
    Cancelled = 8,
}
