using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Scheduling.Domain;

public sealed class Appointment : IHasConcurrencyVersion
{
    private Appointment()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid SlotId
    {
        get; private set;
    }

    public Guid PatientId
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

    public DateTime AppointmentTimeUtc
    {
        get; private set;
    }

    public AppointmentStatus Status
    {
        get; private set;
    }

    public string? ReasonForVisit
    {
        get; private set;
    }

    public string? CancellationReason
    {
        get; private set;
    }

    public DateTime? CancelledAtUtc
    {
        get; private set;
    }

    public DateTime? CheckedInAtUtc
    {
        get; private set;
    }

    public DateTime? CompletedAtUtc
    {
        get; private set;
    }

    public int? QueueNumber
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

    public static Appointment Create(
        Guid id,
        Guid slotId,
        Guid patientId,
        Guid doctorId,
        Guid departmentId,
        DateTime appointmentTimeUtc,
        string? reasonForVisit,
        DateTime nowUtc)
    {
        return new Appointment
        {
            Id = id,
            SlotId = slotId,
            PatientId = patientId,
            DoctorId = doctorId,
            DepartmentId = departmentId,
            AppointmentTimeUtc = appointmentTimeUtc,
            Status = AppointmentStatus.Confirmed,
            ReasonForVisit = string.IsNullOrWhiteSpace(reasonForVisit) ? null : reasonForVisit.Trim(),
            Version = 1,
            CreatedAtUtc = nowUtc,
        };
    }

    public void CheckIn(int queueNumber, DateTime nowUtc)
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Onaylı' (Confirmed) durumundaki randevular için check-in yapılabilir. Mevcut durum: {Status}");
        }

        if (queueNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queueNumber), "Sıra numarası pozitif bir tam sayı olmalıdır.");
        }

        Status = AppointmentStatus.CheckedIn;
        QueueNumber = queueNumber;
        CheckedInAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Complete(DateTime nowUtc)
    {
        if (Status != AppointmentStatus.CheckedIn)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Giriş Yapıldı' (CheckedIn) durumundaki muayeneler tamamlanabilir. Mevcut durum: {Status}");
        }

        Status = AppointmentStatus.Completed;
        CompletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == AppointmentStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış bir muayene/randevu iptal edilemez.");
        }

        if (Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Randevu zaten iptal edilmiştir.");
        }

        Status = AppointmentStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Belirtilmedi" : reason.Trim();
        CancelledAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkNoShow(DateTime nowUtc)
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Onaylı' randevular için 'Gelmedi' (No-Show) işareti konulabilir. Mevcut durum: {Status}");
        }

        Status = AppointmentStatus.NoShow;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
