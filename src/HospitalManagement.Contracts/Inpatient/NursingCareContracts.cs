namespace HospitalManagement.Contracts.Inpatient;

public sealed record NursingObservationResponse(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid RecordedByNurseId,
    DateTime ObservedAtUtc,
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    int? RespiratoryRate,
    decimal? BodyTemperatureCelsius,
    int? OxygenSaturationPercent,
    int? PainScale,
    int? OralIntakeMl,
    int? IvIntakeMl,
    int? UrineOutputMl,
    int? DrainOutputMl,
    int? OtherOutputMl,
    string Consciousness,
    string? ClinicalNotes,
    bool IsCorrection,
    Guid? CorrectedObservationId,
    string? CorrectionReason,
    DateTime CreatedAtUtc,
    int Version);

public sealed record RecordObservationRequest
{
    public Guid AdmissionId
    {
        get; init;
    }
    public DateTime? ObservedAtUtc
    {
        get; init;
    }
    public int? SystolicBp
    {
        get; init;
    }
    public int? DiastolicBp
    {
        get; init;
    }
    public int? HeartRate
    {
        get; init;
    }
    public int? RespiratoryRate
    {
        get; init;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; init;
    }
    public int? OxygenSaturationPercent
    {
        get; init;
    }
    public int? PainScale
    {
        get; init;
    }
    public int? OralIntakeMl
    {
        get; init;
    }
    public int? IvIntakeMl
    {
        get; init;
    }
    public int? UrineOutputMl
    {
        get; init;
    }
    public int? DrainOutputMl
    {
        get; init;
    }
    public int? OtherOutputMl
    {
        get; init;
    }
    public string Consciousness { get; init; } = "Alert";
    public string? ClinicalNotes
    {
        get; init;
    }
}

public sealed record CorrectObservationRequest
{
    public string CorrectionReason { get; init; } = string.Empty;
    public int? SystolicBp
    {
        get; init;
    }
    public int? DiastolicBp
    {
        get; init;
    }
    public int? HeartRate
    {
        get; init;
    }
    public int? RespiratoryRate
    {
        get; init;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; init;
    }
    public int? OxygenSaturationPercent
    {
        get; init;
    }
    public int? PainScale
    {
        get; init;
    }
    public int? OralIntakeMl
    {
        get; init;
    }
    public int? IvIntakeMl
    {
        get; init;
    }
    public int? UrineOutputMl
    {
        get; init;
    }
    public int? DrainOutputMl
    {
        get; init;
    }
    public int? OtherOutputMl
    {
        get; init;
    }
    public string Consciousness { get; init; } = "Alert";
    public string? ClinicalNotes
    {
        get; init;
    }
}

public sealed record NursingCareTaskResponse(
    Guid Id,
    Guid CarePlanId,
    string Title,
    string Frequency,
    DateTime DueTimeUtc,
    string Status,
    Guid? CompletedByNurseId,
    DateTime? CompletedAtUtc,
    string? CompletionNotes,
    Guid? CancelledByNurseId,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    int Version);

public sealed record NursingCarePlanResponse(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid CreatedByNurseId,
    string NursingDiagnosis,
    string Goal,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes,
    int Version,
    List<NursingCareTaskResponse> Tasks);

public sealed record CreateCarePlanRequest
{
    public Guid AdmissionId
    {
        get; init;
    }
    public string NursingDiagnosis { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
}

public sealed record AddCareTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string Frequency { get; init; } = "Daily";
    public DateTime DueTimeUtc
    {
        get; init;
    }
}

public sealed record CompleteCareTaskRequest
{
    public string? Notes
    {
        get; init;
    }
}

public sealed record CancelCareTaskRequest
{
    public string Reason { get; init; } = string.Empty;
}
