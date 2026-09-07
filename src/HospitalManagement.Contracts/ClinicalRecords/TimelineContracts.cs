namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record PatientTimelineItemResponse(
    Guid EventId,
    string EventType,
    DateTime TimestampUtc,
    string Title,
    string Summary,
    Guid? EncounterId,
    string? Badge,
    string? Severity,
    bool IsEnteredInError);

public sealed record PatientTimelinePagedResponse(
    Guid PatientId,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<PatientTimelineItemResponse> Items);
