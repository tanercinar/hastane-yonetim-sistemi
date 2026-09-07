namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class PathologyCase
{
    private PathologyCase()
    {
    }

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
    public string PathologyNumber { get; private set; } = null!;
    public PathologySpecimenType SpecimenType
    {
        get; private set;
    }
    public string AnatomicSite { get; private set; } = null!;
    public string? ClinicalHistoryAndDiagnosis
    {
        get; private set;
    }
    public string? FixativeUsed
    {
        get; private set;
    }
    public PathologyCaseStatus Status
    {
        get; private set;
    }

    public DateTime? ReceivedAtUtc
    {
        get; private set;
    }
    public Guid? ReceivedByUserId
    {
        get; private set;
    }

    public string? GrossDescription
    {
        get; private set;
    }
    public DateTime? GrossExamAtUtc
    {
        get; private set;
    }
    public Guid? GrossExamByUserId
    {
        get; private set;
    }

    public string? MicroscopicDescription
    {
        get; private set;
    }
    public DateTime? MicroscopicExamAtUtc
    {
        get; private set;
    }
    public Guid? MicroscopicExamByUserId
    {
        get; private set;
    }

    public string? PathologicalDiagnosis
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
    public Guid? PathologistUserId
    {
        get; private set;
    }

    public string? CorrectionReason
    {
        get; private set;
    }
    public Guid? PreviousCaseId
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
    public int Version { get; set; } = 1;

    public static PathologyCase Create(
        Guid id,
        Guid diagnosticOrderId,
        Guid diagnosticOrderItemId,
        Guid patientId,
        string pathologyNumber,
        PathologySpecimenType specimenType,
        string anatomicSite,
        string? clinicalHistoryAndDiagnosis,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id zorunludur.", nameof(id));
        if (diagnosticOrderId == Guid.Empty)
            throw new ArgumentException("DiagnosticOrderId zorunludur.", nameof(diagnosticOrderId));
        if (diagnosticOrderItemId == Guid.Empty)
            throw new ArgumentException("DiagnosticOrderItemId zorunludur.", nameof(diagnosticOrderItemId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId zorunludur.", nameof(patientId));
        if (string.IsNullOrWhiteSpace(pathologyNumber))
            throw new ArgumentException("Patoloji numarası zorunludur.", nameof(pathologyNumber));
        if (string.IsNullOrWhiteSpace(anatomicSite))
            throw new ArgumentException("Anatomik bölge zorunludur.", nameof(anatomicSite));

        return new PathologyCase
        {
            Id = id,
            DiagnosticOrderId = diagnosticOrderId,
            DiagnosticOrderItemId = diagnosticOrderItemId,
            PatientId = patientId,
            PathologyNumber = pathologyNumber.Trim(),
            SpecimenType = specimenType,
            AnatomicSite = anatomicSite.Trim(),
            ClinicalHistoryAndDiagnosis = clinicalHistoryAndDiagnosis?.Trim(),
            Status = PathologyCaseStatus.Ordered,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void ReceiveSpecimen(Guid receiverUserId, string fixativeUsed, DateTime nowUtc)
    {
        if (Status != PathologyCaseStatus.Ordered)
        {
            throw new InvalidOperationException($"Materyal kabulü yalnızca İstem Verildi durumundayken yapılabilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(fixativeUsed))
        {
            throw new ArgumentException("Tespit solüsyonu / fiksatif bilgisi zorunludur.", nameof(fixativeUsed));
        }

        ReceivedByUserId = receiverUserId;
        FixativeUsed = fixativeUsed.Trim();
        ReceivedAtUtc = nowUtc;
        Status = PathologyCaseStatus.SpecimenReceived;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void RecordGrossExam(Guid examinerUserId, string grossDescription, DateTime nowUtc)
    {
        if (Status != PathologyCaseStatus.SpecimenReceived && Status != PathologyCaseStatus.GrossExamCompleted)
        {
            throw new InvalidOperationException($"Makroskopi incelemesi yalnızca materyal kabulünden sonra yapılabilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(grossDescription))
        {
            throw new ArgumentException("Makroskopi bulguları zorunludur.", nameof(grossDescription));
        }

        GrossExamByUserId = examinerUserId;
        GrossDescription = grossDescription.Trim();
        GrossExamAtUtc = nowUtc;
        Status = PathologyCaseStatus.GrossExamCompleted;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void RecordMicroscopicExam(Guid examinerUserId, string microscopicDescription, DateTime nowUtc)
    {
        if (Status != PathologyCaseStatus.GrossExamCompleted && Status != PathologyCaseStatus.MicroscopicExamCompleted && Status != PathologyCaseStatus.ReportDrafted)
        {
            throw new InvalidOperationException($"Mikroskopi incelemesi makroskopi tamamlandıktan sonra yapılabilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(microscopicDescription))
        {
            throw new ArgumentException("Mikroskopi bulguları zorunludur.", nameof(microscopicDescription));
        }

        MicroscopicExamByUserId = examinerUserId;
        MicroscopicDescription = microscopicDescription.Trim();
        MicroscopicExamAtUtc = nowUtc;
        Status = PathologyCaseStatus.MicroscopicExamCompleted;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void DraftReport(string pathologicalDiagnosis, DateTime nowUtc)
    {
        if (Status != PathologyCaseStatus.MicroscopicExamCompleted && Status != PathologyCaseStatus.ReportDrafted)
        {
            throw new InvalidOperationException($"Taslak rapor mikroskopi tamamlandıktan sonra oluşturulabilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(pathologicalDiagnosis))
        {
            throw new ArgumentException("Patolojik tanı metni zorunludur.", nameof(pathologicalDiagnosis));
        }

        PathologicalDiagnosis = pathologicalDiagnosis.Trim();
        ReportDraftedAtUtc = nowUtc;
        Status = PathologyCaseStatus.ReportDrafted;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void FinalizeReport(Guid pathologistUserId, string pathologicalDiagnosis, DateTime nowUtc)
    {
        if (Status is not PathologyCaseStatus.MicroscopicExamCompleted and not PathologyCaseStatus.ReportDrafted)
        {
            throw new InvalidOperationException($"Patoloji raporu yalnızca mikroskopi tamamlandıktan sonra kesinleştirilebilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(pathologicalDiagnosis))
        {
            throw new ArgumentException("Patolojik tanı metni zorunludur.", nameof(pathologicalDiagnosis));
        }

        PathologistUserId = pathologistUserId;
        PathologicalDiagnosis = pathologicalDiagnosis.Trim();
        ReportFinalizedAtUtc = nowUtc;
        Status = PathologyCaseStatus.ReportFinalized;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public static PathologyCase CreateCorrected(
        Guid newId,
        PathologyCase original,
        string correctionReason,
        string newDiagnosis,
        Guid pathologistUserId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (original.Status != PathologyCaseStatus.ReportFinalized && original.Status != PathologyCaseStatus.Corrected)
        {
            throw new InvalidOperationException("Düzeltme yalnızca kesinleşmiş bir patoloji raporu üzerine yapılabilir.");
        }

        if (string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new ArgumentException("Düzeltme gerekçesi zorunludur.", nameof(correctionReason));
        }

        if (string.IsNullOrWhiteSpace(newDiagnosis))
        {
            throw new ArgumentException("Düzeltilmiş patolojik tanı metni zorunludur.", nameof(newDiagnosis));
        }

        return new PathologyCase
        {
            Id = newId,
            DiagnosticOrderId = original.DiagnosticOrderId,
            DiagnosticOrderItemId = original.DiagnosticOrderItemId,
            PatientId = original.PatientId,
            PathologyNumber = $"{original.PathologyNumber}-CORR",
            SpecimenType = original.SpecimenType,
            AnatomicSite = original.AnatomicSite,
            ClinicalHistoryAndDiagnosis = original.ClinicalHistoryAndDiagnosis,
            FixativeUsed = original.FixativeUsed,
            ReceivedAtUtc = original.ReceivedAtUtc,
            ReceivedByUserId = original.ReceivedByUserId,
            GrossDescription = original.GrossDescription,
            GrossExamAtUtc = original.GrossExamAtUtc,
            GrossExamByUserId = original.GrossExamByUserId,
            MicroscopicDescription = original.MicroscopicDescription,
            MicroscopicExamAtUtc = original.MicroscopicExamAtUtc,
            MicroscopicExamByUserId = original.MicroscopicExamByUserId,
            PathologicalDiagnosis = newDiagnosis.Trim(),
            ReportFinalizedAtUtc = nowUtc,
            PathologistUserId = pathologistUserId,
            CorrectionReason = correctionReason.Trim(),
            PreviousCaseId = original.Id,
            Status = PathologyCaseStatus.Corrected,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == PathologyCaseStatus.ReportFinalized || Status == PathologyCaseStatus.Corrected)
        {
            throw new InvalidOperationException("Kesinleşmiş veya düzeltilmiş bir patoloji raporu iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi zorunludur.", nameof(reason));
        }

        Status = PathologyCaseStatus.Cancelled;
        CancellationReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
