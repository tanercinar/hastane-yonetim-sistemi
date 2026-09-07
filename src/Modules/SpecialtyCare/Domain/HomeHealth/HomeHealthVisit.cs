namespace HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;

public sealed class HomeHealthVisit
{
    private HomeHealthVisit()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public Guid? EncounterId
    {
        get; private set;
    }
    public string ProtocolNumber { get; private set; } = string.Empty;

    public HomeCareServiceType ServiceType
    {
        get; private set;
    }
    public HomeVisitPriority Priority
    {
        get; private set;
    }
    public HomeVisitStatus Status
    {
        get; private set;
    }

    public DateTime RequestedDateUtc
    {
        get; private set;
    }
    public DateTime? ScheduledDateUtc
    {
        get; private set;
    }
    public DateTime? VisitStartedAtUtc
    {
        get; private set;
    }
    public DateTime? VisitCompletedAtUtc
    {
        get; private set;
    }

    // Minimum Location & Contact info for privacy
    public string City { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string AddressDetail { get; private set; } = string.Empty;
    public string ContactPhone { get; private set; } = string.Empty;

    public Guid RequestedByStaffId
    {
        get; private set;
    }
    public Guid? AssignedStaffId
    {
        get; private set;
    }

    public string? ClinicalNotes
    {
        get; private set;
    }
    public string? VitalsSummaryNotes
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    public static HomeHealthVisit Request(
        Guid id,
        Guid patientId,
        HomeCareServiceType serviceType,
        HomeVisitPriority priority,
        string city,
        string district,
        string addressDetail,
        string contactPhone,
        Guid requestedByStaffId,
        string? initialNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Evde sağlık talep ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("İl bilgisi boş olamaz.", nameof(city));
        }

        if (string.IsNullOrWhiteSpace(district))
        {
            throw new ArgumentException("İlçe bilgisi boş olamaz.", nameof(district));
        }

        if (string.IsNullOrWhiteSpace(addressDetail))
        {
            throw new ArgumentException("Adres detayı boş olamaz.", nameof(addressDetail));
        }

        if (string.IsNullOrWhiteSpace(contactPhone))
        {
            throw new ArgumentException("İletişim telefonu boş olamaz.", nameof(contactPhone));
        }

        if (requestedByStaffId == Guid.Empty)
        {
            throw new ArgumentException("Talebi açan personel ID boş olamaz.", nameof(requestedByStaffId));
        }

        var protocol = $"DEMO-HOM-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new HomeHealthVisit
        {
            Id = id,
            PatientId = patientId,
            ProtocolNumber = protocol,
            ServiceType = serviceType,
            Priority = priority,
            Status = HomeVisitStatus.Requested,
            RequestedDateUtc = nowUtc,
            City = city.Trim(),
            District = district.Trim(),
            AddressDetail = addressDetail.Trim(),
            ContactPhone = contactPhone.Trim(),
            RequestedByStaffId = requestedByStaffId,
            ClinicalNotes = string.IsNullOrWhiteSpace(initialNotes) ? null : initialNotes.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void AssignTeam(Guid assignedStaffId, DateTime scheduledDateUtc, DateTime nowUtc)
    {
        if (assignedStaffId == Guid.Empty)
        {
            throw new ArgumentException("Görevlendirilen personel ID boş olamaz.", nameof(assignedStaffId));
        }

        if (Status is HomeVisitStatus.Completed or HomeVisitStatus.Cancelled)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki ziyaret görevlendirilemez.");
        }

        AssignedStaffId = assignedStaffId;
        ScheduledDateUtc = scheduledDateUtc;
        Status = HomeVisitStatus.Assigned;
        UpdatedAtUtc = nowUtc;
    }

    public void StartVisit(DateTime nowUtc)
    {
        if (Status != HomeVisitStatus.Assigned && Status != HomeVisitStatus.Approved)
        {
            throw new InvalidOperationException("Ziyaret yalnızca atanmış veya onaylanmış durumda başlatılabilir.");
        }

        Status = HomeVisitStatus.InProgress;
        VisitStartedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void CompleteVisit(string clinicalNotes, string? vitalsSummaryNotes, Guid encounterId, DateTime nowUtc)
    {
        if (Status != HomeVisitStatus.InProgress && Status != HomeVisitStatus.Assigned)
        {
            throw new InvalidOperationException("Ziyaret devam ediyor veya atanmış durumda olmadan tamamlanamaz.");
        }

        if (string.IsNullOrWhiteSpace(clinicalNotes))
        {
            throw new ArgumentException("Ziyaret sonuç klinik notu zorunludur.", nameof(clinicalNotes));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Evde sağlık karşılaşma ID boş olamaz.", nameof(encounterId));
        }

        Status = HomeVisitStatus.Completed;
        VisitCompletedAtUtc = nowUtc;
        EncounterId = encounterId;
        VitalsSummaryNotes = string.IsNullOrWhiteSpace(vitalsSummaryNotes) ? null : vitalsSummaryNotes.Trim();
        ClinicalNotes = string.IsNullOrWhiteSpace(ClinicalNotes)
            ? clinicalNotes.Trim()
            : $"{ClinicalNotes}\n[Ziyaret Sonuç Notu]: {clinicalNotes.Trim()}";
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == HomeVisitStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış ziyaret iptal edilemez.");
        }

        Status = HomeVisitStatus.Cancelled;
        ClinicalNotes = string.IsNullOrWhiteSpace(ClinicalNotes)
            ? $"[İptal Gerekçesi]: {reason.Trim()}"
            : $"{ClinicalNotes}\n[İptal Gerekçesi]: {reason.Trim()}";
        UpdatedAtUtc = nowUtc;
    }
}
