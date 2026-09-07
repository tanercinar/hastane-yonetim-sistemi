using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class ConsultationRequest : IHasConcurrencyVersion
{
    private ConsultationRequest()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid EncounterId
    {
        get; private set;
    }

    public Guid PatientId
    {
        get; private set;
    }

    public Guid RequestingPractitionerId
    {
        get; private set;
    }

    public Guid TargetDepartmentId
    {
        get; private set;
    }

    public Guid? TargetPractitionerId
    {
        get; private set;
    }

    public Guid? AssignedPractitionerId
    {
        get; private set;
    }

    public ConsultationUrgency Urgency
    {
        get; private set;
    }

    public ConsultationStatus Status
    {
        get; private set;
    }

    public string ReasonForConsultation { get; private set; } = string.Empty;

    public string ClinicalQuestion { get; private set; } = string.Empty;

    public string? ConsultationReport
    {
        get; private set;
    }

    public string? Recommendation
    {
        get; private set;
    }

    public string? DeclineReason
    {
        get; private set;
    }

    public string? CancellationReason
    {
        get; private set;
    }

    public string? EnteredInErrorReason
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

    public DateTime? CompletedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public static ConsultationRequest Create(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid requestingPractitionerId,
        Guid targetDepartmentId,
        Guid? targetPractitionerId,
        ConsultationUrgency urgency,
        string reasonForConsultation,
        string clinicalQuestion,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Konsültasyon kimliği boş olamaz.", nameof(id));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (requestingPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("İsteyen hekim kimliği boş olamaz.", nameof(requestingPractitionerId));
        }

        if (targetDepartmentId == Guid.Empty)
        {
            throw new ArgumentException("Hedef bölüm kimliği boş olamaz.", nameof(targetDepartmentId));
        }

        if (string.IsNullOrWhiteSpace(reasonForConsultation))
        {
            throw new ArgumentException("Konsültasyon gerekçesi boş olamaz.", nameof(reasonForConsultation));
        }

        if (string.IsNullOrWhiteSpace(clinicalQuestion))
        {
            throw new ArgumentException("Klinik soru / danışılan konu boş olamaz.", nameof(clinicalQuestion));
        }

        return new ConsultationRequest
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = patientId,
            RequestingPractitionerId = requestingPractitionerId,
            TargetDepartmentId = targetDepartmentId,
            TargetPractitionerId = targetPractitionerId,
            Urgency = urgency,
            Status = ConsultationStatus.Requested,
            ReasonForConsultation = reasonForConsultation.Trim(),
            ClinicalQuestion = clinicalQuestion.Trim(),
            RequestedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Accept(Guid practitionerId, DateTime nowUtc)
    {
        if (Status != ConsultationStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca talep durumundaki konsültasyonlar kabul edilebilir. Mevcut durum: {Status}");
        }

        if (practitionerId == Guid.Empty)
        {
            throw new ArgumentException("Kabul eden hekim kimliği boş olamaz.", nameof(practitionerId));
        }

        AssignedPractitionerId = practitionerId;
        Status = ConsultationStatus.Accepted;
        AcceptedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Complete(Guid practitionerId, string report, string? recommendation, DateTime nowUtc)
    {
        if (Status != ConsultationStatus.Accepted && Status != ConsultationStatus.InProgress)
        {
            throw new InvalidOperationException($"Konsültasyon tamamlanabilmesi için kabul edilmiş veya işlemde olmalıdır. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(report))
        {
            throw new ArgumentException("Konsültasyon yanıt raporu zorunludur.", nameof(report));
        }

        AssignedPractitionerId = practitionerId;
        Status = ConsultationStatus.Completed;
        ConsultationReport = report.Trim();
        Recommendation = string.IsNullOrWhiteSpace(recommendation) ? null : recommendation.Trim();
        CompletedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Decline(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status != ConsultationStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca talep durumundaki konsültasyonlar reddedilebilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Red gerekçesi zorunludur.", nameof(reason));
        }

        AssignedPractitionerId = practitionerId;
        Status = ConsultationStatus.Declined;
        DeclineReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(Guid requestingPractitionerId, string reason, DateTime nowUtc)
    {
        if (Status == ConsultationStatus.Completed || Status == ConsultationStatus.Cancelled || Status == ConsultationStatus.EnteredInError)
        {
            throw new InvalidOperationException($"Bu durumdaki konsültasyon iptal edilemez. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi zorunludur.", nameof(reason));
        }

        Status = ConsultationStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status == ConsultationStatus.EnteredInError)
        {
            throw new InvalidOperationException("Konsültasyon zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        Status = ConsultationStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
