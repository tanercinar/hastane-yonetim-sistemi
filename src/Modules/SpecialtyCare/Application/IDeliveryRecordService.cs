namespace HospitalManagement.Modules.SpecialtyCare.Application;

public interface IDeliveryRecordService
{
    Task<SpecialtyOperationResult<DeliveryRecordDto>> CreateDeliveryRecordAsync(
        CreateDeliveryRecordDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<SpecialtyOperationResult<NewbornDto>> AddNewbornAsync(
        Guid deliveryRecordId,
        AddNewbornDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<DeliveryRecordDto?> GetDeliveryRecordByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<DeliveryRecordDto>> GetDeliveryRecordsByMotherPatientIdAsync(
        Guid motherPatientId,
        CancellationToken cancellationToken = default);
}
