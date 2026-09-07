using HospitalManagement.Modules.Emergency.Domain;

namespace HospitalManagement.Modules.Emergency.Application;

public interface IEmergencyTrackingBoardService
{
    Task<EmergencyBoardSummaryDto> GetBoardSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<List<EmergencyBoardWorklistItemDto>> GetBoardWorklistAsync(
        EmergencyAdmissionStatus? status = null,
        TriageLevel? triageLevel = null,
        string? zone = null,
        CancellationToken cancellationToken = default);
}
