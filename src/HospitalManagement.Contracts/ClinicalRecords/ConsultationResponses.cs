namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record ConsultationResponse(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid RequestingPractitionerId,
    Guid TargetDepartmentId,
    Guid? TargetPractitionerId,
    Guid? AssignedPractitionerId,
    string Urgency,
    string Status,
    string ReasonForConsultation,
    string ClinicalQuestion,
    string? ConsultationReport,
    string? Recommendation,
    string? DeclineReason,
    string? CancellationReason,
    string? EnteredInErrorReason,
    DateTime RequestedAtUtc,
    DateTime? AcceptedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? UpdatedAtUtc,
    long Version);
