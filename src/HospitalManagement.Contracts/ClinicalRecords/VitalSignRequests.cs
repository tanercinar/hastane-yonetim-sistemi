namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record CreateVitalSignObservationRequest
{
    public Guid PatientId
    {
        get; init;
    }

    public Guid? EncounterId
    {
        get; init;
    }

    public string MeasurementType { get; init; } = "HeartRate";

    public decimal Value
    {
        get; init;
    }

    public string Unit { get; init; } = "bpm";

    public string? MeasurementMethod
    {
        get; init;
    }

    public DateTime? MeasuredAtUtc
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record RecordVitalSignsPanelRequest
{
    public Guid PatientId
    {
        get; init;
    }

    public Guid? EncounterId
    {
        get; init;
    }

    public decimal? TemperatureCelsius
    {
        get; init;
    }

    public int? SystolicBloodPressureMmHg
    {
        get; init;
    }

    public int? DiastolicBloodPressureMmHg
    {
        get; init;
    }

    public int? HeartRateBpm
    {
        get; init;
    }

    public int? RespiratoryRatePerMin
    {
        get; init;
    }

    public decimal? OxygenSaturationPercent
    {
        get; init;
    }

    public decimal? BodyWeightKg
    {
        get; init;
    }

    public decimal? BodyHeightCm
    {
        get; init;
    }

    public decimal? BloodGlucoseMgDl
    {
        get; init;
    }

    public int? PainScore
    {
        get; init;
    }

    public string? ConsciousnessState
    {
        get; init;
    }

    public string? Notes
    {
        get; init;
    }
}

public sealed record MarkVitalSignEnteredInErrorRequest
{
    public long ExpectedVersion
    {
        get; init;
    }

    public string Reason { get; init; } = "Hatalı ölçüm / giriş";
}
