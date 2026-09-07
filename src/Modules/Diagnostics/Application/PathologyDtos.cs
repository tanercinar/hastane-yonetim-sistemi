using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record PathologyCaseDetailDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string PathologyNumber,
    PathologySpecimenType SpecimenType,
    string AnatomicSite,
    string? ClinicalHistoryAndDiagnosis,
    string? FixativeUsed,
    PathologyCaseStatus Status,
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

public sealed record PathologyCaseSummaryDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string PathologyNumber,
    PathologySpecimenType SpecimenType,
    string AnatomicSite,
    PathologyCaseStatus Status,
    DateTime? ReceivedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    DateTime CreatedAtUtc);
