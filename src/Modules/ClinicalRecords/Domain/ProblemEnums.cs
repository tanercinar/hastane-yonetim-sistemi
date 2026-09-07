namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum ProblemCategory
{
    ActiveProblem = 1,
    ChronicCondition = 2,
    PastMedicalHistory = 3,
    SurgicalHistory = 4,
    FamilyHistory = 5,
}

public enum ProblemClinicalStatus
{
    Active = 1,
    Inactive = 2,
    Resolved = 3,
    InRemission = 4,
}

public enum ProblemVerificationStatus
{
    Provisional = 1,
    Differential = 2,
    Confirmed = 3,
    Refuted = 4,
    EnteredInError = 5,
}
