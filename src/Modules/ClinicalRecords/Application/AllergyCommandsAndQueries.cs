using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record CreateAllergyCommand(
    Guid PatientId,
    Guid? EncounterId,
    string Substance,
    AllergyCategory Category,
    AllergyCriticality Criticality,
    string? Manifestation,
    DateTime? OnsetDateTimeUtc,
    string? Notes);

public sealed record UpdateAllergyStatusCommand(
    Guid AllergyId,
    long ExpectedVersion,
    AllergyClinicalStatus ClinicalStatus,
    string? Notes);

public sealed record MarkAllergyEnteredInErrorCommand(
    Guid AllergyId,
    long ExpectedVersion,
    string Reason);

public sealed record CreateClinicalProblemCommand(
    Guid PatientId,
    Guid? EncounterId,
    string ProblemTitle,
    string? Code,
    ProblemCategory Category,
    DateOnly? OnsetDate,
    string? Notes);

public sealed record UpdateClinicalProblemStatusCommand(
    Guid ProblemId,
    long ExpectedVersion,
    ProblemClinicalStatus ClinicalStatus,
    DateOnly? ResolvedDate,
    string? Notes);

public sealed record MarkClinicalProblemEnteredInErrorCommand(
    Guid ProblemId,
    long ExpectedVersion,
    string Reason);
