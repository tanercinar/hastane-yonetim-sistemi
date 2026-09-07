using System.Security.Claims;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IDicomSimulationService
{
    Task<DiagnosticOrderOperationResult<DicomStudyMetadataDto>> GetStudyMetadataAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DicomPreviewTokenDto>> GeneratePreviewTokenAsync(
        ClaimsPrincipal actor,
        Guid studyId,
        string sopInstanceUid,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<(byte[] Content, string ContentType)>> RenderPreviewImageAsync(
        ClaimsPrincipal actor,
        string token,
        CancellationToken cancellationToken = default);
}
