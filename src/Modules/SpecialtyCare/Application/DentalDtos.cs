using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

namespace HospitalManagement.Modules.SpecialtyCare.Application;

public sealed record RecordToothConditionDto(
    Guid PatientId,
    int ToothNumber,
    ToothCondition Condition,
    ToothSurface AffectedSurfaces,
    string? Notes);

public sealed record ToothConditionDto(
    Guid Id,
    Guid PatientId,
    int ToothNumber,
    ToothCondition Condition,
    ToothSurface AffectedSurfaces,
    string? Notes,
    DateTime RecordedAtUtc,
    Guid RecordedByStaffId,
    int Version);

public sealed record PlanDentalProcedureDto(
    Guid PatientId,
    Guid? EncounterId,
    int? ToothNumber,
    ToothSurface Surfaces,
    string ProcedureCode,
    string ProcedureName,
    decimal EstimatedCost,
    Guid PerformedByDoctorId,
    DateTime? ScheduledDateUtc,
    string? ClinicalNotes);

public sealed record DentalProcedureDto(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string ProcedureProtocolNumber,
    int? ToothNumber,
    ToothSurface Surfaces,
    string ProcedureCode,
    string ProcedureName,
    DentalProcedureStatus Status,
    decimal EstimatedCost,
    Guid PerformedByDoctorId,
    DateTime? ScheduledDateUtc,
    DateTime? CompletedDateUtc,
    string? ClinicalNotes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateDentalExaminationDto(
    Guid PatientId,
    Guid? EncounterId,
    Guid DentistId,
    DateTime ExaminationDateUtc,
    string? ChiefComplaint,
    string? DiagnosisNotes,
    string? TreatmentPlanSummary);

public sealed record DentalExaminationDto(
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
