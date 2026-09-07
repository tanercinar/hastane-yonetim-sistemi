using HospitalManagement.Contracts.Emergency;

namespace HospitalManagement.Web.Client.Emergency;

public interface IEmergencyApiClient
{
    Task<EmergencyAdmissionResponse?> CreateAdmissionAsync(
        CreateEmergencyAdmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> RecordTriageAsync(
        Guid id,
        RecordTriageRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> AssignDoctorAsync(
        Guid id,
        AssignEmergencyDoctorRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> UpdateStatusAsync(
        Guid id,
        UpdateEmergencyAdmissionStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyAdmissionSummaryResponse>> GetAdmissionsAsync(
        string? status = null,
        string? triageLevel = null,
        Guid? patientId = null,
        Guid? assignedDoctorId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default);

    Task<EmergencyBoardSummaryResponse?> GetBoardSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<List<EmergencyBoardWorklistItemResponse>> GetBoardWorklistAsync(
        string? status = null,
        string? triageLevel = null,
        string? zone = null,
        CancellationToken cancellationToken = default);

    Task<EmergencyAdmissionResponse?> RecordDispositionAsync(
        Guid id,
        RecordEmergencyDispositionRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyOrderResponse?> CreateOrderAsync(
        CreateEmergencyOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyOrderResponse?> CompleteOrderAsync(
        Guid id,
        CompleteEmergencyOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyOrderResponse?> CancelOrderAsync(
        Guid id,
        CancelEmergencyOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyOrderResponse>> GetOrdersByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<EmergencyConsultationResponse?> RequestConsultationAsync(
        RequestEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyConsultationResponse?> AcceptConsultationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<EmergencyConsultationResponse?> RespondConsultationAsync(
        Guid id,
        RespondEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default);

    Task<EmergencyConsultationResponse?> CancelConsultationAsync(
        Guid id,
        CancelEmergencyConsultationRequest request,
        CancellationToken cancellationToken = default);

    Task<List<EmergencyConsultationResponse>> GetConsultationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);
}
