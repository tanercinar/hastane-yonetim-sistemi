using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record RecordVitalSignObservationCommand(
    Guid PatientId,
    Guid? EncounterId,
    VitalSignType MeasurementType,
    decimal Value,
    string Unit,
    string? MeasurementMethod,
    DateTime? MeasuredAtUtc,
    string? Notes);

public sealed record RecordVitalSignsPanelCommand(
    Guid PatientId,
    Guid? EncounterId,
    decimal? TemperatureCelsius,
    int? SystolicBloodPressureMmHg,
    int? DiastolicBloodPressureMmHg,
    int? HeartRateBpm,
    int? RespiratoryRatePerMin,
    decimal? OxygenSaturationPercent,
    decimal? BodyWeightKg,
    decimal? BodyHeightCm,
    decimal? BloodGlucoseMgDl,
    int? PainScore,
    string? ConsciousnessState,
    string? Notes);

public sealed record MarkVitalSignEnteredInErrorCommand(
    Guid ObservationId,
    long ExpectedVersion,
    string Reason);
