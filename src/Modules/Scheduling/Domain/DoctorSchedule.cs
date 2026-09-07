namespace HospitalManagement.Modules.Scheduling.Domain;

public sealed class DoctorSchedule
{
    private readonly List<ScheduleBreak> _breaks = [];

    private DoctorSchedule()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid DoctorId
    {
        get; private set;
    }

    public Guid DepartmentId
    {
        get; private set;
    }

    public DayOfWeek DayOfWeek
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

    public int SlotDurationMinutes
    {
        get; private set;
    }

    public bool IsActive
    {
        get; private set;
    }

    public IReadOnlyList<ScheduleBreak> Breaks => _breaks.AsReadOnly();

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public static DoctorSchedule Create(
        Guid id,
        Guid doctorId,
        Guid departmentId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateTime createdAtUtc)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Çalışma başlangıç saati bitiş saatinden önce olmalıdır.", nameof(startTime));
        }

        if (slotDurationMinutes is < 5 or > 120)
        {
            throw new ArgumentOutOfRangeException(nameof(slotDurationMinutes), "Randevu süresi 5 ile 120 dakika arasında olmalıdır.");
        }

        return new DoctorSchedule
        {
            Id = id,
            DoctorId = doctorId,
            DepartmentId = departmentId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = slotDurationMinutes,
            IsActive = true,
            CreatedAtUtc = createdAtUtc,
        };
    }

    public void AddBreak(Guid breakId, TimeOnly startTime, TimeOnly endTime, string reason)
    {
        if (startTime < StartTime || endTime > EndTime)
        {
            throw new InvalidOperationException("Mola süresi çalışma saatleri penceresi içinde olmalıdır.");
        }

        if (startTime >= endTime)
        {
            throw new ArgumentException("Mola başlangıcı bitişinden önce olmalıdır.", nameof(startTime));
        }

        foreach (var existing in _breaks)
        {
            if (startTime < existing.EndTime && endTime > existing.StartTime)
            {
                throw new InvalidOperationException("Molalar birbiriyle çakışamaz.");
            }
        }

        _breaks.Add(ScheduleBreak.Create(breakId, Id, startTime, endTime, reason));
    }

    public void ClearBreaks()
    {
        _breaks.Clear();
    }

    public void SetActiveStatus(bool isActive, DateTime updatedAtUtc)
    {
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }
}
