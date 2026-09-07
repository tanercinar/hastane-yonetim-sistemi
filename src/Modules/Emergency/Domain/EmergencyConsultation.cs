namespace HospitalManagement.Modules.Emergency.Domain;

public sealed class EmergencyConsultation
{
    private EmergencyConsultation()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid AdmissionId
    {
        get; private set;
    }
    public Guid DepartmentId
    {
        get; private set;
    }
    public string DepartmentName { get; private set; } = string.Empty;
    public Guid RequestedByDoctorId
    {
        get; private set;
    }
    public DateTime RequestedAtUtc
    {
        get; private set;
    }
    public EmergencyConsultationUrgency Urgency
    {
        get; private set;
    }
    public string ClinicalReason { get; private set; } = string.Empty;
    public EmergencyConsultationStatus Status
    {
        get; private set;
    }
    public Guid? ConsultantDoctorId
    {
        get; private set;
    }
    public string? ConsultationResponseNotes
    {
        get; private set;
    }
    public DateTime? RespondedAtUtc
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }
    public uint Version
    {
        get; internal set;
    }

    public static EmergencyConsultation Create(
        Guid id,
        Guid admissionId,
        Guid departmentId,
        string departmentName,
        Guid requestedByDoctorId,
        EmergencyConsultationUrgency urgency,
        string clinicalReason,
        DateTime requestedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Konsültasyon ID boş olamaz.", nameof(id));
        }

        if (admissionId == Guid.Empty)
        {
            throw new ArgumentException("Acil başvuru ID boş olamaz.", nameof(admissionId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm ID boş olamaz.", nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(departmentName))
        {
            throw new ArgumentException("Bölüm adı boş olamaz.", nameof(departmentName));
        }

        if (requestedByDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Konsültasyon isteyen hekim ID boş olamaz.", nameof(requestedByDoctorId));
        }

        if (string.IsNullOrWhiteSpace(clinicalReason))
        {
            throw new ArgumentException("Konsültasyon klinik gerekçesi boş olamaz.", nameof(clinicalReason));
        }

        return new EmergencyConsultation
        {
            Id = id,
            AdmissionId = admissionId,
            DepartmentId = departmentId,
            DepartmentName = departmentName.Trim(),
            RequestedByDoctorId = requestedByDoctorId,
            RequestedAtUtc = requestedAtUtc,
            Urgency = urgency,
            ClinicalReason = clinicalReason.Trim(),
            Status = EmergencyConsultationStatus.Requested,
        };
    }

    public void Accept(Guid consultantDoctorId, DateTime acceptedAtUtc)
    {
        if (Status != EmergencyConsultationStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca 'Requested' durumundaki konsültasyon kabul edilebilir. Mevcut durum: {Status}");
        }

        if (consultantDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Konsültan hekim ID boş olamaz.", nameof(consultantDoctorId));
        }

        ConsultantDoctorId = consultantDoctorId;
        Status = EmergencyConsultationStatus.Accepted;
    }

    public void Respond(Guid consultantDoctorId, string responseNotes, DateTime respondedAtUtc)
    {
        if (Status == EmergencyConsultationStatus.Completed || Status == EmergencyConsultationStatus.Cancelled)
        {
            throw new InvalidOperationException($"Sonuçlanmış veya iptal edilmiş konsültasyona yanıt verilemez. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(responseNotes))
        {
            throw new ArgumentException("Konsültasyon yanıt notu boş olamaz.", nameof(responseNotes));
        }

        ConsultantDoctorId = consultantDoctorId != Guid.Empty ? consultantDoctorId : ConsultantDoctorId;
        ConsultationResponseNotes = responseNotes.Trim();
        Status = EmergencyConsultationStatus.Completed;
        RespondedAtUtc = respondedAtUtc;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == EmergencyConsultationStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış konsültasyon iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(reason));
        }

        Status = EmergencyConsultationStatus.Cancelled;
        CancellationReason = reason.Trim();
        RespondedAtUtc = nowUtc;
    }
}
