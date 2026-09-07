using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record GetPatientTimelineQuery(
    Guid PatientId,
    int PageNumber = 1,
    int PageSize = 20,
    IReadOnlyList<PatientTimelineEventType>? EventTypes = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    bool IncludeEnteredInError = false);
