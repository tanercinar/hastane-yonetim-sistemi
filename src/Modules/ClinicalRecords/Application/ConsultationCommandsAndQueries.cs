using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record RequestConsultationCommand(
    Guid EncounterId,
    Guid PatientId,
    Guid TargetDepartmentId,
    Guid? TargetPractitionerId,
    ConsultationUrgency Urgency,
    string ReasonForConsultation,
    string ClinicalQuestion);

public sealed record AcceptConsultationCommand(
    Guid ConsultationId,
    long ExpectedVersion,
    string? Notes);

public sealed record CompleteConsultationCommand(
    Guid ConsultationId,
    long ExpectedVersion,
    string ConsultationReport,
    string? Recommendation);

public sealed record DeclineConsultationCommand(
    Guid ConsultationId,
    long ExpectedVersion,
    string Reason);

public sealed record CancelConsultationCommand(
    Guid ConsultationId,
    long ExpectedVersion,
    string Reason);

public sealed record MarkConsultationEnteredInErrorCommand(
    Guid ConsultationId,
    long ExpectedVersion,
    string Reason);
