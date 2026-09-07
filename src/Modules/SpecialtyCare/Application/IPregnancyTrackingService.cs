using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface IPregnancyTrackingService
{
    Task<SpecialtyOperationResult<PregnancyEpisodeDto>> CreateEpisodeAsync(
        CreatePregnancyEpisodeDto dto,
        Guid creatingStaffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<AntenatalVisitDto>> RecordAntenatalVisitAsync(
        RecordAntenatalVisitDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<PregnancyEpisodeDto>> UpdateRiskCategoryAsync(
        Guid episodeId,
        PregnancyRiskCategory newRiskCategory,
        string? riskNotes,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<PregnancyEpisodeDto>> CompleteEpisodeAsync(
        Guid episodeId,
        PregnancyEpisodeStatus outcomeStatus,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<PregnancyEpisodeDto?> GetEpisodeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<PregnancyEpisodeDto>> GetEpisodesByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<PregnancyEpisodeDto>> GetActiveEpisodesAsync(
        CancellationToken cancellationToken = default);
}
