namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record AllergyResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string Substance,
    string Category,
    string Criticality,
    string ClinicalStatus,
    string VerificationStatus,
    string? Manifestation,
    DateTime? OnsetDateTimeUtc,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    string? EnteredInErrorReason,
    long Version);
