using System.Security.Claims;
using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IDiagnosticOrderService
{
    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreateDiagnosticOrderDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> UpdateDraftAsync(
        ClaimsPrincipal actor,
        UpdateDiagnosticOrderDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> PlaceAsync(
        ClaimsPrincipal actor,
        PlaceDiagnosticOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        CancelDiagnosticOrderCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkDiagnosticOrderEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetByEncounterAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetByPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        DiagnosticOrderType? orderType = null,
        DiagnosticOrderStatus? status = null,
        string? orderNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);
}
