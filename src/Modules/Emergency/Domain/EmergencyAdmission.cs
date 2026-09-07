namespace HospitalManagement.Modules.Emergency.Domain;

public sealed class EmergencyAdmission
{
    private EmergencyAdmission()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string EmergencyProtocolNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public EmergencyArrivalType ArrivalType
    {
        get; private set;
    }
    public string ChiefComplaint { get; private set; } = string.Empty;
    public string? AdmissionNotes
    {
        get; private set;
    }
    public EmergencyAdmissionStatus Status
    {
        get; private set;
    }
    public DateTime AdmittedAtUtc
    {
        get; private set;
    }
    public Guid AdmittingStaffId
    {
        get; private set;
    }

    public EmergencyTriageInfo? Triage
    {
        get; private set;
    }

    public EmergencyDispositionInfo? Disposition
    {
        get; private set;
    }

    public Guid? AssignedDoctorId
    {
        get; private set;
    }
    public string? AssignedBedOrZone
    {
        get; private set;
    }
    public DateTime? CompletedAtUtc
    {
        get; private set;
    }
    public string? DischargeOrDispositionNotes
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
        get; private set;
    }

    public static EmergencyAdmission Create(
        Guid id,
        Guid patientId,
        EmergencyArrivalType arrivalType,
        string chiefComplaint,
        string? admissionNotes,
        Guid admittingStaffId,
        DateTime nowUtc,
        string? protocolNumber = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Acil kabul kimliği geçerli olmalıdır.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği geçerli olmalıdır.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(chiefComplaint))
        {
            throw new ArgumentException("Başvuru şikâyeti boş olamaz.", nameof(chiefComplaint));
        }

        if (admittingStaffId == Guid.Empty)
        {
            throw new ArgumentException("Kabulü yapan personel kimliği geçerli olmalıdır.", nameof(admittingStaffId));
        }

        var protocol = string.IsNullOrWhiteSpace(protocolNumber)
            ? GenerateProtocolNumber(nowUtc)
            : protocolNumber.Trim();

        return new EmergencyAdmission
        {
            Id = id,
            EmergencyProtocolNumber = protocol,
            PatientId = patientId,
            ArrivalType = arrivalType,
            ChiefComplaint = chiefComplaint.Trim(),
            AdmissionNotes = admissionNotes?.Trim(),
            Status = EmergencyAdmissionStatus.WaitingTriage,
            AdmittedAtUtc = nowUtc,
            AdmittingStaffId = admittingStaffId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void RecordTriage(
        TriageLevel triageLevel,
        string triageCategoryReason,
        Guid triageNurseId,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        decimal? bodyTemperatureCelsius,
        int? respiratoryRate,
        int? oxygenSaturationPercent,
        int? painScale,
        string? consciousness,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        if (Status is EmergencyAdmissionStatus.Discharged or EmergencyAdmissionStatus.TransferredOut
            or EmergencyAdmissionStatus.LeftWithoutBeingSeen or EmergencyAdmissionStatus.Deceased)
        {
            throw new InvalidOperationException($"Sonuçlandırılmış acil başvurusuna ({Status}) triyaj kaydı girilemez.");
        }

        Triage = new EmergencyTriageInfo(
            triageLevel,
            triageCategoryReason,
            nowUtc,
            triageNurseId,
            educationalClassificationAssisted: true,
            systolicBp,
            diastolicBp,
            heartRate,
            bodyTemperatureCelsius,
            respiratoryRate,
            oxygenSaturationPercent,
            painScale,
            consciousness,
            clinicalNotes);

        if (Status == EmergencyAdmissionStatus.WaitingTriage)
        {
            Status = EmergencyAdmissionStatus.TriagedWaitingDoctor;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void AssignDoctor(Guid doctorId, DateTime nowUtc)
    {
        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException("Hekim kimliği geçerli olmalıdır.", nameof(doctorId));
        }

        if (Status is EmergencyAdmissionStatus.Discharged or EmergencyAdmissionStatus.TransferredOut
            or EmergencyAdmissionStatus.LeftWithoutBeingSeen or EmergencyAdmissionStatus.Deceased)
        {
            throw new InvalidOperationException($"Sonuçlandırılmış acil başvurusuna ({Status}) hekim atanamaz.");
        }

        AssignedDoctorId = doctorId;
        if (Status == EmergencyAdmissionStatus.TriagedWaitingDoctor || Status == EmergencyAdmissionStatus.WaitingTriage)
        {
            Status = EmergencyAdmissionStatus.InEvaluation;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void AssignBedOrZone(string? bedOrZone, DateTime nowUtc)
    {
        AssignedBedOrZone = string.IsNullOrWhiteSpace(bedOrZone) ? null : bedOrZone.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void RecordDisposition(
        EmergencyDispositionType dispositionType,
        Guid decidedByDoctorId,
        Guid? targetWardOrIcuId,
        string? targetDepartmentName,
        string dispositionNotes,
        string? followUpInstructions,
        DateTime nowUtc)
    {
        if (decidedByDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Karar veren hekim ID boş olamaz.", nameof(decidedByDoctorId));
        }

        if (string.IsNullOrWhiteSpace(dispositionNotes))
        {
            throw new ArgumentException("Disposition klinik karar özeti/notu boş olamaz.", nameof(dispositionNotes));
        }

        Disposition = new EmergencyDispositionInfo(
            dispositionType,
            decidedByDoctorId,
            nowUtc,
            targetWardOrIcuId,
            string.IsNullOrWhiteSpace(targetDepartmentName) ? null : targetDepartmentName.Trim(),
            dispositionNotes.Trim(),
            string.IsNullOrWhiteSpace(followUpInstructions) ? null : followUpInstructions.Trim());

        DischargeOrDispositionNotes = dispositionNotes.Trim();

        var correspondingStatus = dispositionType switch
        {
            EmergencyDispositionType.DischargeHome => EmergencyAdmissionStatus.Discharged,
            EmergencyDispositionType.AdmitToWard => EmergencyAdmissionStatus.AdmittedToInpatient,
            EmergencyDispositionType.AdmitToIcu => EmergencyAdmissionStatus.AdmittedToIcu,
            EmergencyDispositionType.DirectToSurgery => EmergencyAdmissionStatus.AdmittedToInpatient,
            EmergencyDispositionType.TransferToOtherHospital => EmergencyAdmissionStatus.TransferredOut,
            EmergencyDispositionType.Exitus => EmergencyAdmissionStatus.Deceased,
            _ => EmergencyAdmissionStatus.Discharged,
        };

        UpdateStatus(correspondingStatus, dispositionNotes, nowUtc);
    }

    public void UpdateStatus(EmergencyAdmissionStatus newStatus, string? notes, DateTime nowUtc)
    {
        Status = newStatus;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            DischargeOrDispositionNotes = notes.Trim();
        }

        if (newStatus is EmergencyAdmissionStatus.Discharged or EmergencyAdmissionStatus.TransferredOut
            or EmergencyAdmissionStatus.LeftWithoutBeingSeen or EmergencyAdmissionStatus.Deceased
            or EmergencyAdmissionStatus.AdmittedToInpatient or EmergencyAdmissionStatus.AdmittedToIcu)
        {
            CompletedAtUtc = nowUtc;
        }

        UpdatedAtUtc = nowUtc;
    }

    private static string GenerateProtocolNumber(DateTime date)
    {
        var randomSuffix = Random.Shared.Next(100000, 999999);
        return $"DEMO-EMG-{date:yyyyMMdd}-{randomSuffix}";
    }
}
