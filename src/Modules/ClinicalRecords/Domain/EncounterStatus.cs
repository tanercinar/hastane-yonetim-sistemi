namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum EncounterStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    EnteredInError = 5,
    Amended = 6,
}
