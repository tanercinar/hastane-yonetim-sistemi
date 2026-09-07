using HospitalManagement.Modules.SurgeryCriticalCare.Domain;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public interface ISurgeryPlanningService
{
    Task<List<OperatingRoomDto>> GetOperatingRoomsAsync(
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<SurgeryBookingDto>> CreateBookingAsync(
        CreateSurgeryBookingDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<SurgeryBookingDto>> RescheduleBookingAsync(
        Guid bookingId,
        RescheduleSurgeryBookingDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<SurgeryBookingDto>> RecordPreOpChecklistAsync(
        Guid bookingId,
        RecordPreOpChecklistDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<SurgeryBookingDto>> CancelBookingAsync(
        Guid bookingId,
        string reason,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingDto?> GetBookingByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<List<SurgeryBookingDto>> GetBookingsAsync(
        Guid? operatingRoomId = null,
        Guid? leadSurgeonId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        SurgeryBookingStatus? status = null,
        CancellationToken cancellationToken = default);
}
