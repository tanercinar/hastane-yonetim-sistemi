using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record AllergyDto(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string Substance,
    AllergyCategory Category,
    AllergyCriticality Criticality,
    AllergyClinicalStatus ClinicalStatus,
    AllergyVerificationStatus VerificationStatus,
    string? Manifestation,
    DateTime? OnsetDateTimeUtc,
    string? Notes,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    string? EnteredInErrorReason,
    long Version);

public sealed record ClinicalProblemDto(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProblemTitle,
    string? Code,
    ProblemCategory Category,
    ProblemClinicalStatus ClinicalStatus,
    ProblemVerificationStatus VerificationStatus,
    DateOnly? OnsetDate,
    DateOnly? ResolvedDate,
    string? Notes,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    string? EnteredInErrorReason,
    long Version);
