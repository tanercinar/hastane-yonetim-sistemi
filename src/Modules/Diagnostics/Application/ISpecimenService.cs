using System.Security.Claims;
using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.Modules.Diagnostics.Application;

public interface ISpecimenService
{
    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> CollectAsync(
        ClaimsPrincipal actor,
        CollectSpecimenCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> MarkInTransitAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        TransitSpecimenCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> ReceiveAtLabAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        ReceiveSpecimenCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> RejectAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        RejectSpecimenCommand command,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> GetByBarcodeAsync(
        ClaimsPrincipal actor,
        string barcode,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<SpecimenSummaryDto>>> GetByOrderIdAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderOperationResult<IReadOnlyList<SpecimenSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        SpecimenStatus? status = null,
        string? barcode = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);
}
