namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class ClinicalHandoff
{
    private ClinicalHandoff()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string HandoffProtocolNumber { get; private set; } = string.Empty;
    public Guid PatientId
    {
        get; private set;
    }
    public Guid? InpatientStayId
    {
        get; private set;
    }
    public Guid? EncounterId
    {
        get; private set;
    }

    public ClinicalAreaType SourceArea
    {
        get; private set;
    }
    public string SourceLocationDetails { get; private set; } = string.Empty;
    public ClinicalAreaType DestinationArea
    {
        get; private set;
    }
    public string DestinationLocationDetails { get; private set; } = string.Empty;

    public Guid HandingOverStaffId
    {
        get; private set;
    }
    public Guid? ReceivingStaffId
    {
        get; private set;
    }

    // ISBAR Structure
    public string Situation { get; private set; } = string.Empty;
    public string Background { get; private set; } = string.Empty;
    public string Assessment { get; private set; } = string.Empty;
    public string Recommendation { get; private set; } = string.Empty;

    public string? CriticalAlerts
    {
        get; private set;
    }
    public HandoffStatus Status
    {
        get; private set;
    }
    public string? StatusReason
    {
        get; private set;
    }

    public DateTime HandedOverAtUtc
    {
        get; private set;
    }
    public DateTime? AcceptedAtUtc
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

    public static ClinicalHandoff Initiate(
        Guid id,
        Guid patientId,
        Guid? inpatientStayId,
        Guid? encounterId,
        ClinicalAreaType sourceArea,
        string sourceLocationDetails,
        ClinicalAreaType destinationArea,
        string destinationLocationDetails,
        Guid handingOverStaffId,
        string situation,
        string background,
        string assessment,
        string recommendation,
        string? criticalAlerts,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Devir teslim ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (handingOverStaffId == Guid.Empty)
        {
            throw new ArgumentException("Devreden personel ID boş olamaz.", nameof(handingOverStaffId));
        }

        if (string.IsNullOrWhiteSpace(situation))
        {
            throw new ArgumentException("ISBAR Situation (Mevcut Durum) alanı zorunludur.", nameof(situation));
        }

        if (string.IsNullOrWhiteSpace(background))
        {
            throw new ArgumentException("ISBAR Background (Tıbbi Geçmiş) alanı zorunludur.", nameof(background));
        }

        if (string.IsNullOrWhiteSpace(assessment))
        {
            throw new ArgumentException("ISBAR Assessment (Değerlendirme & Bulgular) alanı zorunludur.", nameof(assessment));
        }

        if (string.IsNullOrWhiteSpace(recommendation))
        {
            throw new ArgumentException("ISBAR Recommendation (Öneriler & Açık Görevler) alanı zorunludur.", nameof(recommendation));
        }

        var protocol = $"DEMO-HOF-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new ClinicalHandoff
        {
            Id = id,
            HandoffProtocolNumber = protocol,
            PatientId = patientId,
            InpatientStayId = inpatientStayId,
            EncounterId = encounterId,
            SourceArea = sourceArea,
            SourceLocationDetails = sourceLocationDetails.Trim(),
            DestinationArea = destinationArea,
            DestinationLocationDetails = destinationLocationDetails.Trim(),
            HandingOverStaffId = handingOverStaffId,
            Situation = situation.Trim(),
            Background = background.Trim(),
            Assessment = assessment.Trim(),
            Recommendation = recommendation.Trim(),
            CriticalAlerts = string.IsNullOrWhiteSpace(criticalAlerts) ? null : criticalAlerts.Trim(),
            Status = HandoffStatus.PendingAcceptance,
            HandedOverAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Accept(Guid receivingStaffId, string? acceptanceNote, DateTime nowUtc)
    {
        if (Status != HandoffStatus.PendingAcceptance)
        {
            throw new InvalidOperationException("Yalnızca kabul bekleyen devir teslimler kabul edilebilir.");
        }

        if (receivingStaffId == Guid.Empty)
        {
            throw new ArgumentException("Devralan personel ID boş olamaz.", nameof(receivingStaffId));
        }

        if (receivingStaffId == HandingOverStaffId)
        {
            throw new InvalidOperationException("Devreden personel kendi devir teslimini tek taraflı kabul edemez; karşı ekipten onay gereklidir.");
        }

        ReceivingStaffId = receivingStaffId;
        Status = HandoffStatus.Accepted;
        StatusReason = string.IsNullOrWhiteSpace(acceptanceNote) ? null : acceptanceNote.Trim();
        AcceptedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public void Reject(Guid rejectingStaffId, string rejectionReason, DateTime nowUtc)
    {
        if (Status != HandoffStatus.PendingAcceptance)
        {
            throw new InvalidOperationException("Yalnızca kabul bekleyen devir teslimler reddedilebilir.");
        }

        if (rejectingStaffId == Guid.Empty)
        {
            throw new ArgumentException("Reddeden personel ID boş olamaz.", nameof(rejectingStaffId));
        }

        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("Devir teslim ret gerekçesi boş olamaz.", nameof(rejectionReason));
        }

        ReceivingStaffId = rejectingStaffId;
        Status = HandoffStatus.Rejected;
        StatusReason = rejectionReason.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(Guid cancellingStaffId, string cancelReason, DateTime nowUtc)
    {
        if (Status != HandoffStatus.PendingAcceptance)
        {
            throw new InvalidOperationException("Yalnızca kabul bekleyen devir teslimler iptal edilebilir.");
        }

        if (cancellingStaffId != HandingOverStaffId)
        {
            throw new InvalidOperationException("Devir teslimi yalnızca başlatan devreden personel iptal edebilir.");
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(cancelReason));
        }

        Status = HandoffStatus.Cancelled;
        StatusReason = cancelReason.Trim();
        UpdatedAtUtc = nowUtc;
    }
}
