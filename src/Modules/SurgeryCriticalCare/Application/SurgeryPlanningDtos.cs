using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public sealed record CreateSurgeryBookingDto(
    Guid PatientId,
    Guid? EncounterId,
    Guid DepartmentId,
    string DepartmentName,
    string ProcedureName,
    string ProcedureCode,
    SurgeryUrgency Urgency,
    Guid OperatingRoomId,
    Guid LeadSurgeonDoctorId,
    Guid AnesthesiologistDoctorId,
    Guid? OperatingNurseStaffId,
    DateTime ScheduledStartTimeUtc,
    DateTime ScheduledEndTimeUtc,
    string? ClinicalNotes);

public sealed record RescheduleSurgeryBookingDto(
    Guid OperatingRoomId,
    DateTime ScheduledStartTimeUtc,
    DateTime ScheduledEndTimeUtc);

public sealed record RecordPreOpChecklistDto(
    bool ConsentSigned,
    bool AnesthesiaClearance,
    bool NpoConfirmed,
    bool BloodProductsReserved,
    bool SiteMarked,
    bool AllergyChecked,
    string? Notes);

public sealed record PreOpChecklistDto(
    bool ConsentSigned,
    bool AnesthesiaClearance,
    bool NpoConfirmed,
    bool BloodProductsReserved,
    bool SiteMarked,
    bool AllergyChecked,
    bool IsFullyCleared,
    Guid CompletedByStaffId,
    DateTime CompletedAtUtc,
    string? Notes);

public sealed record SurgeryBookingDto(
    Guid Id,
    string BookingProtocolNumber,
    Guid PatientId,
    Guid? EncounterId,
    Guid DepartmentId,
    string DepartmentName,
    string ProcedureName,
    string ProcedureCode,
    SurgeryUrgency Urgency,
    Guid OperatingRoomId,
    Guid LeadSurgeonDoctorId,
    Guid AnesthesiologistDoctorId,
    Guid? OperatingNurseStaffId,
    DateTime ScheduledStartTimeUtc,
    DateTime ScheduledEndTimeUtc,
    SurgeryBookingStatus Status,
    PreOpChecklistDto? PreOpChecklist,
    string? ClinicalNotes,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);

public sealed record OperatingRoomDto(
    Guid Id,
    string RoomCode,
    string RoomName,
    bool IsActive,
    int Capacity,
    Guid? SpecialtyDepartmentId);

public enum SurgeryOperationStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationFailed,
}

public sealed record SurgeryOperationResult<T>(
    SurgeryOperationStatus Status,
    T? Value = default,
    string? ErrorMessage = null,
    Dictionary<string, string[]>? ValidationErrors = null)
{
    public bool IsSuccess => Status == SurgeryOperationStatus.Success;
}

public static class SurgeryOperationResult
{
    public static SurgeryOperationResult<T> Success<T>(T value) =>
        new(SurgeryOperationStatus.Success, Value: value);

    public static SurgeryOperationResult<T> NotFound<T>(string message) =>
        new(SurgeryOperationStatus.NotFound, ErrorMessage: message);

    public static SurgeryOperationResult<T> Conflict<T>(string message) =>
        new(SurgeryOperationStatus.Conflict, ErrorMessage: message);

    public static SurgeryOperationResult<T> Validation<T>(Dictionary<string, string[]> errors) =>
        new(SurgeryOperationStatus.ValidationFailed, ValidationErrors: errors);

    public static SurgeryOperationResult<T> Validation<T>(string field, string message) =>
        new(SurgeryOperationStatus.ValidationFailed, ValidationErrors: new Dictionary<string, string[]> { [field] = [message] });
}
