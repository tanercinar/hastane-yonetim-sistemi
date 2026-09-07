namespace HospitalManagement.Contracts.ClinicalRecords;

public sealed record ClinicalAttachmentResponse(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid UploadedByPractitionerId,
    string AttachmentType,
    string FileName,
    string ContentType,
    long ByteSize,
    string Sha256Checksum,
    string? Description,
    DateTime UploadedAtUtc,
    bool IsEnteredInError,
    string? EnteredInErrorReason,
    long Version);
