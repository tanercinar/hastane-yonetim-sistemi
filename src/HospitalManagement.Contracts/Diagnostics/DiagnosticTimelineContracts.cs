namespace HospitalManagement.Contracts.Diagnostics;

public sealed record TimelineEntryResponse(
    Guid Id,
    string Category, // "Laboratory", "Radiology", "Pathology", "BloodBank"
    string Title,
    string Status,
    DateTime EventDateUtc,
    string? PerformedBy,
    bool HasCriticalFlag,
    string? SummaryText,
    Guid? ReferenceId,
    List<TimelineParameterResponse>? Parameters);

public sealed record TimelineParameterResponse(
    string Name,
    string Value,
    string Unit,
    string ReferenceRange,
    string Interpretation, // Normal, High, Low, CriticalHigh, CriticalLow
    bool IsCritical);

public sealed record PatientDiagnosticTimelineResponse(
    Guid PatientId,
    int TotalEntries,
    List<TimelineEntryResponse> Entries);

public sealed record PatientPortalResultSummaryResponse(
    Guid Id,
    string Category, // "Laboratory", "Radiology", "Pathology", "BloodBank"
    string TestOrStudyName,
    string Status,
    DateTime ResultDateUtc,
    string DoctorOrDepartment,
    bool IsPendingDoctorReview,
    string? FinalReportDiagnosis,
    List<TimelineParameterResponse>? Parameters);
