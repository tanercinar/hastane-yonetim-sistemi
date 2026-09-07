using HospitalManagement.Modules.Interoperability.Domain.Mhrs;

namespace HospitalManagement.Modules.Interoperability.Application;

public sealed record MhrsAppointmentDto(
    Guid Id,
    string MhrsAppointmentId,
    string SlotId,
    string PatientNationalId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorName,
    string ClinicName,
    DateTime AppointmentDateTimeUtc,
    string Status,
    string IdempotencyKey,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record BookMhrsAppointmentDto(
    string SlotId,
    string PatientNationalId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorName,
    string ClinicName,
    DateTime AppointmentDateTimeUtc,
    string IdempotencyKey);

public sealed record MhrsSyncSummaryDto(
    int TotalSynced,
    int ConflictsDetected,
    int NewAppointmentsAdded,
    int CancelledAppointments,
    DateTime SyncTimestampUtc);

public interface IMhrsService
{
    Task<List<MhrsSlot>> QueryAvailableSlotsAsync(
        Guid? doctorId = null,
        string? clinicCode = null,
        DateTime? slotDate = null,
        CancellationToken cancellationToken = default);

    Task<MhrsAppointmentDto> BookAppointmentAsync(
        BookMhrsAppointmentDto request,
        CancellationToken cancellationToken = default);

    Task<MhrsAppointmentDto> CancelAppointmentAsync(
        string mhrsAppointmentId,
        string reason,
        bool isDoctor = false,
        CancellationToken cancellationToken = default);

    Task<List<MhrsAppointmentDto>> GetPatientAppointmentsAsync(
        string patientNationalId,
        CancellationToken cancellationToken = default);

    Task<MhrsSyncSummaryDto> SyncWithLocalScheduleAsync(
        DateTime syncDate,
        CancellationToken cancellationToken = default);
}
