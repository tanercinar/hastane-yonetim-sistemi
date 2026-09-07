namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record ClinicalNoteResponse(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid AuthorPractitionerId,
    string NoteType,
    string Status,
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
