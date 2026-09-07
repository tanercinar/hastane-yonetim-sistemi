namespace HospitalManagement.Contracts.Emergency;

public sealed class CreateEmergencyAdmissionRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public string ArrivalType { get; set; } = "WalkIn";
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? AdmissionNotes
    {
        get; set;
    }
}

public sealed class RecordTriageRequest
{
    public string TriageLevel { get; set; } = "GreenStandard";
    public string TriageCategoryReason { get; set; } = string.Empty;
    public int? SystolicBp
    {
        get; set;
    }
    public int? DiastolicBp
    {
        get; set;
    }
    public int? HeartRate
    {
        get; set;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; set;
    }
    public int? RespiratoryRate
    {
        get; set;
    }
    public int? OxygenSaturationPercent
    {
        get; set;
    }
    public int? PainScale
    {
        get; set;
    }
    public string? Consciousness
    {
        get; set;
    }
    public string? ClinicalNotes
    {
        get; set;
    }
}

public sealed class AssignEmergencyDoctorRequest
{
    public Guid DoctorId
    {
        get; set;
    }
    public string? BedOrZone
    {
        get; set;
    }
}

public sealed class UpdateEmergencyAdmissionStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes
    {
        get; set;
    }
}

public sealed record EmergencyTriageResponse(
    string TriageLevel,
    string TriageCategoryReason,
    DateTime TriagedAtUtc,
    Guid TriageNurseId,
    bool EducationalClassificationAssisted,
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    decimal? BodyTemperatureCelsius,
    int? RespiratoryRate,
    int? OxygenSaturationPercent,
    int? PainScale,
    string? Consciousness,
    string? ClinicalNotes);

public sealed record EmergencyAdmissionResponse(
    Guid Id,
    string EmergencyProtocolNumber,
    Guid PatientId,
    string ArrivalType,
    string ChiefComplaint,
    string? AdmissionNotes,
    string Status,
    DateTime AdmittedAtUtc,
    Guid AdmittingStaffId,
    EmergencyTriageResponse? Triage,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone,
    DateTime? CompletedAtUtc,
    string? DischargeOrDispositionNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);

public sealed record EmergencyAdmissionSummaryResponse(
    Guid Id,
    string EmergencyProtocolNumber,
    Guid PatientId,
    string ArrivalType,
    string ChiefComplaint,
    string Status,
    string? TriageLevel,
    DateTime AdmittedAtUtc,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone);
