namespace HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

public sealed class AntenatalVisit
{
    private AntenatalVisit()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid PregnancyEpisodeId
    {
        get; private set;
    }
    public Guid EncounterId
    {
        get; private set;
    }
    public DateTime VisitDateUtc
    {
        get; private set;
    }
    public int GestationalAgeWeeks
    {
        get; private set;
    }
    public int GestationalAgeDays
    {
        get; private set;
    }

    public decimal? MaternalWeightKg
    {
        get; private set;
    }
    public int? SystolicBpMmHg
    {
        get; private set;
    }
    public int? DiastolicBpMmHg
    {
        get; private set;
    }
    public decimal? FundalHeightCm
    {
        get; private set;
    }
    public int? FetalHeartRateBpm
    {
        get; private set;
    }
    public FetalPresentation FetalPresentation
    {
        get; private set;
    }
    public EdemaLevel EdemaLevel
    {
        get; private set;
    }
    public bool UrineProteinPresent
    {
        get; private set;
    }
    public bool UrineGlucosePresent
    {
        get; private set;
    }

    public Guid StaffId
    {
        get; private set;
    }
    public string? ClinicalNotes
    {
        get; private set;
    }
    public DateTime? NextVisitRecommendedDateUtc
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static AntenatalVisit Create(
        Guid id,
        Guid pregnancyEpisodeId,
        Guid encounterId,
        DateTime visitDateUtc,
        int gestationalAgeWeeks,
        int gestationalAgeDays,
        decimal? maternalWeightKg,
        int? systolicBpMmHg,
        int? diastolicBpMmHg,
        decimal? fundalHeightCm,
        int? fetalHeartRateBpm,
        FetalPresentation fetalPresentation,
        EdemaLevel edemaLevel,
        bool urineProteinPresent,
        bool urineGlucosePresent,
        Guid staffId,
        string? clinicalNotes,
        DateTime? nextVisitRecommendedDateUtc,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Vizit ID boş olamaz.", nameof(id));
        }

        if (pregnancyEpisodeId == Guid.Empty)
        {
            throw new ArgumentException("Gebelik takip ID boş olamaz.", nameof(pregnancyEpisodeId));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma ID boş olamaz.", nameof(encounterId));
        }

        if (staffId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden personel ID boş olamaz.", nameof(staffId));
        }

        if (gestationalAgeWeeks is < 0 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(gestationalAgeWeeks), "Gebelik haftası 0-45 aralığında olmalıdır.");
        }

        if (gestationalAgeDays is < 0 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(gestationalAgeDays), "Gebelik günü 0-6 aralığında olmalıdır.");
        }

        if (fetalHeartRateBpm.HasValue && fetalHeartRateBpm.Value is < 50 or > 240)
        {
            throw new ArgumentOutOfRangeException(nameof(fetalHeartRateBpm), "Fetal kalp atımı 50-240 bpm aralığında olmalıdır.");
        }

        if (maternalWeightKg.HasValue && maternalWeightKg.Value is < 30 or > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(maternalWeightKg), "Anne kilosu geçerli fizyolojik aralıkta (30-300 kg) olmalıdır.");
        }

        return new AntenatalVisit
        {
            Id = id,
            PregnancyEpisodeId = pregnancyEpisodeId,
            EncounterId = encounterId,
            VisitDateUtc = visitDateUtc,
            GestationalAgeWeeks = gestationalAgeWeeks,
            GestationalAgeDays = gestationalAgeDays,
            MaternalWeightKg = maternalWeightKg,
            SystolicBpMmHg = systolicBpMmHg,
            DiastolicBpMmHg = diastolicBpMmHg,
            FundalHeightCm = fundalHeightCm,
            FetalHeartRateBpm = fetalHeartRateBpm,
            FetalPresentation = fetalPresentation,
            EdemaLevel = edemaLevel,
            UrineProteinPresent = urineProteinPresent,
            UrineGlucosePresent = urineGlucosePresent,
            StaffId = staffId,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            NextVisitRecommendedDateUtc = nextVisitRecommendedDateUtc,
            CreatedAtUtc = nowUtc,
        };
    }
}
