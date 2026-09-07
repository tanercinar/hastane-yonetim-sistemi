namespace HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

public sealed class PregnancyEpisode
{
    private readonly List<AntenatalVisit> _antenatalVisits = [];

    private PregnancyEpisode()
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
    public Guid OpeningEncounterId
    {
        get; private set;
    }
    public string EpisodeProtocolNumber { get; private set; } = string.Empty;

    public int Gravida
    {
        get; private set;
    }
    public int Para
    {
        get; private set;
    }
    public int Abortus
    {
        get; private set;
    }
    public int LivingChildren
    {
        get; private set;
    }

    public DateTime LastMenstrualPeriodUtc
    {
        get; private set;
    }
    public DateTime EstimatedDeliveryDateUtc
    {
        get; private set;
    }
    public string? BloodGroupAndRh
    {
        get; private set;
    }

    public PregnancyRiskCategory RiskCategory
    {
        get; private set;
    }
    public string? RiskFactorsNotes
    {
        get; private set;
    }
    public PregnancyEpisodeStatus Status
    {
        get; private set;
    }

    public Guid? AssignedDoctorId
    {
        get; private set;
    }
    public Guid? AssignedMidwifeId
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

    public IReadOnlyCollection<AntenatalVisit> AntenatalVisits => _antenatalVisits.AsReadOnly();

    public static PregnancyEpisode Create(
        Guid id,
        Guid patientId,
        Guid openingEncounterId,
        int gravida,
        int para,
        int abortus,
        int livingChildren,
        DateTime lastMenstrualPeriodUtc,
        DateTime? customEstimatedDeliveryDateUtc,
        string? bloodGroupAndRh,
        PregnancyRiskCategory riskCategory,
        string? riskFactorsNotes,
        Guid? assignedDoctorId,
        Guid? assignedMidwifeId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Takip ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (openingEncounterId == Guid.Empty)
        {
            throw new ArgumentException("Başlangıç karşılaşma ID boş olamaz.", nameof(openingEncounterId));
        }

        if (gravida < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(gravida), "Gravida en az 1 olmalıdır.");
        }

        if (para < 0 || abortus < 0 || livingChildren < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(para), "Obstetrik öykü sayaçları negatif olamaz.");
        }

        // Naegele rule: LMP + 280 days (40 weeks) if not explicitly overridden by ultrasound
        var edd = customEstimatedDeliveryDateUtc ?? lastMenstrualPeriodUtc.AddDays(280);

        var protocol = $"DEMO-OBS-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new PregnancyEpisode
        {
            Id = id,
            PatientId = patientId,
            OpeningEncounterId = openingEncounterId,
            EpisodeProtocolNumber = protocol,
            Gravida = gravida,
            Para = para,
            Abortus = abortus,
            LivingChildren = livingChildren,
            LastMenstrualPeriodUtc = lastMenstrualPeriodUtc,
            EstimatedDeliveryDateUtc = edd,
            BloodGroupAndRh = string.IsNullOrWhiteSpace(bloodGroupAndRh) ? null : bloodGroupAndRh.Trim(),
            RiskCategory = riskCategory,
            RiskFactorsNotes = string.IsNullOrWhiteSpace(riskFactorsNotes) ? null : riskFactorsNotes.Trim(),
            Status = PregnancyEpisodeStatus.Active,
            AssignedDoctorId = assignedDoctorId,
            AssignedMidwifeId = assignedMidwifeId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public AntenatalVisit RecordAntenatalVisit(
        Guid visitId,
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
        if (Status != PregnancyEpisodeStatus.Active)
        {
            throw new InvalidOperationException("Yalnızca aktif gebelik takiplerine antenatal vizit eklenebilir.");
        }

        var visit = AntenatalVisit.Create(
            visitId,
            Id,
            encounterId,
            visitDateUtc,
            gestationalAgeWeeks,
            gestationalAgeDays,
            maternalWeightKg,
            systolicBpMmHg,
            diastolicBpMmHg,
            fundalHeightCm,
            fetalHeartRateBpm,
            fetalPresentation,
            edemaLevel,
            urineProteinPresent,
            urineGlucosePresent,
            staffId,
            clinicalNotes,
            nextVisitRecommendedDateUtc,
            nowUtc);

        _antenatalVisits.Add(visit);
        UpdatedAtUtc = nowUtc;
        return visit;
    }

    public void UpdateRiskCategory(
        PregnancyRiskCategory newRiskCategory,
        string? riskNotes,
        DateTime nowUtc)
    {
        RiskCategory = newRiskCategory;
        RiskFactorsNotes = string.IsNullOrWhiteSpace(riskNotes) ? null : riskNotes.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void CompleteEpisode(PregnancyEpisodeStatus outcomeStatus, DateTime nowUtc)
    {
        if (Status != PregnancyEpisodeStatus.Active)
        {
            throw new InvalidOperationException("Gebelik takibi zaten tamamlanmış durumdadır.");
        }

        if (outcomeStatus == PregnancyEpisodeStatus.Active)
        {
            throw new ArgumentException("Kapanış statüsü aktif olamaz.", nameof(outcomeStatus));
        }

        Status = outcomeStatus;
        UpdatedAtUtc = nowUtc;
    }
}
