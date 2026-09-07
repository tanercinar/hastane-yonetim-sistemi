namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record ClinicalProblemResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProblemTitle,
    string? Code,
    string Category,
    string ClinicalStatus,
    string VerificationStatus,
    DateOnly? OnsetDate,
    DateOnly? ResolvedDate,
    string? Notes,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    string? EnteredInErrorReason,
    long Version);
