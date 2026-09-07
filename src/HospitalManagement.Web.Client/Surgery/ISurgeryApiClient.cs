using HospitalManagement.Contracts.Surgery;

namespace HospitalManagement.Web.Client.Surgery;

public interface ISurgeryApiClient
{
    Task<List<OperatingRoomResponse>> GetOperatingRoomsAsync(
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingResponse?> CreateBookingAsync(
        CreateSurgeryBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingResponse?> RescheduleBookingAsync(
        Guid id,
        RescheduleSurgeryBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingResponse?> RecordPreOpChecklistAsync(
        Guid id,
        RecordPreOpChecklistRequest request,
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingResponse?> CancelBookingAsync(
        Guid id,
        CancelSurgeryBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<SurgeryBookingResponse?> GetBookingByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<SurgeryBookingResponse>> GetBookingsAsync(
        Guid? operatingRoomId = null,
        Guid? leadSurgeonId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordResponse?> SavePerioperativeRecordAsync(
        SavePerioperativeRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordResponse?> GetPerioperativeRecordByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordResponse?> GetPerioperativeRecordByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordResponse?> SignPerioperativeRecordAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PerioperativeCorrectionResponse?> AddPerioperativeCorrectionAsync(
        Guid id,
        AddPerioperativeCorrectionRequest request,
        CancellationToken cancellationToken = default);
}
