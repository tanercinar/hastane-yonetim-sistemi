using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record CriticalResultNotificationDto(
    Guid Id,
    Guid LabResultId,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string ParameterCode,
    string ParameterName,
    decimal? NumericValue,
    string? StringValue,
    string? Unit,
    LabResultInterpretation Flag,
    CriticalNotificationStatus Status,
    int EscalationLevel,
    Guid? ResponsibleDoctorUserId,
    Guid? AcknowledgedByUserId,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgmentNotes,
    DateTime? EscalatedAtUtc,
    string? EscalationReason,
    DateTime CreatedAtUtc);
