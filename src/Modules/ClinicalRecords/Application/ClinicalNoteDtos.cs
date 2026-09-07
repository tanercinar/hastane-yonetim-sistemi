using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record ClinicalNoteDto(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid AuthorPractitionerId,
    ClinicalNoteType NoteType,
    ClinicalNoteStatus Status,
    string Title,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PhysicalExamination,
    string? Assessment,
    string? Plan,
    string? Content,
    DateTime? SignedAtUtc,
    Guid? SignedByPractitionerId,
    Guid? ParentNoteId,
    string? CorrectionReason,
    string? EnteredInErrorReason,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    long Version);
