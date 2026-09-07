namespace HospitalManagement.Contracts.Surgery;

public sealed class CreateIcuFlowsheetEntryRequest
{
    public DateTime? RecordedAtUtc
    {
        get; set;
    }

    // Vital Signs
    public int? HeartRateBpm
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
    public int? RespiratoryRateBpm
    {
        get; set;
    }
    public decimal? OxygenSaturationPct
    {
        get; set;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; set;
    }

    // Clinical Scores
    public int? GlasgowComaScale
    {
        get; set;
    }
    public int? RichmondAgitationSedationScale
    {
        get; set;
    }

    // Ventilator
    public string VentilationMode { get; set; } = "NoneSpontaneous";
    public int? FractionOfInspiredOxygenPct
    {
        get; set;
    }
    public int? PositiveEndExpiratoryPressure
    {
        get; set;
    }
    public int? TidalVolumeMl
    {
        get; set;
    }
    public int? PeakInspiratoryPressure
    {
        get; set;
    }

    // Fluid Balance (I&O ml)
    public int? IvFluidIntakeMl
    {
        get; set;
    }
    public int? EnteralNutritionIntakeMl
    {
        get; set;
    }
    public int? UrineOutputMl
    {
        get; set;
    }
    public int? DrainOutputMl
    {
        get; set;
    }

    public string? ClinicalNotes
    {
        get; set;
    }
}

public sealed record IcuFlowsheetEntryResponse(
    Guid Id,
    Guid IcuAdmissionId,
    DateTime RecordedAtUtc,
    Guid RecordedByStaffId,
    int? HeartRateBpm,
    int? SystolicBpMmHg,
    int? DiastolicBpMmHg,
    int? MeanArterialPressureMmHg,
    int? RespiratoryRateBpm,
    decimal? OxygenSaturationPct,
    decimal? BodyTemperatureCelsius,
    int? GlasgowComaScale,
    int? RichmondAgitationSedationScale,
    string VentilationMode,
    int? FractionOfInspiredOxygenPct,
    int? PositiveEndExpiratoryPressure,
    int? TidalVolumeMl,
    int? PeakInspiratoryPressure,
    int? IvFluidIntakeMl,
    int? EnteralNutritionIntakeMl,
    int? UrineOutputMl,
    int? DrainOutputMl,
    int TotalIntakeMl,
    int TotalOutputMl,
    int NetFluidBalanceMl,
    string? ClinicalNotes,
    DateTime CreatedAtUtc);

public sealed record IcuFluidBalanceSummaryResponse(
    Guid IcuAdmissionId,
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalIvIntakeMl,
    int TotalEnteralIntakeMl,
    int TotalIntakeMl,
    int TotalUrineOutputMl,
    int TotalDrainOutputMl,
    int TotalOutputMl,
    int NetBalanceMl,
    int EntryCount);
