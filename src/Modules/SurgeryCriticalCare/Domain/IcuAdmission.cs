namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class IcuAdmission
{
    private IcuAdmission()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string AdmissionProtocolNumber { get; private set; } = string.Empty;
    public Guid InpatientStayId
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
    public Guid IcuBedId
    {
        get; private set;
    }
    public string IcuBedCode { get; private set; } = string.Empty;
    public Guid AttendingDoctorId
    {
        get; private set;
    }
    public Guid? PrimaryNurseId
    {
        get; private set;
    }
    public string AdmissionReason { get; private set; } = string.Empty;
    public IcuAcuityLevel AcuityLevel
    {
        get; private set;
    }
    public int MonitoringFrequencyMinutes
    {
        get; private set;
    }
    public IcuVentilationMode VentilationMode
    {
        get; private set;
    }
    public IcuAdmissionStatus Status
    {
        get; private set;
    }
    public string? CarePlanNotes
    {
        get; private set;
    }
    public DateTime AdmittedAtUtc
    {
        get; private set;
    }
    public DateTime? DischargedAtUtc
    {
        get; private set;
    }
    public string? DischargeNotes
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
    public uint Version
    {
        get; internal set;
    }

    public static IcuAdmission Create(
        Guid id,
        Guid inpatientStayId,
        Guid patientId,
        Guid? encounterId,
        Guid icuBedId,
        string icuBedCode,
        Guid attendingDoctorId,
        Guid? primaryNurseId,
        string admissionReason,
        IcuAcuityLevel acuityLevel,
        int monitoringFrequencyMinutes,
        IcuVentilationMode ventilationMode,
        string? carePlanNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Kabul ID boş olamaz.", nameof(id));
        }

        if (inpatientStayId == Guid.Empty)
        {
            throw new ArgumentException("Yatış takip ID boş olamaz.", nameof(inpatientStayId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (icuBedId == Guid.Empty)
        {
            throw new ArgumentException("Yoğun bakım yatak ID boş olamaz.", nameof(icuBedId));
        }

        if (string.IsNullOrWhiteSpace(icuBedCode))
        {
            throw new ArgumentException("Yoğun bakım yatak kodu boş olamaz.", nameof(icuBedCode));
        }

        if (attendingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu yoğun bakım hekimi seçilmelidir.", nameof(attendingDoctorId));
        }

        if (string.IsNullOrWhiteSpace(admissionReason))
        {
            throw new ArgumentException("Yoğun bakım kabul gerekçesi boş olamaz.", nameof(admissionReason));
        }

        if (monitoringFrequencyMinutes <= 0)
        {
            monitoringFrequencyMinutes = 60; // Varsayılan saatlik
        }

        var protocol = $"DEMO-ICU-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new IcuAdmission
        {
            Id = id,
            AdmissionProtocolNumber = protocol,
            InpatientStayId = inpatientStayId,
            PatientId = patientId,
            EncounterId = encounterId,
            IcuBedId = icuBedId,
            IcuBedCode = icuBedCode.Trim().ToUpperInvariant(),
            AttendingDoctorId = attendingDoctorId,
            PrimaryNurseId = primaryNurseId,
            AdmissionReason = admissionReason.Trim(),
            AcuityLevel = acuityLevel,
            MonitoringFrequencyMinutes = monitoringFrequencyMinutes,
            VentilationMode = ventilationMode,
            CarePlanNotes = string.IsNullOrWhiteSpace(carePlanNotes) ? null : carePlanNotes.Trim(),
            Status = IcuAdmissionStatus.Active,
            AdmittedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void UpdateCarePlan(
        IcuAcuityLevel acuityLevel,
        int monitoringFrequencyMinutes,
        IcuVentilationMode ventilationMode,
        Guid? primaryNurseId,
        string? carePlanNotes,
        DateTime nowUtc)
    {
        if (Status != IcuAdmissionStatus.Active)
        {
            throw new InvalidOperationException("Yalnızca aktif yoğun bakım yatışlarının bakım planı güncellenebilir.");
        }

        AcuityLevel = acuityLevel;
        MonitoringFrequencyMinutes = monitoringFrequencyMinutes > 0 ? monitoringFrequencyMinutes : 60;
        VentilationMode = ventilationMode;
        PrimaryNurseId = primaryNurseId;
        CarePlanNotes = string.IsNullOrWhiteSpace(carePlanNotes) ? null : carePlanNotes.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void TransferOutOrDischarge(
        IcuAdmissionStatus destinationStatus,
        string dischargeNotes,
        DateTime nowUtc)
    {
        if (Status != IcuAdmissionStatus.Active)
        {
            throw new InvalidOperationException("Bu yoğun bakım yatışı zaten sonlandırılmıştır.");
        }

        if (destinationStatus == IcuAdmissionStatus.Active)
        {
            throw new ArgumentException("Çıkış / devir durumu aktif olamaz.", nameof(destinationStatus));
        }

        if (string.IsNullOrWhiteSpace(dischargeNotes))
        {
            throw new ArgumentException("Yoğun bakım çıkış / devir notu boş olamaz.", nameof(dischargeNotes));
        }

        Status = destinationStatus;
        DischargeNotes = dischargeNotes.Trim();
        DischargedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
