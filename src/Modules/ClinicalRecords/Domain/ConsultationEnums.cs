namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum ConsultationUrgency
{
    Routine = 1,
    Urgent = 2,
    Stat = 3,
}

public enum ConsultationStatus
{
    Requested = 1,
    Accepted = 2,
    InProgress = 3,
    Completed = 4,
    Declined = 5,
    Cancelled = 6,
    EnteredInError = 7,
}
