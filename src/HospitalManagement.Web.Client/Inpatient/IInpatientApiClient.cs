using HospitalManagement.Contracts.Inpatient;

namespace HospitalManagement.Web.Client.Inpatient;

public interface IInpatientApiClient
{
    Task<List<WardResponse>> GetWardsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<List<WardResponse>> GetTransferDestinationWardsAsync(
        CancellationToken cancellationToken = default) =>
        GetWardsAsync(isActive: true, cancellationToken);

    Task<WardResponse?> GetWardByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<RoomResponse>> GetRoomsByWardIdAsync(
        Guid wardId,
        CancellationToken cancellationToken = default);

    Task<List<BedResponse>> GetBedsAsync(
        Guid? wardId = null,
        Guid? roomId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<BedResponse?> GetBedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BedResponse?> UpdateBedStatusAsync(
        Guid id,
        UpdateBedStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<BedOccupancySummaryResponse?> GetOccupancySummaryAsync(
        Guid? wardId = null,
        CancellationToken cancellationToken = default);

    // Admissions (F07-G02)
    Task<AdmissionResponse?> RequestAdmissionAsync(
        CreateAdmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> AcceptAdmissionAsync(
        Guid id,
        AcceptAdmissionRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> AdmitPatientAsync(
        Guid id,
        AdmitPatientRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> CancelAdmissionAsync(
        Guid id,
        CancelAdmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> UpdateCareDetailsAsync(
        Guid id,
        UpdateCareDetailsRequest request,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> GetAdmissionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AdmissionResponse?> GetActiveAdmissionByPatientIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<AdmissionSummaryResponse>> GetAdmissionsAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    // Transfers (F07-G03)
    Task<TransferResponse?> RequestTransferAsync(
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<TransferResponse?> AcceptTransferAsync(
        Guid id,
        AcceptTransferRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<TransferResponse?> CompleteTransferAsync(
        Guid id,
        CompleteTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<TransferResponse?> CancelTransferAsync(
        Guid id,
        CancelTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<TransferResponse?> GetTransferByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<TransferSummaryResponse>> GetTransfersAsync(
        Guid? admissionId = null,
        Guid? sourceWardId = null,
        Guid? targetWardId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    // Board & Clinical Dashboard (F07-G04)
    Task<List<InpatientBoardItemResponse>> GetInpatientBoardAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        string? riskLevel = null,
        bool? isolationOnly = null,
        CancellationToken cancellationToken = default);

    Task<InpatientPatientSummaryResponse?> GetPatientSummaryAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    // Nursing Care & Observations (F07-G05)
    Task<NursingObservationResponse?> RecordObservationAsync(
        RecordObservationRequest request,
        CancellationToken cancellationToken = default);

    Task<NursingObservationResponse?> RecordObservationCorrectionAsync(
        Guid id,
        CorrectObservationRequest request,
        CancellationToken cancellationToken = default);

    Task<List<NursingObservationResponse>> GetObservationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<NursingCarePlanResponse?> CreateCarePlanAsync(
        CreateCarePlanRequest request,
        CancellationToken cancellationToken = default);

    Task<NursingCareTaskResponse?> AddTaskToCarePlanAsync(
        Guid carePlanId,
        AddCareTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<NursingCareTaskResponse?> CompleteCareTaskAsync(
        Guid taskId,
        CompleteCareTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<NursingCareTaskResponse?> CancelCareTaskAsync(
        Guid taskId,
        CancelCareTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<List<NursingCarePlanResponse>> GetCarePlansByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<NursingCareTaskResponse>> GetOverdueTasksAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default);

    // eMAR & Medication Administration (F07-G06)
    Task<List<ActiveMedicationOrderResponse>> GetActiveMedicationOrdersAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new List<ActiveMedicationOrderResponse>());

    Task<MedicationAdministrationResponse?> ScheduleMedicationAsync(
        ScheduleMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicationAdministrationResponse?> AdministerMedicationAsync(
        Guid id,
        AdministerMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicationAdministrationResponse?> SkipMedicationAsync(
        Guid id,
        SkipMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicationAdministrationResponse?> RefuseMedicationAsync(
        Guid id,
        RefuseMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicationAdministrationResponse?> DelayMedicationAsync(
        Guid id,
        DelayMedicationRequest request,
        CancellationToken cancellationToken = default);

    Task<List<MedicationAdministrationResponse>> GetMedicationsByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<MedicationAdministrationResponse>> GetDueMedicationsAsync(
        Guid? admissionId = null,
        Guid? wardId = null,
        CancellationToken cancellationToken = default);

    // Discharge & Referral (F07-G07)
    Task<InpatientDischargeResponse?> ProcessDischargeAsync(
        DischargeAdmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<InpatientDischargeResponse?> GetDischargeByAdmissionIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default);

    Task<List<InpatientDischargeResponse>> GetDischargesAsync(
        Guid? patientId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        CancellationToken cancellationToken = default);

    // Dashboard & Realtime Occupancy (F07-G08)
    Task<InpatientDashboardResponse?> GetDashboardSummaryAsync(
        Guid? wardId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}
