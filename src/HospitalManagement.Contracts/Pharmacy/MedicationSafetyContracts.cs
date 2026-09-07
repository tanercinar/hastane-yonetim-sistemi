namespace HospitalManagement.Contracts.Pharmacy;

public sealed record MedicationSafetyCheckRequest
{
    public required Guid PatientId
    {
        get; init;
    }
    public Guid? EncounterId
    {
        get; init;
    }
    public required IReadOnlyList<MedicationSafetyItemRequest> Items
    {
        get; init;
    }
}

public sealed record MedicationSafetyItemRequest
{
    public required Guid MedicationCatalogItemId
    {
        get; init;
    }
    public decimal Dose { get; init; } = 1;
    public string DoseUnit { get; init; } = "tablet";
    public string Frequency { get; init; } = "1x1";
    public int DurationDays { get; init; } = 7;
}

public sealed record MedicationSafetyCheckResponse(
    bool HasWarnings,
    bool HasCriticalWarnings,
    IReadOnlyList<MedicationSafetyWarningResponse> Warnings,
    string Disclaimer);

public sealed record MedicationSafetyWarningResponse(
    string WarningCode,
    string WarningType,
    string Severity,
    string Title,
    string Message,
    string? OffendingMedicationName,
    string? ConflictingItemName,
    bool RequiresOverrideReason);
