namespace HospitalManagement.Modules.Pharmacy.Domain;

public enum SafetyWarningType
{
    AllergyCrossReaction = 1,
    DuplicateTherapy = 2,
    DrugInteraction = 3,
    DoseWarning = 4,
}

public enum SafetyWarningSeverity
{
    Low = 1,
    Moderate = 2,
    Critical = 3,
}
