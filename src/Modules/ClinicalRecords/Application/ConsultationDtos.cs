using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record ConsultationDto(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid RequestingPractitionerId,
    Guid TargetDepartmentId,
    Guid? TargetPractitionerId,
    Guid? AssignedPractitionerId,
    ConsultationUrgency Urgency,
    ConsultationStatus Status,
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
