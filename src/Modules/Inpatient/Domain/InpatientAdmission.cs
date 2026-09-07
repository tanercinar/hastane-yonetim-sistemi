namespace HospitalManagement.Modules.Inpatient.Domain;

public sealed class InpatientAdmission
{
    private InpatientAdmission()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string AdmissionNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public Guid? EncounterId
    {
        get; private set;
    }
    public Guid OrderingDoctorId
    {
        get; private set;
    }
    public Guid AttendingDoctorId
    {
        get; private set;
    }
    public Guid DepartmentId
    {
        get; private set;
    }
    public Guid AdmittingWardId
    {
        get; private set;
    }
    public Guid? AssignedBedId
    {
        get; private set;
    }
    public string AdmissionReason { get; private set; } = string.Empty;
    public string? DiagnosisCode
    {
        get; private set;
    }
    public string? DiagnosisDescription
    {
        get; private set;
    }
    public string DietType { get; private set; } = "Standard";
    public int FallRiskScore
    {
        get; private set;
    }
    public IsolationType IsolationRequired { get; private set; } = IsolationType.None;
    public int? EstimatedStayDays
    {
        get; private set;
    }

    public AdmissionStatus Status
    {
        get; private set;
    }
    public DateTime RequestedAtUtc
    {
        get; private set;
    }
    public DateTime? AcceptedAtUtc
    {
        get; private set;
    }
    public Guid? AcceptedByUserId
    {
        get; private set;
    }
    public DateTime? AdmittedAtUtc
    {
        get; private set;
    }
    public DateTime? DischargedAtUtc
    {
        get; private set;
    }
    public string? DischargeSummary
    {
        get; private set;
    }
    public DateTime? CancelledAtUtc
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }

    public int Version
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

    public static InpatientAdmission Request(
        Guid id,
        string admissionNumber,
        Guid patientId,
        Guid? encounterId,
        Guid orderingDoctorId,
        Guid attendingDoctorId,
        Guid departmentId,
        Guid admittingWardId,
        string admissionReason,
        string? diagnosisCode,
        string? diagnosisDescription,
        string? dietType,
        int fallRiskScore,
        IsolationType isolationRequired,
        int? estimatedStayDays,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Yatış ID boş olamaz.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(admissionNumber))
        {
            throw new ArgumentException("Yatış numarası boş olamaz.", nameof(admissionNumber));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (orderingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("İsteyen hekim ID boş olamaz.", nameof(orderingDoctorId));
        }

        if (attendingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu hekim ID boş olamaz.", nameof(attendingDoctorId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm ID boş olamaz.", nameof(departmentId));
        }

        if (admittingWardId == Guid.Empty)
        {
            throw new ArgumentException("Yatış servisi ID boş olamaz.", nameof(admittingWardId));
        }

        if (string.IsNullOrWhiteSpace(admissionReason))
        {
            throw new ArgumentException("Yatış endikasyonu / gerekçesi boş olamaz.", nameof(admissionReason));
        }

        return new InpatientAdmission
        {
            Id = id,
            AdmissionNumber = admissionNumber.Trim(),
            PatientId = patientId,
            EncounterId = encounterId,
            OrderingDoctorId = orderingDoctorId,
            AttendingDoctorId = attendingDoctorId,
            DepartmentId = departmentId,
            AdmittingWardId = admittingWardId,
            AdmissionReason = admissionReason.Trim(),
            DiagnosisCode = string.IsNullOrWhiteSpace(diagnosisCode) ? null : diagnosisCode.Trim(),
            DiagnosisDescription = string.IsNullOrWhiteSpace(diagnosisDescription) ? null : diagnosisDescription.Trim(),
            DietType = string.IsNullOrWhiteSpace(dietType) ? "Standard" : dietType.Trim(),
            FallRiskScore = Math.Max(0, fallRiskScore),
            IsolationRequired = isolationRequired,
            EstimatedStayDays = estimatedStayDays is > 0 ? estimatedStayDays : null,
            Status = AdmissionStatus.Requested,
            RequestedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Accept(Guid acceptedByUserId, DateTime nowUtc)
    {
        if (Status != AdmissionStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca istem aşamasındaki ({AdmissionStatus.Requested}) yatışlar onaylanabilir. Mevcut durum: {Status}");
        }

        Status = AdmissionStatus.Accepted;
        AcceptedByUserId = acceptedByUserId;
        AcceptedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Admit(Guid bedId, DateTime nowUtc)
    {
        if (bedId == Guid.Empty)
        {
            throw new ArgumentException("Yatak ID boş olamaz.", nameof(bedId));
        }

        if (Status != AdmissionStatus.Accepted && Status != AdmissionStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca onaylanmış veya talep edilmiş yatışlar yatağa kabul edilebilir. Mevcut durum: {Status}");
        }

        Status = AdmissionStatus.Admitted;
        AssignedBedId = bedId;
        AdmittedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void AssignBedDirectly(Guid bedId, DateTime nowUtc)
    {
        if (bedId == Guid.Empty)
        {
            throw new ArgumentException("Yatak ID boş olamaz.", nameof(bedId));
        }

        AssignedBedId = bedId;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ClearBed()
    {
        AssignedBedId = null;
        Version++;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == AdmissionStatus.Discharged)
        {
            throw new InvalidOperationException("Taburcu edilmiş bir yatış iptal edilemez.");
        }

        if (Status == AdmissionStatus.Cancelled)
        {
            throw new InvalidOperationException("Yatış zaten iptal edilmiş durumda.");
        }

        Status = AdmissionStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Gerekçe belirtilmedi" : reason.Trim();
        CancelledAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Discharge(string summary, DateTime nowUtc)
    {
        if (Status != AdmissionStatus.Admitted && Status != AdmissionStatus.Transferring)
        {
            throw new InvalidOperationException($"Yalnızca yatakta aktif olan ({AdmissionStatus.Admitted}) yatışlar taburcu edilebilir. Mevcut durum: {Status}");
        }

        Status = AdmissionStatus.Discharged;
        DischargeSummary = string.IsNullOrWhiteSpace(summary) ? "Taburculuk özeti düzenlendi." : summary.Trim();
        DischargedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void UpdateCareDetails(
        Guid attendingDoctorId,
        string dietType,
        int fallRiskScore,
        IsolationType isolationRequired,
        DateTime nowUtc)
    {
        if (Status is AdmissionStatus.Discharged or AdmissionStatus.Cancelled)
        {
            throw new InvalidOperationException("Sonlandırılmış veya iptal edilmiş yatışın bakım detayları güncellenemez.");
        }

        if (attendingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu hekim ID boş olamaz.", nameof(attendingDoctorId));
        }

        AttendingDoctorId = attendingDoctorId;
        DietType = string.IsNullOrWhiteSpace(dietType) ? "Standard" : dietType.Trim();
        FallRiskScore = Math.Max(0, fallRiskScore);
        IsolationRequired = isolationRequired;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void InitiateTransfer(DateTime nowUtc)
    {
        if (Status != AdmissionStatus.Admitted)
        {
            throw new InvalidOperationException($"Yalnızca aktif yatıştakiler transfer sürecine alınabilir. Mevcut durum: {Status}");
        }

        Status = AdmissionStatus.Transferring;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void CompleteTransfer(Guid newWardId, Guid newDepartmentId, Guid newBedId, DateTime nowUtc)
    {
        if (Status != AdmissionStatus.Transferring && Status != AdmissionStatus.Admitted)
        {
            throw new InvalidOperationException($"Yalnızca transfer veya aktif yatıştakilerin transferi tamamlanabilir. Mevcut durum: {Status}");
        }

        if (newWardId == Guid.Empty)
            throw new ArgumentException("Yeni servis ID boş olamaz.", nameof(newWardId));
        if (newDepartmentId == Guid.Empty)
            throw new ArgumentException("Yeni bölüm ID boş olamaz.", nameof(newDepartmentId));
        if (newBedId == Guid.Empty)
            throw new ArgumentException("Yeni yatak ID boş olamaz.", nameof(newBedId));

        AdmittingWardId = newWardId;
        DepartmentId = newDepartmentId;
        AssignedBedId = newBedId;
        Status = AdmissionStatus.Admitted;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void CancelTransfer(DateTime nowUtc)
    {
        if (Status == AdmissionStatus.Transferring)
        {
            Status = AdmissionStatus.Admitted;
            UpdatedAtUtc = nowUtc;
            Version++;
        }
    }
}
