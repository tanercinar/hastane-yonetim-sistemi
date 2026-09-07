using HospitalManagement.Modules.Emergency.Domain;

namespace HospitalManagement.Modules.Emergency.Application;

public sealed record CreateEmergencyOrderDto(
    Guid AdmissionId,
    EmergencyOrderType OrderType,
    string OrderCatalogCode,
    string OrderCatalogName,
    EmergencyOrderPriority Priority,
    string? ClinicalInstructions);

public sealed record EmergencyOrderDto(
    Guid Id,
    Guid AdmissionId,
    EmergencyOrderType OrderType,
    string OrderCatalogCode,
    string OrderCatalogName,
    EmergencyOrderPriority Priority,
    Guid OrderedByDoctorId,
    DateTime OrderedAtUtc,
    EmergencyOrderStatus Status,
    string? ClinicalInstructions,
    string? ResultSummary,
    DateTime? CompletedAtUtc,
    string? CancellationReason,
    uint Version);

public sealed record RequestEmergencyConsultationDto(
    Guid AdmissionId,
    Guid DepartmentId,
    string DepartmentName,
    EmergencyConsultationUrgency Urgency,
    string ClinicalReason);

public sealed record EmergencyConsultationDto(
    Guid Id,
    Guid AdmissionId,
    Guid DepartmentId,
    string DepartmentName,
    Guid RequestedByDoctorId,
    DateTime RequestedAtUtc,
    EmergencyConsultationUrgency Urgency,
    string ClinicalReason,
    EmergencyConsultationStatus Status,
    Guid? ConsultantDoctorId,
    string? ConsultationResponseNotes,
    DateTime? RespondedAtUtc,
    string? CancellationReason,
    uint Version);

public sealed record RecordEmergencyDispositionDto(
    EmergencyDispositionType DispositionType,
    Guid? TargetWardOrIcuId,
    string? TargetDepartmentName,
    string DispositionSummaryNotes,
    string? FollowUpInstructions);
