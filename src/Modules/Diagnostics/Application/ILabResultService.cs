using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface ILabResultService
{
    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreateDraftLabResultCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> UpdateItemsAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        UpdateLabResultItemsCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> ApproveTechnicallyAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> ApproveClinicallyAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> CorrectAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CorrectLabResultCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<LabResultDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid resultId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<LabResultDetailDto>>> GetByOrderIdAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<LabResultSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        LabResultStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);
}
