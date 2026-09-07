namespace HospitalManagement.Contracts.Diagnostics;

public sealed record AcknowledgeCriticalResultRequest
{
    public required string AcknowledgmentNotes
    {
        get; init;
    }
}

public sealed record EscalateCriticalResultRequest
{
    public required string EscalationReason
    {
        get; init;
    }
}

public sealed record CriticalResultNotificationResponse(
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
    string Flag,
    string Status,
    int EscalationLevel,
    Guid? ResponsibleDoctorUserId,
    Guid? AcknowledgedByUserId,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgmentNotes,
    DateTime? EscalatedAtUtc,
    string? EscalationReason,
    DateTime CreatedAtUtc);
