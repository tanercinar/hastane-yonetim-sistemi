using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IConsultationService
{
    Task<ClinicalEncounterOperationResult<ConsultationDto>> RequestConsultationAsync(
        ClaimsPrincipal actor,
        RequestConsultationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> AcceptConsultationAsync(
        ClaimsPrincipal actor,
        AcceptConsultationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> CompleteConsultationAsync(
        ClaimsPrincipal actor,
        CompleteConsultationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> DeclineConsultationAsync(
        ClaimsPrincipal actor,
        DeclineConsultationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> CancelConsultationAsync(
        ClaimsPrincipal actor,
        CancelConsultationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkConsultationEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<ConsultationDto>> GetConsultationByIdAsync(
        ClaimsPrincipal actor,
        Guid consultationId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetEncounterConsultationsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetDepartmentPendingConsultationsAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<ConsultationDto>>> GetPatientConsultationsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
