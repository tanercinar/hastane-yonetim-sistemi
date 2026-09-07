namespace HospitalManagement.Modules.Reporting.Application;

public sealed record AppointmentProjectedEvent(
    Guid EventId,
    Guid AppointmentId,
    DateOnly Date,
    Guid DepartmentId,
    string DepartmentName,
    Guid? DoctorId,
    string? DoctorName,
    string PreviousStatus,
    string NewStatus,
    DateTime OccurredAtUtc);

public sealed record DiagnosticProjectedEvent(
    Guid EventId,
    Guid OrderId,
    DateOnly Date,
    string ModalityOrSection,
    string PreviousStatus,
    string NewStatus,
    bool IsCritical,
    double? TurnaroundMinutes,
    DateTime OccurredAtUtc);

public sealed record BedOccupancyProjectedEvent(
    Guid EventId,
    DateOnly Date,
    Guid DepartmentId,
    string DepartmentName,
    string WardType,
    int TotalBeds,
    int OccupiedBeds,
    int PendingTransfers,
    DateTime OccurredAtUtc);

public sealed record PharmacyProjectedEvent(
    Guid EventId,
    DateOnly Date,
    string EventType,
    int PendingDelta,
    int DispensedDelta,
    int? LowStockCount,
    int? NearExpiryCount,
    DateTime OccurredAtUtc);
