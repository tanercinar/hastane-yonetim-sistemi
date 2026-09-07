namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record DiagnosisCatalogItemResponse(
    Guid Id,
    string Code,
    string NameTurkish,
    string NameEnglish,
    string Chapter,
    string Block,
    string CatalogVersion);

public sealed record EncounterDiagnosisResponse(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid DiagnosedByPractitionerId,
    string DiagnosisType,
    bool IsCoded,
    string? Icd10Code,
    string DiagnosisTitle,
    string? CatalogVersion,
    string? Notes,
    DateTime DiagnosedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsEnteredInError,
    string? EnteredInErrorReason,
    long Version);
