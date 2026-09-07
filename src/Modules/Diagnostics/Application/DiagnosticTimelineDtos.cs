namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record TimelineParameterDto(
    string Name,
    string Value,
    string Unit,
    string ReferenceRange,
    string Interpretation,
    bool IsCritical);

public sealed record TimelineEntryDto(
    Guid Id,
    string Category,
    string Title,
    string Status,
    DateTime EventDateUtc,
    string? PerformedBy,
    bool HasCriticalFlag,
    string? SummaryText,
    Guid? ReferenceId,
    List<TimelineParameterDto>? Parameters);

public sealed record PatientDiagnosticTimelineDto(
    Guid PatientId,
    int TotalEntries,
    List<TimelineEntryDto> Entries);

public sealed record PatientPortalResultSummaryDto(
    Guid Id,
    string Category,
    string TestOrStudyName,
    string Status,
    DateTime ResultDateUtc,
    string DoctorOrDepartment,
    bool IsPendingDoctorReview,
    string? FinalReportDiagnosis,
    List<TimelineParameterDto>? Parameters);
