using HospitalManagement.Modules.Emergency.Domain;

namespace HospitalManagement.Modules.Emergency.Application;

public sealed record CreateEmergencyAdmissionDto
{
    public Guid PatientId
    {
        get; init;
    }
    public EmergencyArrivalType ArrivalType { get; init; } = EmergencyArrivalType.WalkIn;
    public string ChiefComplaint { get; init; } = string.Empty;
    public string? AdmissionNotes
    {
        get; init;
    }
}

public sealed record RecordTriageDto
{
    public TriageLevel TriageLevel { get; init; } = TriageLevel.GreenStandard;
    public string TriageCategoryReason { get; init; } = string.Empty;
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
    public decimal? BodyTemperatureCelsius
    {
        get; init;
    }
    public int? RespiratoryRate
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
    public string? Consciousness
    {
        get; init;
    }
    public string? ClinicalNotes
    {
        get; init;
    }
}

public sealed record AssignDoctorDto
{
    public Guid DoctorId
    {
        get; init;
    }
    public string? BedOrZone
    {
        get; init;
    }
}

public sealed record UpdateEmergencyStatusDto
{
    public EmergencyAdmissionStatus Status
    {
        get; init;
    }
    public string? Notes
    {
        get; init;
    }
}

public sealed record EmergencyTriageDto(
    TriageLevel TriageLevel,
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

public sealed record EmergencyAdmissionDto(
    Guid Id,
    string EmergencyProtocolNumber,
    Guid PatientId,
    EmergencyArrivalType ArrivalType,
    string ChiefComplaint,
    string? AdmissionNotes,
    EmergencyAdmissionStatus Status,
    DateTime AdmittedAtUtc,
    Guid AdmittingStaffId,
    EmergencyTriageDto? Triage,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone,
    DateTime? CompletedAtUtc,
    string? DischargeOrDispositionNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version);

public sealed record EmergencyAdmissionSummaryDto(
    Guid Id,
    string EmergencyProtocolNumber,
    Guid PatientId,
    EmergencyArrivalType ArrivalType,
    string ChiefComplaint,
    EmergencyAdmissionStatus Status,
    TriageLevel? TriageLevel,
    DateTime AdmittedAtUtc,
    Guid? AssignedDoctorId,
    string? AssignedBedOrZone);
