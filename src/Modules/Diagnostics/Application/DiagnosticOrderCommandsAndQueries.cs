using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public sealed record CreateDiagnosticOrderItemCommand(
    string CatalogCode,
    string CatalogItemName,
    string Category,
    string? SpecialInstructions);

public sealed record CreateDiagnosticOrderDraftCommand(
    Guid PatientId,
    Guid EncounterId,
    Guid DepartmentId,
    Guid PlacingDoctorId,
    DiagnosticOrderType OrderType,
    DiagnosticOrderPriority Priority,
    string? ClinicalIndication,
    string? OrderNotes,
    IReadOnlyList<CreateDiagnosticOrderItemCommand> Items);

public sealed record UpdateDiagnosticOrderDraftCommand(
    Guid OrderId,
    Guid PlacingDoctorId,
    DiagnosticOrderPriority? Priority,
    string? ClinicalIndication,
    string? OrderNotes,
    IReadOnlyList<CreateDiagnosticOrderItemCommand> Items);

public sealed record PlaceDiagnosticOrderCommand(
    Guid OrderId,
    Guid PlacingDoctorId,
    string? Notes = null);

public sealed record CancelDiagnosticOrderCommand(
    Guid OrderId,
    Guid PlacingDoctorId,
    string Reason);

public sealed record MarkDiagnosticOrderEnteredInErrorCommand(
    Guid OrderId,
    Guid DoctorId,
    string Reason);
