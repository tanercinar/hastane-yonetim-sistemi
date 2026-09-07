using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record DiagnosticOrderItemDto(
    Guid Id,
    Guid DiagnosticOrderId,
    string CatalogCode,
    string CatalogItemName,
    string Category,
    DiagnosticOrderItemStatus Status,
    string? SpecialInstructions,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record DiagnosticOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PlacingDoctorId,
    Guid DepartmentId,
    DiagnosticOrderType OrderType,
    DiagnosticOrderPriority Priority,
    DiagnosticOrderStatus Status,
    string? ClinicalIndication,
    string? OrderNotes,
    string? CancellationReason,
    string? EnteredInErrorReason,
    DateTime? PlacedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    uint Version,
    IReadOnlyList<DiagnosticOrderItemDto> Items);

public sealed record DiagnosticOrderSummaryDto(
    Guid Id,
    string OrderNumber,
    Guid PatientId,
    Guid EncounterId,
    Guid PlacingDoctorId,
    Guid DepartmentId,
    DiagnosticOrderType OrderType,
    DiagnosticOrderPriority Priority,
    DiagnosticOrderStatus Status,
    int ItemCount,
    DateTime? PlacedAtUtc,
    DateTime CreatedAtUtc);
