using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Scheduling.Domain;

public sealed class AppointmentSlot : IHasConcurrencyVersion
{
    private AppointmentSlot()
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

    public Guid? ScheduleId
    {
        get; private set;
    }

    public DateTime StartUtc
    {
        get; private set;
    }

    public DateTime EndUtc
    {
        get; private set;
    }

    public SlotStatus Status
    {
        get; private set;
    }

    public DateTime? HoldExpirationUtc
    {
        get; private set;
    }

    public Guid? HeldByPersonId
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public static AppointmentSlot Create(
        Guid id,
        Guid doctorId,
        Guid departmentId,
        Guid? scheduleId,
        DateTime startUtc,
        DateTime endUtc,
        DateTime createdAtUtc)
    {
        if (startUtc >= endUtc)
        {
            throw new ArgumentException("Slot başlangıç zamanı bitiş zamanından önce olmalıdır.", nameof(startUtc));
        }

        return new AppointmentSlot
        {
            Id = id,
            DoctorId = doctorId,
            DepartmentId = departmentId,
            ScheduleId = scheduleId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = SlotStatus.Available,
            Version = 1,
            CreatedAtUtc = createdAtUtc,
        };
    }

    public void Hold(Guid personId, TimeSpan holdDuration, DateTime nowUtc)
    {
        if (Status != SlotStatus.Available && !(Status == SlotStatus.Held && HoldExpirationUtc < nowUtc))
        {
            throw new InvalidOperationException("Yalnızca müsait durumdaki bir slot rezerve edilebilir.");
        }

        Status = SlotStatus.Held;
        HeldByPersonId = personId;
        HoldExpirationUtc = nowUtc.Add(holdDuration);
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ReleaseHold(DateTime nowUtc)
    {
        if (Status == SlotStatus.Held)
        {
            Status = SlotStatus.Available;
            HeldByPersonId = null;
            HoldExpirationUtc = null;
            UpdatedAtUtc = nowUtc;
            Version++;
        }
    }

    public void Book(Guid personId, DateTime nowUtc)
    {
        if (Status == SlotStatus.Booked || Status == SlotStatus.Blocked || Status == SlotStatus.Cancelled)
        {
            throw new InvalidOperationException("Bu slot randevu için uygun değildir.");
        }

        if (Status == SlotStatus.Held && HeldByPersonId != personId && HoldExpirationUtc > nowUtc)
        {
            throw new InvalidOperationException("Bu slot başka bir kullanıcı tarafından geçici olarak tutulmaktadır.");
        }

        Status = SlotStatus.Booked;
        HeldByPersonId = personId;
        HoldExpirationUtc = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Block(DateTime nowUtc)
    {
        Status = SlotStatus.Blocked;
        HoldExpirationUtc = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(DateTime nowUtc)
    {
        Status = SlotStatus.Available;
        HeldByPersonId = null;
        HoldExpirationUtc = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
