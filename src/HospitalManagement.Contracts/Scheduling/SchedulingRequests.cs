namespace HospitalManagement.Contracts.Scheduling;

public sealed record ScheduleBreakDto(
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Reason);

public sealed record CreateDoctorScheduleRequest
{
    public Guid DoctorId
    {
        get; init;
    }

    public Guid DepartmentId
    {
        get; init;
    }

    public string DayOfWeek { get; init; } = "Monday";

    public TimeOnly StartTime
    {
        get; init;
    }

    public TimeOnly EndTime
    {
        get; init;
    }

    public int SlotDurationMinutes { get; init; } = 15;

    public IReadOnlyList<ScheduleBreakDto>? Breaks
    {
        get; init;
    }
}

public sealed record CreateDoctorLeaveBlockRequest
{
    public Guid DoctorId
    {
        get; init;
    }

    public DateTime StartUtc
    {
        get; init;
    }

    public DateTime EndUtc
    {
        get; init;
    }

    public string Reason { get; init; } = "İzin";
}

public sealed record GenerateSlotsRequest
{
    public Guid DoctorId
    {
        get; init;
    }

    public DateOnly StartDate
    {
        get; init;
    }

    public DateOnly EndDate
    {
        get; init;
    }

    public string TimeZoneId { get; init; } = "Europe/Istanbul";
}

public sealed record BookAppointmentRequest
{
    public Guid SlotId
    {
        get; init;
    }

    public Guid PatientId
    {
        get; init;
    }

    public string? ReasonForVisit
    {
        get; init;
    }
}

public sealed record CancelAppointmentRequest
{
    public string Reason { get; init; } = "Hasta İsteği";
}
