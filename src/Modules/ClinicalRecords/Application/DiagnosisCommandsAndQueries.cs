using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record RecordDiagnosisCommand(
    Guid EncounterId,
    Guid PatientId,
    DiagnosisType DiagnosisType,
    bool IsCoded,
    string? Icd10Code,
    string DiagnosisTitle,
    string? Notes);

public sealed record UpdateDiagnosisCommand(
    Guid DiagnosisId,
    long ExpectedVersion,
    DiagnosisType DiagnosisType,
    bool IsCoded,
    string? Icd10Code,
    string DiagnosisTitle,
    string? Notes);

public sealed record MarkDiagnosisEnteredInErrorCommand(
    Guid DiagnosisId,
    long ExpectedVersion,
    string Reason);
