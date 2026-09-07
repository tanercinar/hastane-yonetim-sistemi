namespace HospitalManagement.Contracts.Specialty;

public sealed record RecordToothConditionRequest
{
    public int ToothNumber
    {
        get; set;
    }
    public string Condition { get; set; } = "Sound";
    public int AffectedSurfaces
    {
        get; set;
    }
    public string? Notes
    {
        get; set;
    }
}

public sealed record ToothConditionResponse(
    Guid Id,
    Guid PatientId,
    int ToothNumber,
    string Condition,
    int AffectedSurfaces,
    string? Notes,
    DateTime RecordedAtUtc,
    Guid RecordedByStaffId,
    int Version);

public sealed record PlanDentalProcedureRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public int? ToothNumber
    {
        get; set;
    }
    public int Surfaces
    {
        get; set;
    }
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public decimal EstimatedCost
    {
        get; set;
    }
    public Guid PerformedByDoctorId
    {
        get; set;
    }
    public DateTime? ScheduledDateUtc
    {
        get; set;
    }
    public string? ClinicalNotes
    {
        get; set;
    }
}

public sealed record CompleteDentalProcedureRequest
{
    public DateTime CompletedDateUtc
    {
        get; set;
    }
    public string? CompletionNotes
    {
        get; set;
    }
}

public sealed record DentalProcedureResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProcedureProtocolNumber,
    int? ToothNumber,
    int Surfaces,
    string ProcedureCode,
    string ProcedureName,
    string Status,
    decimal EstimatedCost,
    Guid PerformedByDoctorId,
    DateTime? ScheduledDateUtc,
    DateTime? CompletedDateUtc,
    string? ClinicalNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateDentalExaminationRequest
{
    public Guid PatientId
    {
        get; set;
    }
    public Guid? EncounterId
    {
        get; set;
    }
    public Guid DentistId
    {
        get; set;
    }
    public DateTime ExaminationDateUtc
    {
        get; set;
    }
    public string? ChiefComplaint
    {
        get; set;
    }
    public string? DiagnosisNotes
    {
        get; set;
    }
    public string? TreatmentPlanSummary
    {
        get; set;
    }
}

public sealed record DentalExaminationResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ExaminationProtocolNumber,
    Guid DentistId,
    DateTime ExaminationDateUtc,
    string? ChiefComplaint,
    string? DiagnosisNotes,
    string? TreatmentPlanSummary,
    DateTime CreatedAtUtc);
