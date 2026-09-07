namespace HospitalManagement.Contracts.Diagnostics;

public sealed record ReceivePathologySpecimenRequest
{
    public required string FixativeUsed
    {
        get; init;
    }
}

public sealed record RecordGrossExamRequest
{
    public required string GrossDescription
    {
        get; init;
    }
}

public sealed record RecordMicroscopicExamRequest
{
    public required string MicroscopicDescription
    {
        get; init;
    }
}

public sealed record DraftPathologyReportRequest
{
    public required string PathologicalDiagnosis
    {
        get; init;
    }
}

public sealed record FinalizePathologyReportRequest
{
    public required string PathologicalDiagnosis
    {
        get; init;
    }
}

public sealed record CorrectPathologyReportRequest
{
    public required string CorrectionReason
    {
        get; init;
    }
    public required string NewDiagnosis
    {
        get; init;
    }
}

public sealed record CancelPathologyCaseRequest
{
    public required string Reason
    {
        get; init;
    }
}

public sealed record PathologyCaseDetailResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string PathologyNumber,
    string SpecimenType,
    string AnatomicSite,
    string? ClinicalHistoryAndDiagnosis,
    string? FixativeUsed,
    string Status,
    DateTime? ReceivedAtUtc,
    Guid? ReceivedByUserId,
    string? GrossDescription,
    DateTime? GrossExamAtUtc,
    Guid? GrossExamByUserId,
    string? MicroscopicDescription,
    DateTime? MicroscopicExamAtUtc,
    Guid? MicroscopicExamByUserId,
    string? PathologicalDiagnosis,
    DateTime? ReportDraftedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    Guid? PathologistUserId,
    string? CorrectionReason,
    Guid? PreviousCaseId,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version);

public sealed record PathologyCaseSummaryResponse(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string PathologyNumber,
    string SpecimenType,
    string AnatomicSite,
    string Status,
    DateTime? ReceivedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    DateTime CreatedAtUtc);
