namespace HospitalManagement.Contracts.Scheduling;

public sealed record DoctorScheduleResponse(
    Guid Id,
    Guid DoctorId,
    Guid DepartmentId,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool IsActive,
    IReadOnlyList<ScheduleBreakDto> Breaks);

public sealed record DoctorLeaveBlockResponse(
    Guid Id,
    Guid DoctorId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason,
    bool IsActive);

public sealed record AppointmentSlotResponse(
    Guid Id,
    Guid DoctorId,
    Guid DepartmentId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Status,
    Guid? HeldByPersonId,
    long Version);

public sealed record DoctorAvailabilityDayResponse(
    DateOnly Date,
    IReadOnlyList<AppointmentSlotResponse> Slots);

public sealed record AppointmentDetailResponse(
    Guid Id,
    Guid SlotId,
    Guid PatientId,
    Guid DoctorId,
    Guid DepartmentId,
    DateTime AppointmentTimeUtc,
    string Status,
    string? ReasonForVisit,
    string? CancellationReason,
    DateTime? CancelledAtUtc,
    DateTime? CheckedInAtUtc,
    DateTime? CompletedAtUtc,
    int? QueueNumber,
    long Version);
