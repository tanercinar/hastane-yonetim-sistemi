namespace HospitalManagement.Modules.Inpatient.Domain;

public sealed class Bed
{
    public Guid Id
    {
        get; private set;
    }
    public Guid RoomId
    {
        get; private set;
    }
    public Guid WardId
    {
        get; private set;
    }
    public string BedNumber { get; private set; } = string.Empty;
    public BedStatus Status
    {
        get; private set;
    }
    public Guid? CurrentAdmissionId
    {
        get; private set;
    }
    public Guid? CurrentPatientId
    {
        get; private set;
    }
    public BedPlacementGender GenderConstraint
    {
        get; private set;
    }
    public IsolationType IsolationType
    {
        get; private set;
    }
    public bool HasTelemetry
    {
        get; private set;
    }
    public bool HasOxygen
    {
        get; private set;
    }
    public bool HasVentilator
    {
        get; private set;
    }
    public string? MaintenanceReason
    {
        get; private set;
    }
    public bool IsActive
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    private Bed()
    {
    }

    public static Bed Create(
        Guid id,
        Guid wardId,
        Guid roomId,
        string bedNumber,
        BedPlacementGender genderConstraint,
        IsolationType isolationType,
        bool hasTelemetry,
        bool hasOxygen,
        bool hasVentilator,
        DateTime nowUtc,
        bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Yatak kimliği zorunludur.", nameof(id));
        }

        if (wardId == Guid.Empty)
        {
            throw new ArgumentException("Servis kimliği zorunludur.", nameof(wardId));
        }

        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Oda kimliği zorunludur.", nameof(roomId));
        }

        if (string.IsNullOrWhiteSpace(bedNumber))
        {
            throw new ArgumentException("Yatak numarası zorunludur.", nameof(bedNumber));
        }

        return new Bed
        {
            Id = id,
            WardId = wardId,
            RoomId = roomId,
            BedNumber = bedNumber.Trim(),
            Status = BedStatus.Available,
            CurrentAdmissionId = null,
            CurrentPatientId = null,
            GenderConstraint = genderConstraint,
            IsolationType = isolationType,
            HasTelemetry = hasTelemetry,
            HasOxygen = hasOxygen,
            HasVentilator = hasVentilator,
            MaintenanceReason = null,
            IsActive = isActive,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
            Version = 1,
        };
    }

    public void AssignAdmission(Guid admissionId, Guid patientId, DateTime nowUtc)
    {
        if (admissionId == Guid.Empty)
        {
            throw new ArgumentException("Yatış kimliği zorunludur.", nameof(admissionId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (!IsActive)
        {
            throw new InvalidOperationException("Pasif durumdaki yatağa yatış yapılamaz.");
        }

        if (Status != BedStatus.Available && !(Status == BedStatus.Reserved && CurrentAdmissionId == admissionId))
        {
            throw new InvalidOperationException($"Yatak yatış için uygun değil. Mevcut durum: {Status}");
        }

        Status = BedStatus.Occupied;
        CurrentAdmissionId = admissionId;
        CurrentPatientId = patientId;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ReleaseBed(DateTime nowUtc, bool requireCleaning = true)
    {
        if (Status != BedStatus.Occupied && Status != BedStatus.Reserved)
        {
            throw new InvalidOperationException($"Yalnızca dolu veya rezerve yataklar serbest bırakılabilir. Mevcut durum: {Status}");
        }

        CurrentAdmissionId = null;
        CurrentPatientId = null;
        Status = requireCleaning ? BedStatus.Cleaning : BedStatus.Available;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void CompleteCleaning(DateTime nowUtc)
    {
        if (Status != BedStatus.Cleaning)
        {
            throw new InvalidOperationException($"Yalnızca temizlikteki yatakların temizliği tamamlanabilir. Mevcut durum: {Status}");
        }

        Status = BedStatus.Available;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkUnderMaintenance(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Bakım gerekçesi belirtilmelidir.", nameof(reason));
        }

        if (Status == BedStatus.Occupied)
        {
            throw new InvalidOperationException("Dolu yatak doğrudan bakım dışına alınamaz. Önce hasta transfer edilmeli veya taburcu edilmelidir.");
        }

        Status = BedStatus.Maintenance;
        MaintenanceReason = reason.Trim();
        CurrentAdmissionId = null;
        CurrentPatientId = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void RestoreFromMaintenance(DateTime nowUtc)
    {
        if (Status != BedStatus.Maintenance)
        {
            throw new InvalidOperationException($"Yalnızca bakım dışı yataklar kullanıma açılabilir. Mevcut durum: {Status}");
        }

        Status = BedStatus.Available;
        MaintenanceReason = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Reserve(Guid admissionId, Guid patientId, DateTime nowUtc)
    {
        if (admissionId == Guid.Empty)
        {
            throw new ArgumentException("Yatış kimliği zorunludur.", nameof(admissionId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (Status != BedStatus.Available)
        {
            throw new InvalidOperationException($"Yalnızca boş yataklar rezerve edilebilir. Mevcut durum: {Status}");
        }

        Status = BedStatus.Reserved;
        CurrentAdmissionId = admissionId;
        CurrentPatientId = patientId;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void CancelReservation(DateTime nowUtc)
    {
        if (Status != BedStatus.Reserved)
        {
            throw new InvalidOperationException($"Yalnızca rezerve yatakların rezervasyonu iptal edilebilir. Mevcut durum: {Status}");
        }

        Status = BedStatus.Available;
        CurrentAdmissionId = null;
        CurrentPatientId = null;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
