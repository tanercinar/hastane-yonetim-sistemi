namespace HospitalManagement.Contracts.Specialty;

public sealed class CreatePregnancyEpisodeRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public Guid OpeningEncounterId
    {
        get; set;
    }
    public int Gravida { get; set; } = 1;
    public int Para
    {
        get; set;
    }
    public int Abortus
    {
        get; set;
    }
    public int LivingChildren
    {
        get; set;
    }
    public DateTime LastMenstrualPeriodUtc
    {
        get; set;
    }
    public DateTime? EstimatedDeliveryDateUtc
    {
        get; set;
    }
    public string? BloodGroupAndRh
    {
        get; set;
    }
    public string RiskCategory { get; set; } = "LowRisk";
    public string? RiskFactorsNotes
    {
        get; set;
    }
    public Guid? AssignedDoctorId
    {
        get; set;
    }
    public Guid? AssignedMidwifeId
    {
        get; set;
    }

    public CreatePregnancyEpisodeRequest()
    {
    }

    public CreatePregnancyEpisodeRequest(
        Guid patientId,
        Guid openingEncounterId,
        int gravida,
        int para,
        int abortus,
        int livingChildren,
        DateTime lastMenstrualPeriodUtc,
        DateTime? estimatedDeliveryDateUtc,
        string? bloodGroupAndRh,
        string riskCategory,
        string? riskFactorsNotes,
        Guid? assignedDoctorId,
        Guid? assignedMidwifeId)
    {
        PatientId = patientId;
        OpeningEncounterId = openingEncounterId;
        Gravida = gravida;
        Para = para;
        Abortus = abortus;
        LivingChildren = livingChildren;
        LastMenstrualPeriodUtc = lastMenstrualPeriodUtc;
        EstimatedDeliveryDateUtc = estimatedDeliveryDateUtc;
        BloodGroupAndRh = bloodGroupAndRh;
        RiskCategory = riskCategory;
        RiskFactorsNotes = riskFactorsNotes;
        AssignedDoctorId = assignedDoctorId;
        AssignedMidwifeId = assignedMidwifeId;
    }
}

public sealed class RecordAntenatalVisitRequest
{
    public Guid EncounterId
    {
        get; set;
    }
    public DateTime VisitDateUtc
    {
        get; set;
    }
    public int GestationalAgeWeeks
    {
        get; set;
    }
    public int GestationalAgeDays
    {
        get; set;
    }
    public decimal? MaternalWeightKg
    {
        get; set;
    }
    public int? SystolicBpMmHg
    {
        get; set;
    }
    public int? DiastolicBpMmHg
    {
        get; set;
    }
    public decimal? FundalHeightCm
    {
        get; set;
    }
    public int? FetalHeartRateBpm
    {
        get; set;
    }
    public string FetalPresentation { get; set; } = "Undetermined";
    public string EdemaLevel { get; set; } = "None";
    public bool UrineProteinPresent
    {
        get; set;
    }
    public bool UrineGlucosePresent
    {
        get; set;
    }
    public string? ClinicalNotes
    {
        get; set;
    }
    public DateTime? NextVisitRecommendedDateUtc
    {
        get; set;
    }

    public RecordAntenatalVisitRequest()
    {
    }

    public RecordAntenatalVisitRequest(
        Guid encounterId,
        DateTime visitDateUtc,
        int gestationalAgeWeeks,
        int gestationalAgeDays,
        decimal? maternalWeightKg,
        int? systolicBpMmHg,
        int? diastolicBpMmHg,
        decimal? fundalHeightCm,
        int? fetalHeartRateBpm,
        string fetalPresentation,
        string edemaLevel,
        bool urineProteinPresent,
        bool urineGlucosePresent,
        string? clinicalNotes,
        DateTime? nextVisitRecommendedDateUtc)
    {
        EncounterId = encounterId;
        VisitDateUtc = visitDateUtc;
        GestationalAgeWeeks = gestationalAgeWeeks;
        GestationalAgeDays = gestationalAgeDays;
        MaternalWeightKg = maternalWeightKg;
        SystolicBpMmHg = systolicBpMmHg;
        DiastolicBpMmHg = diastolicBpMmHg;
        FundalHeightCm = fundalHeightCm;
        FetalHeartRateBpm = fetalHeartRateBpm;
        FetalPresentation = fetalPresentation;
        EdemaLevel = edemaLevel;
        UrineProteinPresent = urineProteinPresent;
        UrineGlucosePresent = urineGlucosePresent;
        ClinicalNotes = clinicalNotes;
        NextVisitRecommendedDateUtc = nextVisitRecommendedDateUtc;
    }
}

public sealed class UpdatePregnancyRiskCategoryRequest
{
    public string RiskCategory { get; set; } = "LowRisk";
    public string? RiskFactorsNotes
    {
        get; set;
    }

    public UpdatePregnancyRiskCategoryRequest()
    {
    }

    public UpdatePregnancyRiskCategoryRequest(string riskCategory, string? riskFactorsNotes)
    {
        RiskCategory = riskCategory;
        RiskFactorsNotes = riskFactorsNotes;
    }
}

public sealed class CompletePregnancyEpisodeRequest
{
    public string OutcomeStatus { get; set; } = "Delivered";

    public CompletePregnancyEpisodeRequest()
    {
    }

    public CompletePregnancyEpisodeRequest(string outcomeStatus)
    {
        OutcomeStatus = outcomeStatus;
    }
}

public sealed record AntenatalVisitResponse(
    Guid Id,
    Guid PregnancyEpisodeId,
    Guid EncounterId,
    DateTime VisitDateUtc,
    int GestationalAgeWeeks,
    int GestationalAgeDays,
    decimal? MaternalWeightKg,
    int? SystolicBpMmHg,
    int? DiastolicBpMmHg,
    decimal? FundalHeightCm,
    int? FetalHeartRateBpm,
    string FetalPresentation,
    string EdemaLevel,
    bool UrineProteinPresent,
    bool UrineGlucosePresent,
    Guid StaffId,
    string? ClinicalNotes,
    DateTime? NextVisitRecommendedDateUtc,
    DateTime CreatedAtUtc);

public sealed record PregnancyEpisodeResponse(
    Guid Id,
    Guid PatientId,
    Guid OpeningEncounterId,
    string EpisodeProtocolNumber,
    int Gravida,
    int Para,
    int Abortus,
    int LivingChildren,
    DateTime LastMenstrualPeriodUtc,
    DateTime EstimatedDeliveryDateUtc,
    string? BloodGroupAndRh,
    string RiskCategory,
    string? RiskFactorsNotes,
    string Status,
    Guid? AssignedDoctorId,
    Guid? AssignedMidwifeId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<AntenatalVisitResponse> AntenatalVisits);
