using System.Security.Claims;

namespace HospitalManagement.Modules.Scheduling.Application;

public interface ISchedulingService
{
    Task<SchedulingOperationResult<DoctorScheduleDto>> CreateDoctorScheduleAsync(
        ClaimsPrincipal actor,
        CreateDoctorScheduleCommand command,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<IReadOnlyList<DoctorScheduleDto>>> GetDoctorSchedulesAsync(
        ClaimsPrincipal actor,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<DoctorLeaveBlockDto>> CreateDoctorLeaveBlockAsync(
        ClaimsPrincipal actor,
        CreateDoctorLeaveBlockCommand command,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<int>> GenerateDoctorSlotsAsync(
        ClaimsPrincipal actor,
        GenerateDoctorSlotsCommand command,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<IReadOnlyList<AvailabilityDayDto>>> GetDoctorAvailabilityAsync(
        GetDoctorAvailabilityQuery query,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> BookAppointmentAsync(
        ClaimsPrincipal actor,
        BookAppointmentCommand command,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> CancelAppointmentAsync(
        ClaimsPrincipal actor,
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> CheckInAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> CompleteAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> MarkNoShowAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<AppointmentDto>> GetAppointmentByIdAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<IReadOnlyList<AppointmentDto>>> GetPatientAppointmentsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<SchedulingOperationResult<IReadOnlyList<AppointmentDto>>> GetDailyAppointmentsAsync(
        ClaimsPrincipal actor,
        DateOnly appointmentDate,
        Guid? doctorId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
