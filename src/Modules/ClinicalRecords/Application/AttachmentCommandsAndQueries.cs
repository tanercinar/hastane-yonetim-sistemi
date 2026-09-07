using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record UploadAttachmentCommand(
    Guid EncounterId,
    Guid PatientId,
    ClinicalAttachmentType AttachmentType,
    string RawFileName,
    string DeclaredContentType,
    string? Description);

public sealed record MarkAttachmentEnteredInErrorCommand(
    Guid AttachmentId,
    long ExpectedVersion,
    string Reason);
