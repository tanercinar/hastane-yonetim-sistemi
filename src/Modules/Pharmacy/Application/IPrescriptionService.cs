using System.Security.Claims;

namespace HospitalManagement.Modules.Pharmacy.Application;

public interface IPrescriptionService
{
    Task<PrescriptionOperationResult<PrescriptionDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreatePrescriptionDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> UpdateDraftAsync(
        ClaimsPrincipal actor,
        UpdatePrescriptionDraftCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> SignAsync(
        ClaimsPrincipal actor,
        SignPrescriptionCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        CancelPrescriptionCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkPrescriptionEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> DispenseAsync(
        ClaimsPrincipal actor,
        DispensePrescriptionCommand command,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<PrescriptionDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByEncounterAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetByPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<PrescriptionOperationResult<IReadOnlyList<PrescriptionSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        string? status = null,
        string? prescriptionNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);
}
