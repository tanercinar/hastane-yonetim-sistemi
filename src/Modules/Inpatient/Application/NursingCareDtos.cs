using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public sealed record NursingObservationDto(
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
    ConsciousnessLevel Consciousness,
    string? ClinicalNotes,
    bool IsCorrection,
    Guid? CorrectedObservationId,
    string? CorrectionReason,
    DateTime CreatedAtUtc,
    int Version);

public sealed record RecordObservationDto(
    Guid AdmissionId,
    DateTime? ObservedAtUtc,
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
    ConsciousnessLevel Consciousness,
    string? ClinicalNotes);

public sealed record CorrectObservationDto(
    string CorrectionReason,
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
    ConsciousnessLevel Consciousness,
    string? ClinicalNotes);

public sealed record NursingCareTaskDto(
    Guid Id,
    Guid CarePlanId,
    string Title,
    string Frequency,
    DateTime DueTimeUtc,
    CareTaskStatus Status,
    Guid? CompletedByNurseId,
    DateTime? CompletedAtUtc,
    string? CompletionNotes,
    Guid? CancelledByNurseId,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    int Version);

public sealed record NursingCarePlanDto(
    Guid Id,
    Guid AdmissionId,
    Guid PatientId,
    Guid CreatedByNurseId,
    string NursingDiagnosis,
    string Goal,
    CarePlanStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? ResolutionNotes,
    int Version,
    List<NursingCareTaskDto> Tasks);

public sealed record CreateCarePlanDto(
    Guid AdmissionId,
    string NursingDiagnosis,
    string Goal);

public sealed record AddCareTaskDto(
    string Title,
    string Frequency,
    DateTime DueTimeUtc);

public sealed record CompleteCareTaskDto(
    string? Notes);

public sealed record CancelCareTaskDto(
    string Reason);
