using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record PatientTimelineItemDto(
    Guid EventId,
    PatientTimelineEventType EventType,
    DateTime TimestampUtc,
    string Title,
    string Summary,
    Guid? EncounterId,
    string? Badge,
    string? Severity,
    bool IsEnteredInError);

public sealed record PatientTimelinePagedDto(
    Guid PatientId,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage,
    IReadOnlyList<PatientTimelineItemDto> Items);
