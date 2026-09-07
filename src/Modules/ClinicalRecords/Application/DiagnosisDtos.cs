using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record DiagnosisCatalogItemDto(
    Guid Id,
    string Code,
    string NameTurkish,
    string NameEnglish,
    string Chapter,
    string Block,
    string CatalogVersion);

public sealed record EncounterDiagnosisDto(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid DiagnosedByPractitionerId,
    DiagnosisType DiagnosisType,
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
