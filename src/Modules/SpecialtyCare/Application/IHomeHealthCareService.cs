namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface IHomeHealthCareService
{
    Task<SpecialtyOperationResult<HomeHealthVisitDto>> RequestVisitAsync(
        RequestHomeHealthVisitDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<HomeHealthVisitDto>> AssignTeamAsync(
        Guid visitId,
        Guid assignedStaffId,
        DateTime scheduledDateUtc,
        Guid actorStaffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<HomeHealthVisitDto>> StartVisitAsync(
        Guid visitId,
        Guid actorStaffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<HomeHealthVisitDto>> CompleteVisitAsync(
        Guid visitId,
        string clinicalNotes,
        string? vitalsSummaryNotes,
        Guid encounterId,
        Guid actorStaffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<HomeHealthVisitDto>> CancelVisitAsync(
        Guid visitId,
        string reason,
        Guid actorStaffId,
        CancellationToken cancellationToken = default);

    Task<HomeHealthVisitDto?> GetVisitByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<HomeHealthVisitDto>> GetActiveVisitsAsync(
        CancellationToken cancellationToken = default);

    Task<List<HomeHealthVisitDto>> GetVisitsByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}
