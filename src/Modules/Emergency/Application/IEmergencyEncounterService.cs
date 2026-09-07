namespace HospitalManagement.Modules.Emergency.Application;

public interface IEmergencyEncounterService
{
    Task<EmergencyOperationResult<EmergencyOrderDto>> CreateOrderAsync(
        CreateEmergencyOrderDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyOrderDto>> CompleteOrderAsync(
        Guid orderId,
        string? resultSummary,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyOrderDto>> CancelOrderAsync(
        Guid orderId,
        string reason,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyOrderDto>> GetOrdersByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyConsultationDto>> RequestConsultationAsync(
        RequestEmergencyConsultationDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyConsultationDto>> AcceptConsultationAsync(
        Guid consultationId,
        Guid consultantDoctorId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyConsultationDto>> RespondConsultationAsync(
        Guid consultationId,
        string responseNotes,
        Guid consultantDoctorId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyConsultationDto>> CancelConsultationAsync(
        Guid consultationId,
        string reason,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyConsultationDto>> GetConsultationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<EmergencyOperationResult<EmergencyAdmissionDto>> RecordDispositionAsync(
        Guid admissionId,
        RecordEmergencyDispositionDto dto,
        Guid doctorId,
        CancellationToken cancellationToken = default);
}
