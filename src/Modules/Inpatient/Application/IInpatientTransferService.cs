namespace HospitalManagement.Modules.Inpatient.Application;

public interface IInpatientTransferService
{
    Task<InpatientOperationResult<InpatientTransferDto>> RequestTransferAsync(
        CreateTransferDto request,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientTransferDto>> AcceptTransferAsync(
        Guid transferId,
        AcceptTransferDto? request,
        Guid acceptedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientTransferDto>> CompleteTransferAsync(
        Guid transferId,
        CompleteTransferDto request,
        Guid completedByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<InpatientTransferDto>> CancelTransferAsync(
        Guid transferId,
        CancelTransferDto request,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default);

    Task<InpatientTransferDto?> GetTransferByIdAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    Task<List<InpatientTransferSummaryDto>> GetTransfersAsync(
        Guid? admissionId = null,
        Guid? sourceWardId = null,
        Guid? targetWardId = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}
