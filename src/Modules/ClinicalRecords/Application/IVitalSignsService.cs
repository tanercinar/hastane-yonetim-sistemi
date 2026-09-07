using System.Security.Claims;

namespace HospitalManagement.Modules.ClinicalRecords.Application;

public interface IVitalSignsService
{
    Task<ClinicalEncounterOperationResult<VitalSignObservationDto>> RecordObservationAsync(
        ClaimsPrincipal actor,
        RecordVitalSignObservationCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<VitalSignsPanelDto>> RecordPanelAsync(
        ClaimsPrincipal actor,
        RecordVitalSignsPanelCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<VitalSignObservationDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkVitalSignEnteredInErrorCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<VitalSignObservationDto>>> GetPatientObservationsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<ClinicalEncounterOperationResult<IReadOnlyList<VitalSignObservationDto>>> GetEncounterObservationsAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default);
}
