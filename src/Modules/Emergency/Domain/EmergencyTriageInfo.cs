namespace HospitalManagement.Modules.Emergency.Domain;

public sealed class EmergencyTriageInfo
{
    private EmergencyTriageInfo()
    {
    }

    public EmergencyTriageInfo(
        TriageLevel triageLevel,
        string triageCategoryReason,
        DateTime triagedAtUtc,
        Guid triageNurseId,
        bool educationalClassificationAssisted,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        decimal? bodyTemperatureCelsius,
        int? respiratoryRate,
        int? oxygenSaturationPercent,
        int? painScale,
        string? consciousness,
        string? clinicalNotes)
    {
        if (string.IsNullOrWhiteSpace(triageCategoryReason))
        {
            throw new ArgumentException("Triyaj kategorilendirme gerekçesi boş olamaz.", nameof(triageCategoryReason));
        }

        if (triageNurseId == Guid.Empty)
        {
            throw new ArgumentException("Triyajı yapan personelin kimliği geçerli olmalıdır.", nameof(triageNurseId));
        }

        if (painScale.HasValue && (painScale.Value < 0 || painScale.Value > 10))
        {
            throw new ArgumentOutOfRangeException(nameof(painScale), "Ağrı skalası 0 ile 10 arasında olmalıdır.");
        }

        if (oxygenSaturationPercent.HasValue && (oxygenSaturationPercent.Value < 0 || oxygenSaturationPercent.Value > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenSaturationPercent), "Oksijen satürasyonu 0 ile 100 arasında olmalıdır.");
        }

        TriageLevel = triageLevel;
        TriageCategoryReason = triageCategoryReason.Trim();
        TriagedAtUtc = triagedAtUtc;
        TriageNurseId = triageNurseId;
        EducationalClassificationAssisted = educationalClassificationAssisted;
        SystolicBp = systolicBp;
        DiastolicBp = diastolicBp;
        HeartRate = heartRate;
        BodyTemperatureCelsius = bodyTemperatureCelsius;
        RespiratoryRate = respiratoryRate;
        OxygenSaturationPercent = oxygenSaturationPercent;
        PainScale = painScale;
        Consciousness = consciousness?.Trim();
        ClinicalNotes = clinicalNotes?.Trim();
    }

    public TriageLevel TriageLevel
    {
        get; private set;
    }
    public string TriageCategoryReason { get; private set; } = string.Empty;
    public DateTime TriagedAtUtc
    {
        get; private set;
    }
    public Guid TriageNurseId
    {
        get; private set;
    }
    public bool EducationalClassificationAssisted
    {
        get; private set;
    }
    public int? SystolicBp
    {
        get; private set;
    }
    public int? DiastolicBp
    {
        get; private set;
    }
    public int? HeartRate
    {
        get; private set;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; private set;
    }
    public int? RespiratoryRate
    {
        get; private set;
    }
    public int? OxygenSaturationPercent
    {
        get; private set;
    }
    public int? PainScale
    {
        get; private set;
    }
    public string? Consciousness
    {
        get; private set;
    }
    public string? ClinicalNotes
    {
        get; private set;
    }
}
