using Bunit;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Pages.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class LabResultEntryAndApprovalComponentTests : BunitContext
{
    private readonly FakeDiagnosticsApiClient _fakeApi = new();

    public LabResultEntryAndApprovalComponentTests()
    {
        Services.AddSingleton<IDiagnosticsApiClient>(_fakeApi);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G04")]
    public void RendersWorklistAndDemoBanner()
    {
        var resultId = Guid.NewGuid();
        _fakeApi.Worklist =
        [
            new(
                resultId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "L-BIO-01",
                "Glukoz Açlık",
                "Draft",
                false,
                true,
                DateTime.UtcNow,
                null),
        ];

        var cut = Render<LabResultEntryAndApproval>();

        cut.WaitForAssertion(() =>
        {
            var content = cut.Markup;
            Assert.Contains("Laboratuvar Sonuç Girişi ve Onay", content, StringComparison.Ordinal);
            Assert.Contains("Glukoz Açlık", content, StringComparison.Ordinal);
            Assert.Contains("L-BIO-01", content, StringComparison.Ordinal);
            Assert.Contains("ANORMAL DEĞER", content, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G04")]
    public void SelectingDraftResultShowsTechnicalApprovalButNotClinicalApproval()
    {
        var resultId = Guid.NewGuid();
        _fakeApi.Worklist =
        [
            new(
                resultId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "L-BIO-01",
                "Glukoz Açlık",
                "Draft",
                false,
                false,
                DateTime.UtcNow,
                null),
        ];

        _fakeApi.Detail = new LabResultDetailResponse(
            resultId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "L-BIO-01",
            "Glukoz Açlık",
            "Draft",
            null,
            null,
            null,
            null,
            null,
            null,
            "Rutin kontrol",
            DateTime.UtcNow,
            null,
            1,
            [
                new(
                    Guid.NewGuid(),
                    resultId,
                    "GLU",
                    "Açlık Glukoz",
                    95m,
                    null,
                    "mg/dL",
                    70m,
                    100m,
                    "70 - 100",
                    "Normal",
                    null),
            ]);

        var cut = Render<LabResultEntryAndApproval>();

        cut.WaitForAssertion(() =>
        {
            var itemBtn = cut.Find("button.list-group-item");
            itemBtn.Click();
        });

        cut.WaitForAssertion(() =>
        {
            var content = cut.Markup;
            Assert.Contains("Açlık Glukoz", content, StringComparison.Ordinal);
            Assert.Contains("70 - 100", content, StringComparison.Ordinal);
            Assert.Contains("Teknik Onay", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Klinik Onay", content, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void ReceivedSpecimenCanCreateResultDraftForPendingOrderItem()
    {
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var specimenId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _fakeApi.PendingOrders =
        [
            new(orderId, "DEMO-LAB-20260830-XYZ789", patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Laboratory", "Routine", "InProgress", 1, now, now),
        ];
        _fakeApi.OrderDetail = new(
            orderId,
            "DEMO-LAB-20260830-XYZ789",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Laboratory",
            "Routine",
            "InProgress",
            "DEMO klinik gerekçe",
            null,
            null,
            null,
            now,
            null,
            null,
            now,
            now,
            1,
            [new(orderItemId, orderId, "DEMO-LAB-CBC", "Hemogram", "Hematoloji", "Pending", null, now, null)]);
        _fakeApi.OrderSpecimens =
        [
            new(specimenId, "DEMO-SMP-20260830-XYZ789", orderId, patientId, "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", "Received", now, now, now),
        ];

        var cut = Render<LabResultEntryAndApproval>();

        cut.WaitForAssertion(() => Assert.Contains("DEMO-LAB-20260830-XYZ789", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Sonuç Taslağı Oluştur", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(_fakeApi.CapturedCreateDraftRequest);
            Assert.Equal(orderItemId, _fakeApi.CapturedCreateDraftRequest.DiagnosticOrderItemId);
            Assert.Equal(specimenId, _fakeApi.CapturedCreateDraftRequest.SpecimenId);
            Assert.Equal(patientId, _fakeApi.CapturedCreateDraftRequest.PatientId);
        });
    }

    private sealed class FakeDiagnosticsApiClient : IDiagnosticsApiClient
    {
        public List<LabResultSummaryResponse> Worklist { get; set; } = [];
        public List<DiagnosticOrderSummaryResponse> PendingOrders { get; set; } = [];
        public DiagnosticOrderDetailResponse? OrderDetail
        {
            get; set;
        }
        public List<SpecimenSummaryResponse> OrderSpecimens { get; set; } = [];
        public List<LabResultDetailResponse> ExistingOrderResults { get; set; } = [];
        public CreateDraftLabResultRequest? CapturedCreateDraftRequest
        {
            get; private set;
        }
        public LabResultDetailResponse? Detail
        {
            get; set;
        }

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
            Task.FromResult(OrderDetail?.Id == id ? OrderDetail : null);

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiagnosticOrderSummaryResponse>());

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiagnosticOrderSummaryResponse>());

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrderWorklistAsync(string? orderType = null, string? status = null, string? orderNumber = null, Guid? patientId = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(PendingOrders);

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
            Task.FromResult(OrderSpecimens);

        public Task<List<SpecimenSummaryResponse>> GetSpecimenWorklistAsync(string? status = null, string? barcode = null, Guid? patientId = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SpecimenSummaryResponse>());

        public Task<LabResultDetailResponse?> CreateDraftLabResultAsync(CreateDraftLabResultRequest request, CancellationToken cancellationToken = default)
        {
            CapturedCreateDraftRequest = request;
            return Task.FromResult<LabResultDetailResponse?>(null);
        }

        public Task<LabResultDetailResponse?> UpdateLabResultItemsAsync(Guid id, UpdateLabResultItemsRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> ApproveLabResultTechnicallyAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> ApproveLabResultClinicallyAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> CorrectLabResultAsync(Guid id, CorrectLabResultRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabResultDetailResponse?>(null);

        public Task<LabResultDetailResponse?> GetLabResultByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Detail);

        public Task<List<LabResultDetailResponse>> GetLabResultsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingOrderResults);

        public Task<List<LabResultSummaryResponse>> GetLabResultWorklistAsync(string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Worklist);

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

        public Task<PatientDiagnosticTimelineResponse?> GetPatientDiagnosticTimelineAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientDiagnosticTimelineResponse?>(null);

        public Task<List<PatientPortalResultSummaryResponse>> GetMyPatientPortalResultsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PatientPortalResultSummaryResponse>());
    }
}
