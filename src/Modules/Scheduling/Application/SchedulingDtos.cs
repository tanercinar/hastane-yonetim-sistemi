using HospitalManagement.Modules.Scheduling.Domain;

namespace HospitalManagement.Modules.Scheduling.Application;

public sealed record BreakItemDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason);

public sealed record DoctorScheduleDto(
    Guid Id,
    Guid DoctorId,
    Guid DepartmentId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    bool IsActive,
    IReadOnlyList<BreakItemDto> Breaks);

public sealed record DoctorLeaveBlockDto(
    Guid Id,
    Guid DoctorId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason,
    bool IsActive);

public sealed record AppointmentSlotDto(
    Guid Id,
    Guid DoctorId,
    Guid DepartmentId,
    DateTime StartUtc,
    DateTime EndUtc,
    SlotStatus Status,
    Guid? HeldByPersonId,
    long Version);

public sealed record AvailabilityDayDto(
    DateOnly Date,
    IReadOnlyList<AppointmentSlotDto> Slots);

public sealed record AppointmentDto(
    Guid Id,
    Guid SlotId,
    Guid PatientId,
    Guid DoctorId,
    Guid DepartmentId,
    DateTime AppointmentTimeUtc,
    AppointmentStatus Status,
    string? ReasonForVisit,
    string? CancellationReason,
    DateTime? CancelledAtUtc,
    DateTime? CheckedInAtUtc,
    DateTime? CompletedAtUtc,
    int? QueueNumber,
    long Version);
