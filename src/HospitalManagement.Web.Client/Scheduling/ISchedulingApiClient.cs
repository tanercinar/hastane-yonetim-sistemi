using HospitalManagement.Contracts.Scheduling;

namespace HospitalManagement.Web.Client.Scheduling;

public interface ISchedulingApiClient
{
    Task<IReadOnlyList<DoctorScheduleResponse>?> GetDoctorSchedulesAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DoctorAvailabilityDayResponse>?> GetDoctorAvailabilityAsync(
        Guid doctorId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? timeZoneId = null,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetailResponse?> BookAppointmentAsync(
        BookAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetailResponse?> CancelAppointmentAsync(
        Guid appointmentId,
        CancelAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetailResponse?> CheckInAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetailResponse?> MarkNoShowAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<AppointmentDetailResponse?> GetAppointmentByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDetailResponse>?> GetPatientAppointmentsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentDetailResponse>?> GetDailyAppointmentsAsync(
        DateOnly? appointmentDate = null,
        Guid? doctorId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
