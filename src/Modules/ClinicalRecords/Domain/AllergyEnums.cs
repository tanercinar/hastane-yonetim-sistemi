namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum AllergyCategory
{
    Food = 1,
    Medication = 2,
    Environment = 3,
    Biologic = 4,
    Other = 5,
}

public enum AllergyCriticality
{
    Low = 1,
    High = 2,
    UnableToAssess = 3,
}

public enum AllergyClinicalStatus
{
    Active = 1,
    Inactive = 2,
    Resolved = 3,
}

public enum AllergyVerificationStatus
{
    Suspected = 1,
    Confirmed = 2,
    Refuted = 3,
    EnteredInError = 4,
}
