using HospitalManagement.Contracts.Interoperability;

namespace HospitalManagement.Web.Client.Interoperability;

public interface IInteroperabilityApiClient
{
    Task<List<MockServerConfigResponse>> GetAllMockConfigsAsync(CancellationToken cancellationToken = default);
    Task<MockServerConfigResponse> UpdateMockConfigAsync(string systemType, UpdateMockServerConfigRequest request, CancellationToken cancellationToken = default);
    Task<List<IntegrationMessageLogResponse>> GetRecentLogsAsync(string? systemType = null, int count = 50, CancellationToken cancellationToken = default);
    Task ResetCircuitBreakerAsync(string systemType, CancellationToken cancellationToken = default);
    Task<SimulateMockEngineResponse> SimulateOperationAsync(SimulateMockEngineRequest request, CancellationToken cancellationToken = default);
    Task<Hl7InboundResponse> ProcessHl7InboundAsync(Hl7InboundRequest request, CancellationToken cancellationToken = default);
    Task<Hl7GenerateResponse> GenerateHl7MessageAsync(string messageType, Hl7GenerateRequest request, CancellationToken cancellationToken = default);
    Task<List<Hl7DeadLetterResponse>> GetHl7DeadLettersAsync(CancellationToken cancellationToken = default);
    Task RetryHl7DeadLetterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<DicomWorklistItemResponse>> QueryModalityWorklistAsync(string? modality = null, string? scheduledDate = null, string? aeTitle = null, CancellationToken cancellationToken = default);
    Task<DicomWorklistItemResponse> CreateDicomWorklistOrderAsync(CreateDicomWorklistOrderRequest request, CancellationToken cancellationToken = default);
    Task<DicomStudyMetadataResponse?> QueryDicomStudyAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
    Task<List<DicomStudyMetadataResponse>> QueryPatientDicomStudiesAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<List<MhrsSlotResponse>> QueryMhrsSlotsAsync(Guid? doctorId = null, string? clinicCode = null, DateTime? slotDate = null, CancellationToken cancellationToken = default);
    Task<MhrsAppointmentResponse> BookMhrsAppointmentAsync(MhrsBookAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<MhrsAppointmentResponse> CancelMhrsAppointmentAsync(string mhrsAppointmentId, MhrsCancelAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<List<MhrsAppointmentResponse>> GetPatientMhrsAppointmentsAsync(string patientNationalId, CancellationToken cancellationToken = default);
    Task<MhrsSyncSummaryResponse> SyncMhrsScheduleAsync(DateTime? syncDate = null, CancellationToken cancellationToken = default);
    Task<List<ENabizTransmissionResponse>> QueryENabizQueueAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default);
    Task<ENabizTransmissionResponse> EnqueueENabizPackageAsync(EnqueueENabizPackageRequest request, CancellationToken cancellationToken = default);
    Task<ENabizTransmissionResponse> SendENabizTransmissionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ENabizTransmissionResponse> RetryENabizTransmissionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ENabizTransmissionResponse?> GetENabizTransmissionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MedulaOperationResultResponse>> GetMedulaBoundaryInfoAsync(CancellationToken cancellationToken = default);
    Task<MedulaOperationResultResponse> ExecuteMedulaDemoOperationAsync(MedulaDemoOperationRequest request, CancellationToken cancellationToken = default);
    Task<MedulaOperationResultResponse> RejectMedulaOutOfScopeAsync(MedulaOutOfScopeRequest request, CancellationToken cancellationToken = default);
}
