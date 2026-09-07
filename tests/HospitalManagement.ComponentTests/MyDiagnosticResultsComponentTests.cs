using Bunit;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Pages.Patient;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class MyDiagnosticResultsComponentTests : BunitContext
{
    private readonly FakeDiagnosticsApiClient _fakeApi = new();

    public MyDiagnosticResultsComponentTests()
    {
        Services.AddSingleton<IDiagnosticsApiClient>(_fakeApi);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G10")]
    public void MyDiagnosticResultsRendersApprovedResults()
    {
        var cut = Render<MyDiagnosticResults>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tam Kan Sayımı (Hemogram)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Klinik Biyokimya Laboratuvarı", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("HGB", cut.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakeDiagnosticsApiClient : IDiagnosticsApiClient
    {
        public Task<List<PatientPortalResultSummaryResponse>> GetMyPatientPortalResultsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<PatientPortalResultSummaryResponse>
            {
                new(
                    Guid.NewGuid(),
                    "Laboratory",
                    "Tam Kan Sayımı (Hemogram)",
                    "FinalApproved",
                    DateTime.UtcNow.AddHours(-3),
                    "Klinik Biyokimya Laboratuvarı",
                    false,
                    "Klinik olarak normal sınırlar içinde.",
                    [
                        new("HGB", "14.5", "g/dL", "12.0 - 16.0", "Normal", false),
                        new("WBC", "7.2", "10^3/uL", "4.0 - 10.0", "Normal", false),
                    ]),
            });
        }

        public Task<PatientDiagnosticTimelineResponse?> GetPatientDiagnosticTimelineAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientDiagnosticTimelineResponse?>(null);

        public Task<List<LabCatalogSummaryResponse>> SearchLabCatalogAsync(string? query = null, string? category = null, bool? isActive = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<LabCatalogSummaryResponse>());

        public Task<LabCatalogItemResponse?> GetLabCatalogItemByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabCatalogItemResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> CreateDiagnosticOrderDraftAsync(CreateDiagnosticOrderDraftRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> UpdateDiagnosticOrderDraftAsync(Guid id, UpdateDiagnosticOrderDraftRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> PlaceDiagnosticOrderAsync(Guid id, PlaceDiagnosticOrderRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> CancelDiagnosticOrderAsync(Guid id, CancelDiagnosticOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> GetDiagnosticOrderByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiagnosticOrderSummaryResponse>());

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiagnosticOrderSummaryResponse>());

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrderWorklistAsync(string? orderType = null, string? status = null, string? orderNumber = null, Guid? patientId = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiagnosticOrderSummaryResponse>());

        public Task<SpecimenDetailResponse?> CollectSpecimenAsync(CollectSpecimenRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> TransitSpecimenAsync(Guid id, TransitSpecimenRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> ReceiveSpecimenAsync(Guid id, ReceiveSpecimenRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> RejectSpecimenAsync(Guid id, RejectSpecimenRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> GetSpecimenByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> GetSpecimenByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<List<SpecimenSummaryResponse>> GetSpecimensByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SpecimenSummaryResponse>());

        public Task<List<SpecimenSummaryResponse>> GetSpecimenWorklistAsync(string? status = null, string? barcode = null, Guid? patientId = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SpecimenSummaryResponse>());

        public Task<LabResultDetailResponse?> CreateDraftLabResultAsync(CreateDraftLabResultRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> UpdateLabResultItemsAsync(Guid id, UpdateLabResultItemsRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> ApproveLabResultTechnicallyAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> ApproveLabResultClinicallyAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> CorrectLabResultAsync(Guid id, CorrectLabResultRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> GetLabResultByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<List<LabResultDetailResponse>> GetLabResultsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<LabResultDetailResponse>());

        public Task<List<LabResultSummaryResponse>> GetLabResultWorklistAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<LabResultSummaryResponse>());

        public Task<List<CriticalResultNotificationResponse>> GetActiveCriticalNotificationsAsync(Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<CriticalResultNotificationResponse>());

        public Task<CriticalResultNotificationResponse?> GetCriticalNotificationByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CriticalResultNotificationResponse?>(null);

        public Task<CriticalResultNotificationResponse?> AcknowledgeCriticalNotificationAsync(Guid id, AcknowledgeCriticalResultRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<CriticalResultNotificationResponse?>(null);

        public Task<CriticalResultNotificationResponse?> EscalateCriticalNotificationAsync(Guid id, EscalateCriticalResultRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<CriticalResultNotificationResponse?>(null);

        public Task<List<RadiologyCatalogItemResponse>> GetRadiologyCatalogAsync(string? modality = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<RadiologyCatalogItemResponse>());

        public Task<List<RadiologyStudySummaryResponse>> GetRadiologyWorklistAsync(string? modality = null, string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<RadiologyStudySummaryResponse>());

        public Task<RadiologyStudyDetailResponse?> GetRadiologyStudyByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> EnsureRadiologyStudyAsync(Guid orderId, Guid orderItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> ScheduleRadiologyStudyAsync(Guid id, ScheduleRadiologyStudyRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> CompleteRadiologyAcquisitionAsync(Guid id, CompleteAcquisitionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> DraftRadiologyReportAsync(Guid id, DraftRadiologyReportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> FinalizeRadiologyReportAsync(Guid id, FinalizeRadiologyReportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> AddRadiologyAddendumAsync(Guid id, AddRadiologyAddendumRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<RadiologyStudyDetailResponse?> CancelRadiologyStudyAsync(Guid id, CancelRadiologyStudyRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<RadiologyStudyDetailResponse?>(null);

        public Task<DicomStudyMetadataResponse?> GetDicomStudyMetadataAsync(Guid studyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DicomStudyMetadataResponse?>(null);

        public Task<DicomPreviewTokenResponse?> GenerateDicomPreviewTokenAsync(Guid studyId, GenerateDicomPreviewTokenRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DicomPreviewTokenResponse?>(null);

        public Task<List<PathologyCaseSummaryResponse>> GetPathologyWorklistAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PathologyCaseSummaryResponse>());

        public Task<PathologyCaseDetailResponse?> GetPathologyCaseByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> EnsurePathologyCaseAsync(Guid orderId, Guid orderItemId, string? specimenType = null, string? anatomicSite = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> ReceivePathologySpecimenAsync(Guid id, ReceivePathologySpecimenRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> RecordPathologyGrossExamAsync(Guid id, RecordGrossExamRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> RecordPathologyMicroscopicExamAsync(Guid id, RecordMicroscopicExamRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> DraftPathologyReportAsync(Guid id, DraftPathologyReportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> FinalizePathologyReportAsync(Guid id, FinalizePathologyReportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> CorrectPathologyReportAsync(Guid id, CorrectPathologyReportRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<PathologyCaseDetailResponse?> CancelPathologyCaseAsync(Guid id, CancelPathologyCaseRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<PathologyCaseDetailResponse?>(null);

        public Task<List<BloodUnitResponse>> GetBloodInventoryAsync(string? productType = null, string? bloodGroup = null, string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<BloodUnitResponse>());

        public Task<BloodInventorySummaryResponse?> GetBloodInventorySummaryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<BloodInventorySummaryResponse?>(null);

        public Task<List<CrossmatchSummaryResponse>> GetCrossmatchWorklistAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<CrossmatchSummaryResponse>());

        public Task<CrossmatchDetailResponse?> GetCrossmatchByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CrossmatchDetailResponse?>(null);

        public Task<CrossmatchDetailResponse?> CreateCrossmatchRequestAsync(CreateCrossmatchRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<CrossmatchDetailResponse?>(null);

        public Task<CrossmatchDetailResponse?> PerformCrossmatchTestAsync(Guid id, PerformCrossmatchRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult<CrossmatchDetailResponse?>(null);

        public Task<BloodUnitResponse?> IssueBloodUnitAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<BloodUnitResponse?>(null);

        public Task<BloodUnitResponse?> RecordTransfusionAsync(Guid id, RecordTransfusionRequestDto? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<BloodUnitResponse?>(null);
    }
}
