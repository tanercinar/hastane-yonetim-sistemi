namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

public sealed class DentalProcedure
{
    private DentalProcedure()
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
    public string ProcedureProtocolNumber { get; private set; } = string.Empty;

    public int? ToothNumber
    {
        get; private set;
    }
    public ToothSurface Surfaces
    {
        get; private set;
    }
    public string ProcedureCode { get; private set; } = string.Empty;
    public string ProcedureName { get; private set; } = string.Empty;

    public DentalProcedureStatus Status
    {
        get; private set;
    }
    public decimal EstimatedCost
    {
        get; private set;
    }

    public Guid PerformedByDoctorId
    {
        get; private set;
    }
    public DateTime? ScheduledDateUtc
    {
        get; private set;
    }
    public DateTime? CompletedDateUtc
    {
        get; private set;
    }

    public string? ClinicalNotes
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

    public static DentalProcedure Plan(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        int? toothNumber,
        ToothSurface surfaces,
        string procedureCode,
        string procedureName,
        decimal estimatedCost,
        Guid performedByDoctorId,
        DateTime? scheduledDateUtc,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("İşlem ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (toothNumber.HasValue && !FdiToothValidator.IsValidToothNumber(toothNumber.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(toothNumber), $"Geçersiz FDI diş numarası: {toothNumber.Value}.");
        }

        if (string.IsNullOrWhiteSpace(procedureCode))
        {
            throw new ArgumentException("İşlem kodu boş olamaz.", nameof(procedureCode));
        }

        if (string.IsNullOrWhiteSpace(procedureName))
        {
            throw new ArgumentException("İşlem adı boş olamaz.", nameof(procedureName));
        }

        if (estimatedCost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCost), "Tahmini maliyet negatif olamaz.");
        }

        if (performedByDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Hekim ID boş olamaz.", nameof(performedByDoctorId));
        }

        var protocol = $"DEMO-DNT-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new DentalProcedure
        {
            Id = id,
            PatientId = patientId,
            EncounterId = encounterId,
            ProcedureProtocolNumber = protocol,
            ToothNumber = toothNumber,
            Surfaces = surfaces,
            ProcedureCode = procedureCode.Trim().ToUpperInvariant(),
            ProcedureName = procedureName.Trim(),
            Status = DentalProcedureStatus.Planned,
            EstimatedCost = estimatedCost,
            PerformedByDoctorId = performedByDoctorId,
            ScheduledDateUtc = scheduledDateUtc,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Complete(DateTime completedDateUtc, string? completionNotes, DateTime nowUtc)
    {
        if (Status == DentalProcedureStatus.Completed)
        {
            throw new InvalidOperationException("İşlem zaten tamamlanmış.");
        }

        if (Status == DentalProcedureStatus.Cancelled)
        {
            throw new InvalidOperationException("İptal edilmiş işlem tamamlanamaz.");
        }

        Status = DentalProcedureStatus.Completed;
        CompletedDateUtc = completedDateUtc == default ? nowUtc : completedDateUtc;
        if (!string.IsNullOrWhiteSpace(completionNotes))
        {
            ClinicalNotes = string.IsNullOrWhiteSpace(ClinicalNotes)
                ? completionNotes.Trim()
                : $"{ClinicalNotes}\n[Tamamlama Notu]: {completionNotes.Trim()}";
        }
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (Status == DentalProcedureStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış işlem iptal edilemez.");
        }

        Status = DentalProcedureStatus.Cancelled;
        ClinicalNotes = string.IsNullOrWhiteSpace(ClinicalNotes)
            ? $"[İptal Gerekçesi]: {reason.Trim()}"
            : $"{ClinicalNotes}\n[İptal Gerekçesi]: {reason.Trim()}";
        UpdatedAtUtc = nowUtc;
    }
}
