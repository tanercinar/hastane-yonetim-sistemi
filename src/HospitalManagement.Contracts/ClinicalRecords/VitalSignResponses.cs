namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record VitalSignObservationResponse(
    Guid Id,
    Guid PatientId,
    Guid? EncounterId,
    string MeasurementType,
    decimal Value,
    string Unit,
    string Interpretation,
    string? MeasurementMethod,
    DateTime MeasuredAtUtc,
    string? Notes,
    Guid RecordedByPractitionerId,
    DateTime RecordedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsEnteredInError,
    string? EnteredInErrorReason,
    long Version);

public sealed record VitalSignsPanelResponse(
    Guid PatientId,
    Guid? EncounterId,
    IReadOnlyList<VitalSignObservationResponse> Observations,
    string? ConsciousnessState,
    DateTime RecordedAtUtc);
