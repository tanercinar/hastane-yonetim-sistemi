using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public sealed record ClinicalAttachmentDto(
    Guid Id,
    Guid EncounterId,
    Guid PatientId,
    Guid UploadedByPractitionerId,
    ClinicalAttachmentType AttachmentType,
    string FileName,
    string ContentType,
    long ByteSize,
    string Sha256Checksum,
    string? Description,
    DateTime UploadedAtUtc,
    bool IsEnteredInError,
    string? EnteredInErrorReason,
    long Version);

public sealed record AttachmentFileDownloadResult(
    Stream Stream,
    string ContentType,
    string FileName,
    long ByteSize);
