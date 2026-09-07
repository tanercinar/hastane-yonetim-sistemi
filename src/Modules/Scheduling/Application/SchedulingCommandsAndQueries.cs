namespace HospitalManagement.Modules.Scheduling.Application;

public sealed record CreateDoctorScheduleCommand(
    Guid DoctorId,
    Guid DepartmentId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int SlotDurationMinutes,
    IReadOnlyList<BreakItemDto>? Breaks);

public sealed record CreateDoctorLeaveBlockCommand(
    Guid DoctorId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason);

public sealed record GenerateDoctorSlotsCommand(
    Guid DoctorId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TimeZoneId = "Europe/Istanbul");

public sealed record GetDoctorAvailabilityQuery(
    Guid DoctorId,
    DateOnly StartDate,
    DateOnly EndDate,
    string TimeZoneId = "Europe/Istanbul");

public sealed record BookAppointmentCommand(
    Guid SlotId,
    Guid PatientId,
    string? ReasonForVisit);

public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason);

public enum SchedulingOperationStatus
{
    Succeeded,
    NotFound,
    ValidationFailed,
    Conflict,
    Forbidden,
}

public sealed class SchedulingOperationResult<T>
{
    public SchedulingOperationStatus Status
    {
        get; init;
    }

    public T? Value
    {
        get; init;
    }

    public IReadOnlyDictionary<string, string[]>? Errors
    {
        get; init;
    }
}

public static class SchedulingOperationResult
{
    public static SchedulingOperationResult<T> Success<T>(T value) =>
        new()
        {
            Status = SchedulingOperationStatus.Succeeded,
            Value = value
        };

    public static SchedulingOperationResult<T> NotFound<T>(string message = "Kayıt bulunamadı.") =>
        new()
        {
            Status = SchedulingOperationStatus.NotFound,
            Errors = new Dictionary<string, string[]> { ["scheduling"] = [message] },
        };

    public static SchedulingOperationResult<T> Validation<T>(string key, string message) =>
        new()
        {
            Status = SchedulingOperationStatus.ValidationFailed,
            Errors = new Dictionary<string, string[]> { [key] = [message] },
        };

    public static SchedulingOperationResult<T> Conflict<T>(string message) =>
        new()
        {
            Status = SchedulingOperationStatus.Conflict,
            Errors = new Dictionary<string, string[]> { ["conflict"] = [message] },
        };

    public static SchedulingOperationResult<T> Forbidden<T>(string message = "Bu takvim işlemine erişim yetkiniz bulunmamaktadır.") =>
        new()
        {
            Status = SchedulingOperationStatus.Forbidden,
            Errors = new Dictionary<string, string[]> { ["authorization"] = [message] },
        };
}
