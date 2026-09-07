using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IEncounterService
{
    Task<ClinicalEncounterOperationResult<EncounterDto>> CreateEncounterAsync(
        ClaimsPrincipal actor,
        CreateEncounterCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> StartEncounterAsync(
        ClaimsPrincipal actor,
        StartEncounterCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> CompleteEncounterAsync(
        ClaimsPrincipal actor,
        CompleteEncounterCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> ReopenEncounterAsync(
        ClaimsPrincipal actor,
        ReopenEncounterCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> CancelEncounterAsync(
        ClaimsPrincipal actor,
        CancelEncounterCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkEncounterEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> AddParticipantAsync(
        ClaimsPrincipal actor,
        AddEncounterParticipantCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> GetEncounterByIdAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<EncounterDto>>> GetPatientEncountersAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<EncounterDto>> GetActiveEncounterByAppointmentIdAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
