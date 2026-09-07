using HospitalManagement.Modules.Pharmacy.Domain;

namespace HospitalManagement.Modules.Pharmacy.Application;

public sealed record PrescriptionItemSafetyCandidate(
    Guid MedicationCatalogItemId,
    decimal Dose = 1,
    string DoseUnit = "tablet",
    string Frequency = "1x1",
    int DurationDays = 7);

public sealed record MedicationSafetyWarning(
    string WarningCode,
    SafetyWarningType Type,
    SafetyWarningSeverity Severity,
    string Title,
    string Message,
    string? OffendingMedicationName,
    string? ConflictingItemName,
    bool RequiresOverrideReason);

public sealed record MedicationSafetyCheckResult(
    bool HasWarnings,
    bool HasCriticalWarnings,
    bool HasModerateWarnings,
    IReadOnlyList<MedicationSafetyWarning> Warnings,
    string Disclaimer);
