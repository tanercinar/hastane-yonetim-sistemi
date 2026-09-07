using System.Security.Claims;

using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface IBloodBankService
{
    Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> CreateCrossmatchRequestAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        BloodGroup patientBloodGroup,
        BloodProductType requestedProductType,
        int unitsRequested,
        DateTime? requiredByUtc,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> PerformCrossmatchTestAsync(
        ClaimsPrincipal actor,
        Guid crossmatchRequestId,
        Guid bloodUnitId,
        string? technicianNotes,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<BloodUnitDto>> IssueBloodUnitAsync(
        ClaimsPrincipal actor,
        Guid bloodUnitId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<BloodUnitDto>> RecordTransfusionAsync(
        ClaimsPrincipal actor,
        Guid bloodUnitId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<BloodUnitDto>>> GetInventoryAsync(
        ClaimsPrincipal actor,
        BloodProductType? productType = null,
        BloodGroup? bloodGroup = null,
        BloodUnitStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<BloodInventorySummaryDto>> GetInventorySummaryAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<CrossmatchSummaryDto>>> GetCrossmatchWorklistAsync(
        ClaimsPrincipal actor,
        CrossmatchStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> GetCrossmatchByIdAsync(
        ClaimsPrincipal actor,
        Guid crossmatchId,
        CancellationToken cancellationToken = default);
}
