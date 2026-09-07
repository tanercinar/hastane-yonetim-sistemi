namespace HospitalManagement.Contracts.Diagnostics;

public sealed record RadiologyCatalogItemResponse(
    Guid Id,
    string Code,
    string Name,
    string Modality,
    string BodySite,
    string? Description,
    string? PreparationInstructions,
    bool ContrastRequired,
    int EstimatedDurationMinutes,
    bool IsActive);

public sealed record ScheduleRadiologyStudyRequest
{
    public required DateTime ScheduledAtUtc
    {
        get; init;
    }
}

public sealed record CompleteAcquisitionRequest
{
    public string? TechnicianNotes
    {
        get; init;
    }
}

public sealed record DraftRadiologyReportRequest
{
    public required string ReportText
    {
        get; init;
    }
    public string? Impression
    {
        get; init;
    }
}

public sealed record FinalizeRadiologyReportRequest
{
    public required string ReportText
    {
        get; init;
    }
    public required string Impression
    {
        get; init;
    }
}

public sealed record AddRadiologyAddendumRequest
{
    public required string AddendumText
    {
        get; init;
    }
}

public sealed record CancelRadiologyStudyRequest
{
    public required string Reason
    {
        get; init;
    }
}

public sealed record RadiologyStudyDetailResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string AccessionNumber,
    string Modality,
    string ProcedureCode,
    string ProcedureName,
    string BodySite,
    string Status,
    DateTime? ScheduledAtUtc,
    DateTime? PerformedAtUtc,
    Guid? TechnicianUserId,
    string? TechnicianNotes,
    Guid? RadiologistUserId,
    string? ReportText,
    string? Impression,
    DateTime? ReportDraftedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    string? AddendumText,
    DateTime? AddendumAddedAtUtc,
    Guid? AddendumByUserId,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record RadiologyStudySummaryResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string AccessionNumber,
    string Modality,
    string ProcedureCode,
    string ProcedureName,
    string BodySite,
    string Status,
    DateTime? ScheduledAtUtc,
    DateTime? PerformedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    DateTime CreatedAtUtc);
