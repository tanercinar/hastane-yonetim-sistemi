namespace HospitalManagement.Modules.SurgeryCriticalCare.Application;

public interface IPerioperativeRecordService
{
    Task<SurgeryOperationResult<PerioperativeRecordDto>> SaveRecordAsync(
        SavePerioperativeRecordDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<PerioperativeRecordDto>> SignRecordAsync(
        Guid recordId,
        Guid signingDoctorId,
        CancellationToken cancellationToken = default);

    Task<SurgeryOperationResult<PerioperativeCorrectionDto>> AddCorrectionAsync(
        Guid recordId,
        AddPerioperativeCorrectionDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordDto?> GetByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task<PerioperativeRecordDto?> GetByIdAsync(
        Guid recordId,
        CancellationToken cancellationToken = default);
}
