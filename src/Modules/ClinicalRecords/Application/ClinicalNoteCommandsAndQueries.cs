using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record CreateDraftNoteCommand(
    Guid EncounterId,
    Guid PatientId,
    ClinicalNoteType NoteType,
    string Title,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PhysicalExamination,
    string? Assessment,
    string? Plan,
    string? Content);

public sealed record UpdateDraftNoteCommand(
    Guid NoteId,
    long ExpectedVersion,
    string Title,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PhysicalExamination,
    string? Assessment,
    string? Plan,
    string? Content);

public sealed record SignClinicalNoteCommand(
    Guid NoteId,
    long ExpectedVersion,
    string? SignatureNote);

public sealed record AddClinicalNoteAddendumCommand(
    Guid NoteId,
    long ExpectedVersion,
    string AddendumContent,
    string Reason);

public sealed record MarkClinicalNoteEnteredInErrorCommand(
    Guid NoteId,
    long ExpectedVersion,
    string Reason);
