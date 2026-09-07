using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public interface IClinicalHandoffService
{
    Task<SurgeryOperationResult<ClinicalHandoffDto>> InitiateHandoffAsync(
        InitiateClinicalHandoffDto dto,
        Guid handingOverStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<ClinicalHandoffDto>> AcceptHandoffAsync(
        Guid handoffId,
        Guid receivingStaffId,
        string? acceptanceNote,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<ClinicalHandoffDto>> RejectHandoffAsync(
        Guid handoffId,
        Guid rejectingStaffId,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<ClinicalHandoffDto>> CancelHandoffAsync(
        Guid handoffId,
        Guid cancellingStaffId,
        string cancelReason,
        CancellationToken cancellationToken = default);

    Task<ClinicalHandoffDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<ClinicalHandoffDto>> GetPendingHandoffsAsync(
        ClinicalAreaType? destinationArea = null,
        CancellationToken cancellationToken = default);

    Task<List<ClinicalHandoffDto>> GetHandoffsByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
