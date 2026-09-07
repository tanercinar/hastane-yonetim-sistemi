namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class SurgeryBooking
{
    private SurgeryBooking()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string BookingProtocolNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public Guid? EncounterId
    {
        get; private set;
    }
    public Guid DepartmentId
    {
        get; private set;
    }
    public string DepartmentName { get; private set; } = string.Empty;
    public string ProcedureName { get; private set; } = string.Empty;
    public string ProcedureCode { get; private set; } = string.Empty;
    public SurgeryUrgency Urgency
    {
        get; private set;
    }
    public Guid OperatingRoomId
    {
        get; private set;
    }
    public Guid LeadSurgeonDoctorId
    {
        get; private set;
    }
    public Guid AnesthesiologistDoctorId
    {
        get; private set;
    }
    public Guid? OperatingNurseStaffId
    {
        get; private set;
    }
    public DateTime ScheduledStartTimeUtc
    {
        get; private set;
    }
    public DateTime ScheduledEndTimeUtc
    {
        get; private set;
    }
    public SurgeryBookingStatus Status
    {
        get; private set;
    }
    public PreOpChecklistInfo? PreOpChecklist
    {
        get; private set;
    }
    public string? ClinicalNotes
    {
        get; private set;
    }
    public string? CancellationReason
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

    public static SurgeryBooking Create(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        Guid departmentId,
        string departmentName,
        string procedureName,
        string procedureCode,
        SurgeryUrgency urgency,
        Guid operatingRoomId,
        Guid leadSurgeonDoctorId,
        Guid anesthesiologistDoctorId,
        Guid? operatingNurseStaffId,
        DateTime scheduledStartTimeUtc,
        DateTime scheduledEndTimeUtc,
        string? clinicalNotes,
        DateTime nowUtc,
        string? protocolNumber = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Ameliyat randevu ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Cerrahi anabilim dalı / bölüm ID boş olamaz.", nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new ArgumentException("Bölüm adı boş olamaz.", nameof(departmentName));
        }

        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new ArgumentException("Ameliyat/İşlem adı boş olamaz.", nameof(procedureName));
        }

        if (string.IsNullOrWhiteSpace(procedureCode))
        {
            throw new ArgumentException("İşlem kodu boş olamaz.", nameof(procedureCode));
        }

        if (operatingRoomId == Guid.Empty)
        {
            throw new ArgumentException("Ameliyathane salon ID boş olamaz.", nameof(operatingRoomId));
        }

        if (leadSurgeonDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu cerrah hekim ID boş olamaz.", nameof(leadSurgeonDoctorId));
        }

        if (anesthesiologistDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Anestezi hekimi ID boş olamaz.", nameof(anesthesiologistDoctorId));
        }

        if (leadSurgeonDoctorId == anesthesiologistDoctorId)
        {
            throw new ArgumentException("Sorumlu cerrah ile anestezi hekimi aynı kişi olamaz.", nameof(anesthesiologistDoctorId));
        }

        if (scheduledStartTimeUtc >= scheduledEndTimeUtc)
        {
            throw new ArgumentException("Ameliyat başlangıç zamanı bitiş zamanından önce olmalıdır.", nameof(scheduledStartTimeUtc));
        }

        var proto = !string.IsNullOrWhiteSpace(protocolNumber)
            ? protocolNumber.Trim()
            : GenerateProtocolNumber(nowUtc);

        return new SurgeryBooking
        {
            Id = id,
            BookingProtocolNumber = proto,
            PatientId = patientId,
            EncounterId = encounterId,
            DepartmentId = departmentId,
            DepartmentName = departmentName.Trim(),
            ProcedureName = procedureName.Trim(),
            ProcedureCode = procedureCode.Trim(),
            Urgency = urgency,
            OperatingRoomId = operatingRoomId,
            LeadSurgeonDoctorId = leadSurgeonDoctorId,
            AnesthesiologistDoctorId = anesthesiologistDoctorId,
            OperatingNurseStaffId = operatingNurseStaffId,
            ScheduledStartTimeUtc = scheduledStartTimeUtc,
            ScheduledEndTimeUtc = scheduledEndTimeUtc,
            Status = SurgeryBookingStatus.Scheduled,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Reschedule(
        Guid operatingRoomId,
        DateTime newStartUtc,
        DateTime newEndUtc,
        DateTime nowUtc)
    {
        if (Status is SurgeryBookingStatus.InProgress or SurgeryBookingStatus.Completed or SurgeryBookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki ameliyat yeniden planlanamaz.");
        }

        if (operatingRoomId == Guid.Empty)
        {
            throw new ArgumentException("Ameliyathane salon ID boş olamaz.", nameof(operatingRoomId));
        }

        if (newStartUtc >= newEndUtc)
        {
            throw new ArgumentException("Başlangıç zamanı bitiş zamanından önce olmalıdır.", nameof(newStartUtc));
        }

        OperatingRoomId = operatingRoomId;
        ScheduledStartTimeUtc = newStartUtc;
        ScheduledEndTimeUtc = newEndUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void UpdateTeam(
        Guid leadSurgeonDoctorId,
        Guid anesthesiologistDoctorId,
        Guid? operatingNurseStaffId,
        DateTime nowUtc)
    {
        if (Status is SurgeryBookingStatus.Completed or SurgeryBookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Tamamlanmış veya iptal edilmiş ameliyatın ekibi değiştirilemez.");
        }

        if (leadSurgeonDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu cerrah hekim ID boş olamaz.", nameof(leadSurgeonDoctorId));
        }

        if (anesthesiologistDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Anestezi hekimi ID boş olamaz.", nameof(anesthesiologistDoctorId));
        }

        if (leadSurgeonDoctorId == anesthesiologistDoctorId)
        {
            throw new ArgumentException("Sorumlu cerrah ile anestezi hekimi aynı kişi olamaz.", nameof(anesthesiologistDoctorId));
        }

        LeadSurgeonDoctorId = leadSurgeonDoctorId;
        AnesthesiologistDoctorId = anesthesiologistDoctorId;
        OperatingNurseStaffId = operatingNurseStaffId;
        UpdatedAtUtc = nowUtc;
    }

    public void RecordPreOpChecklist(PreOpChecklistInfo checklist, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        if (Status is SurgeryBookingStatus.Completed or SurgeryBookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Tamamlanmış veya iptal edilmiş ameliyat için kontrol listesi güncellenemez.");
        }

        PreOpChecklist = checklist;
        if (checklist.IsFullyCleared && Status == SurgeryBookingStatus.Scheduled)
        {
            Status = SurgeryBookingStatus.PreOpCleared;
        }

        UpdatedAtUtc = nowUtc;
    }

    public void MarkInProgress(DateTime nowUtc)
    {
        if (Status is SurgeryBookingStatus.Cancelled or SurgeryBookingStatus.Completed)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki ameliyat başlatılamaz.");
        }

        Status = SurgeryBookingStatus.InProgress;
        UpdatedAtUtc = nowUtc;
    }

    public void MarkCompleted(DateTime nowUtc)
    {
        if (Status != SurgeryBookingStatus.InProgress && Status != SurgeryBookingStatus.PreOpCleared && Status != SurgeryBookingStatus.Scheduled)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki ameliyat tamamlanamaz.");
        }

        Status = SurgeryBookingStatus.Completed;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == SurgeryBookingStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış ameliyat iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(reason));
        }

        Status = SurgeryBookingStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Postpone(string reason, DateTime nowUtc)
    {
        if (Status == SurgeryBookingStatus.Completed || Status == SurgeryBookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş ameliyat ertelenemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Erteleme gerekçesi boş olamaz.", nameof(reason));
        }

        Status = SurgeryBookingStatus.Postponed;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
    }

    private static string GenerateProtocolNumber(DateTime date)
    {
        var randomSuffix = Random.Shared.Next(100000, 999999);
        return $"DEMO-SURG-{date:yyyyMMdd}-{randomSuffix}";
    }
}
