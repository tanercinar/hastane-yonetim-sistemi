using System.Security.Claims;
using HospitalManagement.Modules.Inpatient.Domain;

namespace HospitalManagement.Modules.Inpatient.Application;

public interface IBedManagementService
{
    Task<InpatientOperationResult<IReadOnlyList<WardDto>>> GetWardsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<WardDto>> GetWardByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<IReadOnlyList<RoomDto>>> GetRoomsByWardIdAsync(
        Guid wardId,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<IReadOnlyList<BedDto>>> GetBedsAsync(
        Guid? wardId = null,
        Guid? roomId = null,
        BedStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<BedDto>> GetBedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<BedDto>> UpdateBedStatusAsync(
        ClaimsPrincipal actor,
        Guid bedId,
        BedStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<InpatientOperationResult<BedOccupancySummaryDto>> GetOccupancySummaryAsync(
        Guid? wardId = null,
        CancellationToken cancellationToken = default);
}
