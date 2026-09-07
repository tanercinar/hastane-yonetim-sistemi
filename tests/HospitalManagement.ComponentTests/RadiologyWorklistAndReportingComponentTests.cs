using Bunit;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Pages.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class RadiologyWorklistAndReportingComponentTests : BunitContext
{
    private readonly FakeDiagnosticsApiClient _fakeApi = new();

    public RadiologyWorklistAndReportingComponentTests()
    {
        Services.AddSingleton<IDiagnosticsApiClient>(_fakeApi);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G06")]
    public void RendersWorklistAndDemoBanner()
    {
        var id = Guid.NewGuid();
        _fakeApi.RadiologySummaries =
        [
            new(
                id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEMO-ACC-20260830-112233",
                "XR",
                "DEMO-RAD-CHEST-XR",
                "Akciğer Grafisi (PA)",
                "Toraks / Göğüs",
                "Acquired",
                null,
                DateTime.UtcNow,
                null,
                DateTime.UtcNow),
        ];

        var cut = Render<RadiologyWorklistAndReporting>();

        cut.WaitForAssertion(() =>
        {
            var content = cut.Markup;
            Assert.Contains("Radyoloji İş Listesi ve Raporlama", content, StringComparison.Ordinal);
            Assert.Contains("Akciğer Grafisi (PA)", content, StringComparison.Ordinal);
            Assert.Contains("DEMO-ACC-20260830-112233", content, StringComparison.Ordinal);
            Assert.Contains("Rapor Yaz", content, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G06")]
    public void OpeningReportingModalDisplaysFormWithReportTextAndImpression()
    {
        var id = Guid.NewGuid();
        _fakeApi.RadiologySummaries =
        [
            new(
                id,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEMO-ACC-20260830-112233",
                "XR",
                "DEMO-RAD-CHEST-XR",
                "Akciğer Grafisi (PA)",
                "Toraks / Göğüs",
                "Acquired",
                null,
                DateTime.UtcNow,
                null,
                DateTime.UtcNow),
        ];

        _fakeApi.RadiologyDetail = new(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-ACC-20260830-112233",
            "XR",
            "DEMO-RAD-CHEST-XR",
            "Akciğer Grafisi (PA)",
            "Toraks / Göğüs",
            "Acquired",
            null,
            DateTime.UtcNow,
            Guid.NewGuid(),
            "Çekim yapıldı",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTime.UtcNow,
            null,
            1);

        var cut = Render<RadiologyWorklistAndReporting>();

        cut.WaitForAssertion(() =>
        {
            var btn = cut.Find("button.btn-warning");
            btn.Click();
        });

        cut.WaitForAssertion(() =>
        {
            var content = cut.Markup;
            Assert.Contains("Radyoloji Raporu Düzenleme", content, StringComparison.Ordinal);
            Assert.Contains("Bulgular ve Rapor Metni", content, StringComparison.Ordinal);
            Assert.Contains("Klinik Sonuç / Kanaat (Impression)", content, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void ReportingFormEnablesFinalizeWhileRequiredFieldsAreTyped()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _fakeApi.RadiologySummaries =
        [
            new(id, Guid.NewGuid(), Guid.NewGuid(), "DEMO-ACC-LIVE-BIND", "XR", "DEMO-RAD-CHEST-XR",
                "Akciğer Grafisi", "Toraks", "Acquired", null, now, null, now),
        ];
        _fakeApi.RadiologyDetail = new(
            id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DEMO-ACC-LIVE-BIND", "XR",
            "DEMO-RAD-CHEST-XR", "Akciğer Grafisi", "Toraks", "Acquired", null, now,
            Guid.NewGuid(), null, null, null, null, null, null, null, null, null, null,
            now, null, 1);

        var cut = Render<RadiologyWorklistAndReporting>();
        cut.WaitForAssertion(() => cut.Find("button.btn-warning").Click());

        var dialog = cut.Find("div[role='dialog']");
        var finalizeButton = dialog.QuerySelector("button.btn-success")!;
        Assert.True(finalizeButton.HasAttribute("disabled"));

        dialog.QuerySelector("#radiology-report-text")!.Input("DEMO sentetik bulgular");
        dialog.QuerySelector("#radiology-report-impression")!.Input("DEMO normal kanaat");

        Assert.False(cut.Find("div[role='dialog'] button.btn-success").HasAttribute("disabled"));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void PendingRadiologyOrderCanBeInitializedIntoWorklist()
    {
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _fakeApi.PendingOrders =
        [
            new(orderId, "DEMO-RAD-20260830-ABC123", patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Radiology", "Routine", "Placed", 1, now, now),
        ];
        _fakeApi.OrderDetail = new(
            orderId,
            "DEMO-RAD-20260830-ABC123",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Radiology",
            "Routine",
            "Placed",
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
            [new(orderItemId, orderId, "DEMO-RAD-CHEST-XR", "Akciğer Grafisi", "XR", "Pending", null, now, null)]);
        _fakeApi.EnsureResponse = new(
            Id: Guid.NewGuid(),
            DiagnosticOrderId: orderId,
            DiagnosticOrderItemId: orderItemId,
            PatientId: patientId,
            AccessionNumber: "DEMO-ACC-ABC123",
            Modality: "XR",
            ProcedureCode: "DEMO-RAD-CHEST-XR",
            ProcedureName: "Akciğer Grafisi",
            BodySite: "Toraks",
            Status: "Ordered",
            ScheduledAtUtc: null,
            PerformedAtUtc: null,
            TechnicianUserId: null,
            TechnicianNotes: null,
            RadiologistUserId: null,
            ReportText: null,
            Impression: null,
            ReportDraftedAtUtc: null,
            ReportFinalizedAtUtc: null,
            AddendumText: null,
            AddendumAddedAtUtc: null,
            AddendumByUserId: null,
            CancellationReason: null,
            CreatedAtUtc: now,
            UpdatedAtUtc: null,
            Version: 1);

        var cut = Render<RadiologyWorklistAndReporting>();

        cut.WaitForAssertion(() => Assert.Contains("DEMO-RAD-20260830-ABC123", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("İş Listesine Al", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() => Assert.Contains((orderId, orderItemId), _fakeApi.EnsureCalls));
    }

    private sealed class FakeDiagnosticsApiClient : IDiagnosticsApiClient
    {
        public List<RadiologyStudySummaryResponse> RadiologySummaries { get; set; } = [];
        public List<DiagnosticOrderSummaryResponse> PendingOrders { get; set; } = [];
        public DiagnosticOrderDetailResponse? OrderDetail
        {
            get; set;
        }
        public RadiologyStudyDetailResponse? EnsureResponse
        {
            get; set;
        }
        public List<(Guid OrderId, Guid OrderItemId)> EnsureCalls { get; } = [];
        public RadiologyStudyDetailResponse? RadiologyDetail
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
            Task.FromResult(RadiologySummaries);

        public Task<RadiologyStudyDetailResponse?> GetRadiologyStudyByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(RadiologyDetail);

        public Task<RadiologyStudyDetailResponse?> EnsureRadiologyStudyAsync(Guid orderId, Guid orderItemId, CancellationToken cancellationToken = default)
        {
            EnsureCalls.Add((orderId, orderItemId));
            return Task.FromResult(EnsureResponse);
        }

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
