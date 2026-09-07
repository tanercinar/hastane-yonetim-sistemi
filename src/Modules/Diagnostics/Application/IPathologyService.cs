using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IPathologyService
{
    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> EnsureCaseForOrderItemAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        PathologySpecimenType specimenType,
        string anatomicSite,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> ReceiveSpecimenAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string fixativeUsed,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> RecordGrossExamAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string grossDescription,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> RecordMicroscopicExamAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string microscopicDescription,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> DraftReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string pathologicalDiagnosis,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> FinalizeReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string pathologicalDiagnosis,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> CorrectReportAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string correctionReason,
        string newDiagnosis,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<PathologyCaseDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid caseId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<PathologyCaseSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        PathologyCaseStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);
}
