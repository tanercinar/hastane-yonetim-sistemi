using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Diagnostics;

namespace HospitalManagement.Web.Client.Diagnostics;

public interface IDiagnosticsApiClient
{
    Task<EncounterDetailResponse?> GetEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<EncounterDetailResponse?>(null);

    Task<List<LabCatalogSummaryResponse>> SearchLabCatalogAsync(
        string? query = null,
        string? category = null,
        bool? isActive = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    Task<LabCatalogItemResponse?> GetLabCatalogItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderDetailResponse?> CreateDiagnosticOrderDraftAsync(
        CreateDiagnosticOrderDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderDetailResponse?> UpdateDiagnosticOrderDraftAsync(
        Guid id,
        UpdateDiagnosticOrderDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderDetailResponse?> PlaceDiagnosticOrderAsync(
        Guid id,
        PlaceDiagnosticOrderRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderDetailResponse?> CancelDiagnosticOrderAsync(
        Guid id,
        CancelDiagnosticOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<DiagnosticOrderDetailResponse?> GetDiagnosticOrderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrderWorklistAsync(
        string? orderType = null,
        string? status = null,
        string? orderNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    // Specimen Methods
    Task<SpecimenDetailResponse?> CollectSpecimenAsync(
        CollectSpecimenRequest request,
        CancellationToken cancellationToken = default);

    Task<SpecimenDetailResponse?> TransitSpecimenAsync(
        Guid id,
        TransitSpecimenRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<SpecimenDetailResponse?> ReceiveSpecimenAsync(
        Guid id,
        ReceiveSpecimenRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<SpecimenDetailResponse?> RejectSpecimenAsync(
        Guid id,
        RejectSpecimenRequest request,
        CancellationToken cancellationToken = default);

    Task<SpecimenDetailResponse?> GetSpecimenByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SpecimenDetailResponse?> GetSpecimenByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    Task<List<SpecimenSummaryResponse>> GetSpecimensByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<SpecimenSummaryResponse>> GetSpecimenWorklistAsync(
        string? status = null,
        string? barcode = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    // Lab Result Methods
    Task<LabResultDetailResponse?> CreateDraftLabResultAsync(
        CreateDraftLabResultRequest request,
        CancellationToken cancellationToken = default);

    Task<LabResultDetailResponse?> UpdateLabResultItemsAsync(
        Guid id,
        UpdateLabResultItemsRequest request,
        CancellationToken cancellationToken = default);

    Task<LabResultDetailResponse?> ApproveLabResultTechnicallyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LabResultDetailResponse?> ApproveLabResultClinicallyAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LabResultDetailResponse?> CorrectLabResultAsync(
        Guid id,
        CorrectLabResultRequest request,
        CancellationToken cancellationToken = default);

    Task<LabResultDetailResponse?> GetLabResultByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<LabResultDetailResponse>> GetLabResultsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<LabResultSummaryResponse>> GetLabResultWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    // Critical Result Notification Methods
    Task<List<CriticalResultNotificationResponse>> GetActiveCriticalNotificationsAsync(
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<CriticalResultNotificationResponse?> GetCriticalNotificationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CriticalResultNotificationResponse?> AcknowledgeCriticalNotificationAsync(
        Guid id,
        AcknowledgeCriticalResultRequest request,
        CancellationToken cancellationToken = default);

    Task<CriticalResultNotificationResponse?> EscalateCriticalNotificationAsync(
        Guid id,
        EscalateCriticalResultRequest request,
        CancellationToken cancellationToken = default);

    // Radiology Methods
    Task<List<RadiologyCatalogItemResponse>> GetRadiologyCatalogAsync(
        string? modality = null,
        CancellationToken cancellationToken = default);

    Task<List<RadiologyStudySummaryResponse>> GetRadiologyWorklistAsync(
        string? modality = null,
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> GetRadiologyStudyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> EnsureRadiologyStudyAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> ScheduleRadiologyStudyAsync(
        Guid id,
        ScheduleRadiologyStudyRequest request,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> CompleteRadiologyAcquisitionAsync(
        Guid id,
        CompleteAcquisitionRequest request,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> DraftRadiologyReportAsync(
        Guid id,
        DraftRadiologyReportRequest request,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> FinalizeRadiologyReportAsync(
        Guid id,
        FinalizeRadiologyReportRequest request,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> AddRadiologyAddendumAsync(
        Guid id,
        AddRadiologyAddendumRequest request,
        CancellationToken cancellationToken = default);

    Task<RadiologyStudyDetailResponse?> CancelRadiologyStudyAsync(
        Guid id,
        CancelRadiologyStudyRequest request,
        CancellationToken cancellationToken = default);

    // PACS / DICOM Simulation Methods
    Task<DicomStudyMetadataResponse?> GetDicomStudyMetadataAsync(
        Guid studyId,
        CancellationToken cancellationToken = default);

    Task<DicomPreviewTokenResponse?> GenerateDicomPreviewTokenAsync(
        Guid studyId,
        GenerateDicomPreviewTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<string?> GetDicomPreviewDataUrlAsync(
        string token,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    // Pathology Methods
    Task<List<PathologyCaseSummaryResponse>> GetPathologyWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> GetPathologyCaseByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> EnsurePathologyCaseAsync(
        Guid orderId,
        Guid orderItemId,
        string? specimenType = null,
        string? anatomicSite = null,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> ReceivePathologySpecimenAsync(
        Guid id,
        ReceivePathologySpecimenRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> RecordPathologyGrossExamAsync(
        Guid id,
        RecordGrossExamRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> RecordPathologyMicroscopicExamAsync(
        Guid id,
        RecordMicroscopicExamRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> DraftPathologyReportAsync(
        Guid id,
        DraftPathologyReportRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> FinalizePathologyReportAsync(
        Guid id,
        FinalizePathologyReportRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> CorrectPathologyReportAsync(
        Guid id,
        CorrectPathologyReportRequest request,
        CancellationToken cancellationToken = default);

    Task<PathologyCaseDetailResponse?> CancelPathologyCaseAsync(
        Guid id,
        CancelPathologyCaseRequest request,
        CancellationToken cancellationToken = default);

    // Blood Bank Methods
    Task<List<BloodUnitResponse>> GetBloodInventoryAsync(
        string? productType = null,
        string? bloodGroup = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<BloodInventorySummaryResponse?> GetBloodInventorySummaryAsync(
        CancellationToken cancellationToken = default);

    Task<List<CrossmatchSummaryResponse>> GetCrossmatchWorklistAsync(
        string? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default);

    Task<CrossmatchDetailResponse?> GetCrossmatchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CrossmatchDetailResponse?> CreateCrossmatchRequestAsync(
        CreateCrossmatchRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CrossmatchDetailResponse?> PerformCrossmatchTestAsync(
        Guid id,
        PerformCrossmatchRequestDto request,
        CancellationToken cancellationToken = default);

    Task<BloodUnitResponse?> IssueBloodUnitAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BloodUnitResponse?> RecordTransfusionAsync(
        Guid id,
        RecordTransfusionRequestDto? request = null,
        CancellationToken cancellationToken = default);

    // Timeline and Patient Portal Methods
    Task<PatientDiagnosticTimelineResponse?> GetPatientDiagnosticTimelineAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<List<PatientPortalResultSummaryResponse>> GetMyPatientPortalResultsAsync(
        CancellationToken cancellationToken = default);
}
