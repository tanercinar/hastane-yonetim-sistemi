namespace HospitalManagement.Modules.Scheduling.Domain;

public sealed class ScheduleBreak
{
    private ScheduleBreak()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid ScheduleId
    {
        get; private set;
    }

    public TimeOnly StartTime
    {
        get; private set;
    }

    public TimeOnly EndTime
    {
        get; private set;
    }

    public string Reason { get; private set; } = string.Empty;

    public static ScheduleBreak Create(
        Guid id,
        Guid scheduleId,
        TimeOnly startTime,
        TimeOnly endTime,
        string reason)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Mola başlangıç zamanı bitiş zamanından önce olmalıdır.", nameof(startTime));
        }

        return new ScheduleBreak
        {
            Id = id,
            ScheduleId = scheduleId,
            StartTime = startTime,
            EndTime = endTime,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Mola" : reason.Trim(),
        };
    }
}
