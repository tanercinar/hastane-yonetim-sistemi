using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record VitalSignObservationDto(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    VitalSignType MeasurementType,
    decimal Value,
    string Unit,
    VitalInterpretation Interpretation,
    string? MeasurementMethod,
    DateTime MeasuredAtUtc,
    string? Notes,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsEnteredInError,
    string? EnteredInErrorReason,
    long Version);

public sealed record VitalSignsPanelDto(
    Guid PatientId,
    Guid? EncounterId,
    IReadOnlyList<VitalSignObservationDto> Observations,
    string? ConsciousnessState,
    DateTime RecordedAtUtc);
