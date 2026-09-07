namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed record PatientTimelineItem(
    Guid EventId,
    PatientTimelineEventType EventType,
    DateTime TimestampUtc,
    string Title,
    string Summary,
    Guid? EncounterId,
    string? Badge,
    string? Severity,
    bool IsEnteredInError);
