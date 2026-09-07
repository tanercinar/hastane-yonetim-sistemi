namespace HospitalManagement.Modules.Diagnostics.Domain;

public enum BloodUnitStatus
{
    Available = 1,
    Reserved = 2,
    Issued = 3,
    Transfused = 4,
    Discarded = 5,
}

public enum BloodCompatibilityStatus
{
    PendingTesting = 1,
    Compatible = 2,
    Incompatible = 3,
}

public enum CrossmatchStatus
{
    Requested = 1,
    Completed = 2,
    Cancelled = 3,
}
