using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IClinicalAttachmentService
{
    Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> UploadAttachmentAsync(
        ClaimsPrincipal actor,
        UploadAttachmentCommand command,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<AttachmentFileDownloadResult>> DownloadAttachmentAsync(
        ClaimsPrincipal actor,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> GetAttachmentMetadataAsync(
        ClaimsPrincipal actor,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ClinicalAttachmentDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkAttachmentEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalAttachmentDto>>> GetEncounterAttachmentsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ClinicalAttachmentDto>>> GetPatientAttachmentsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
