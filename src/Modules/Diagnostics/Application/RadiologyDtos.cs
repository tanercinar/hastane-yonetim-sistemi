using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record RadiologyCatalogItemDto(
    Guid Id,
    string Code,
    string Name,
    RadiologyModality Modality,
    string BodySite,
    string? Description,
    string? PreparationInstructions,
    bool ContrastRequired,
    int EstimatedDurationMinutes,
    bool IsActive);

public sealed record RadiologyStudyDetailDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string AccessionNumber,
    RadiologyModality Modality,
    string ProcedureCode,
    string ProcedureName,
    string BodySite,
    RadiologyStudyStatus Status,
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

public sealed record RadiologyStudySummaryDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid PatientId,
    string AccessionNumber,
    RadiologyModality Modality,
    string ProcedureCode,
    string ProcedureName,
    string BodySite,
    RadiologyStudyStatus Status,
    DateTime? ScheduledAtUtc,
    DateTime? PerformedAtUtc,
    DateTime? ReportFinalizedAtUtc,
    DateTime CreatedAtUtc);
