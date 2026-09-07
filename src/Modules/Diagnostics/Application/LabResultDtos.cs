using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record LabResultItemDto(
    Guid Id,
    Guid LabResultId,
    string ParameterCode,
    string ParameterName,
    decimal? NumericValue,
    string? StringValue,
    string? Unit,
    decimal? ReferenceRangeLow,
    decimal? ReferenceRangeHigh,
    string? ReferenceRangeText,
    LabResultInterpretation Flag,
    string? Notes);

public sealed record LabResultDetailDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid? SpecimenId,
    Guid PatientId,
    string CatalogCode,
    string CatalogItemName,
    LabResultStatus Status,
    Guid? TechnicallyApprovedByUserId,
    DateTime? TechnicallyApprovedAtUtc,
    Guid? ClinicallyApprovedByUserId,
    DateTime? ClinicallyApprovedAtUtc,
    Guid? PreviousResultId,
    string? CorrectionReason,
    string? ClinicalNotes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    int Version,
    IReadOnlyList<LabResultItemDto> Items);

public sealed record LabResultSummaryDto(
    Guid Id,
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid PatientId,
    string CatalogCode,
    string CatalogItemName,
    LabResultStatus Status,
    bool HasCriticalFlag,
    bool HasAbnormalFlag,
    DateTime CreatedAtUtc,
    DateTime? ClinicallyApprovedAtUtc);

public sealed record ParameterValueCommand(
    string ParameterCode,
    decimal? NumericValue,
    string? StringValue,
    string? Notes);

public sealed record CreateDraftLabResultCommand(
    Guid DiagnosticOrderId,
    Guid DiagnosticOrderItemId,
    Guid? SpecimenId,
    Guid PatientId,
    string CatalogCode,
    string CatalogItemName,
    string? ClinicalNotes,
    IReadOnlyList<ParameterValueCommand>? ParameterValues);

public sealed record UpdateLabResultItemsCommand(
    IReadOnlyList<ParameterValueCommand> ParameterValues,
    string? ClinicalNotes);

public sealed record CorrectLabResultCommand(
    string CorrectionReason,
    IReadOnlyList<ParameterValueCommand> CorrectedValues);
