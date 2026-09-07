namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class RadiologyStudy
{
    public Guid Id
    {
        get; private set;
    }
    public Guid DiagnosticOrderId
    {
        get; private set;
    }
    public Guid DiagnosticOrderItemId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public string AccessionNumber { get; private set; } = string.Empty;
    public RadiologyModality Modality
    {
        get; private set;
    }
    public string ProcedureCode { get; private set; } = string.Empty;
    public string ProcedureName { get; private set; } = string.Empty;
    public string BodySite { get; private set; } = string.Empty;
    public RadiologyStudyStatus Status
    {
        get; private set;
    }
    public DateTime? ScheduledAtUtc
    {
        get; private set;
    }
    public DateTime? PerformedAtUtc
    {
        get; private set;
    }
    public Guid? TechnicianUserId
    {
        get; private set;
    }
    public string? TechnicianNotes
    {
        get; private set;
    }
    public Guid? RadiologistUserId
    {
        get; private set;
    }
    public string? ReportText
    {
        get; private set;
    }
    public string? Impression
    {
        get; private set;
    }
    public DateTime? ReportDraftedAtUtc
    {
        get; private set;
    }
    public DateTime? ReportFinalizedAtUtc
    {
        get; private set;
    }
    public string? AddendumText
    {
        get; private set;
    }
    public DateTime? AddendumAddedAtUtc
    {
        get; private set;
    }
    public Guid? AddendumByUserId
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
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }
    public int Version { get; private set; } = 1;

    private RadiologyStudy()
    {
    }

    public static RadiologyStudy Create(
        Guid id,
        Guid diagnosticOrderId,
        Guid diagnosticOrderItemId,
        Guid patientId,
        string accessionNumber,
        RadiologyModality modality,
        string procedureCode,
        string procedureName,
        string bodySite,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Çalışma kimliği zorunludur.", nameof(id));
        }

        if (diagnosticOrderId == Guid.Empty)
        {
            throw new ArgumentException("İstem kimliği zorunludur.", nameof(diagnosticOrderId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(accessionNumber))
        {
            throw new ArgumentException("Erişim numarası (Accession Number) zorunludur.", nameof(accessionNumber));
        }

        if (string.IsNullOrWhiteSpace(procedureCode))
        {
            throw new ArgumentException("İşlem kodu zorunludur.", nameof(procedureCode));
        }

        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new ArgumentException("İşlem adı zorunludur.", nameof(procedureName));
        }

        return new RadiologyStudy
        {
            Id = id,
            DiagnosticOrderId = diagnosticOrderId,
            DiagnosticOrderItemId = diagnosticOrderItemId,
            PatientId = patientId,
            AccessionNumber = accessionNumber.Trim().ToUpperInvariant(),
            Modality = modality,
            ProcedureCode = procedureCode.Trim().ToUpperInvariant(),
            ProcedureName = procedureName.Trim(),
            BodySite = string.IsNullOrWhiteSpace(bodySite) ? "Genel" : bodySite.Trim(),
            Status = RadiologyStudyStatus.Ordered,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Schedule(DateTime scheduledAtUtc, DateTime nowUtc)
    {
        if (Status != RadiologyStudyStatus.Ordered && Status != RadiologyStudyStatus.Scheduled)
        {
            throw new InvalidOperationException($"'{Status}' durumundaki bir çekim randevulanamaz.");
        }

        ScheduledAtUtc = scheduledAtUtc;
        Status = RadiologyStudyStatus.Scheduled;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void CompleteAcquisition(Guid technicianUserId, string? technicianNotes, DateTime nowUtc)
    {
        if (technicianUserId == Guid.Empty)
        {
            throw new ArgumentException("Teknisyen kullanıcı kimliği zorunludur.", nameof(technicianUserId));
        }

        if (Status != RadiologyStudyStatus.Scheduled)
        {
            throw new InvalidOperationException($"Çekim yalnızca randevulanmış bir çalışma için tamamlanabilir. Mevcut durum: {Status}");
        }

        TechnicianUserId = technicianUserId;
        TechnicianNotes = technicianNotes?.Trim();
        PerformedAtUtc = nowUtc;
        Status = RadiologyStudyStatus.Acquired;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void DraftReport(Guid radiologistUserId, string reportText, string? impression, DateTime nowUtc)
    {
        if (radiologistUserId == Guid.Empty)
        {
            throw new ArgumentException("Radyolog kullanıcı kimliği zorunludur.", nameof(radiologistUserId));
        }

        if (string.IsNullOrWhiteSpace(reportText))
        {
            throw new ArgumentException("Rapor metni boş olamaz.", nameof(reportText));
        }

        if (Status is not RadiologyStudyStatus.Acquired and not RadiologyStudyStatus.ReportDrafted)
        {
            throw new InvalidOperationException($"Rapor taslağı yalnızca çekim tamamlandıktan sonra düzenlenebilir. Mevcut durum: {Status}");
        }

        RadiologistUserId = radiologistUserId;
        ReportText = reportText.Trim();
        Impression = impression?.Trim();
        ReportDraftedAtUtc = nowUtc;
        Status = RadiologyStudyStatus.ReportDrafted;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void FinalizeReport(Guid radiologistUserId, string reportText, string impression, DateTime nowUtc)
    {
        if (radiologistUserId == Guid.Empty)
        {
            throw new ArgumentException("Radyolog kullanıcı kimliği zorunludur.", nameof(radiologistUserId));
        }

        if (string.IsNullOrWhiteSpace(reportText))
        {
            throw new ArgumentException("Rapor metni boş olamaz.", nameof(reportText));
        }

        if (string.IsNullOrWhiteSpace(impression))
        {
            throw new ArgumentException("Klinik sonuç / kanaat (Impression) zorunludur.", nameof(impression));
        }

        if (Status is not RadiologyStudyStatus.Acquired and not RadiologyStudyStatus.ReportDrafted)
        {
            throw new InvalidOperationException($"Rapor yalnızca çekim tamamlandıktan sonra kesinleştirilebilir. Mevcut durum: {Status}");
        }

        RadiologistUserId = radiologistUserId;
        ReportText = reportText.Trim();
        Impression = impression.Trim();
        ReportFinalizedAtUtc = nowUtc;
        Status = RadiologyStudyStatus.ReportFinalized;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void AddAddendum(Guid radiologistUserId, string addendumText, DateTime nowUtc)
    {
        if (radiologistUserId == Guid.Empty)
        {
            throw new ArgumentException("Ek raporu yazan radyolog kimliği zorunludur.", nameof(radiologistUserId));
        }

        if (string.IsNullOrWhiteSpace(addendumText))
        {
            throw new ArgumentException("Ek rapor metni boş olamaz.", nameof(addendumText));
        }

        if (Status != RadiologyStudyStatus.ReportFinalized && Status != RadiologyStudyStatus.AddendumAdded)
        {
            throw new InvalidOperationException("Ek rapor (addendum) yalnızca kesinleşmiş raporlara eklenebilir.");
        }

        var newEntry = $"[{nowUtc:yyyy-MM-dd HH:mm:ss} UTC - Ek Not]: {addendumText.Trim()}";
        AddendumText = string.IsNullOrWhiteSpace(AddendumText)
            ? newEntry
            : $"{AddendumText}\n\n{newEntry}";

        AddendumByUserId = radiologistUserId;
        AddendumAddedAtUtc = nowUtc;
        Status = RadiologyStudyStatus.AddendumAdded;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi zorunludur.", nameof(reason));
        }

        if (Status == RadiologyStudyStatus.ReportFinalized || Status == RadiologyStudyStatus.AddendumAdded)
        {
            throw new InvalidOperationException("Kesinleşmiş veya onaylanmış bir radyoloji çekimi iptal edilemez.");
        }

        Status = RadiologyStudyStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
