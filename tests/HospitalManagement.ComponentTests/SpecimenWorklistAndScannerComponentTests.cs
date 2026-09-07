using Bunit;

using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Pages.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class SpecimenWorklistAndScannerComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G03")]
    public void RendersDemoDisclaimerAndSpecimenWorklist()
    {
        using var context = new BunitContext();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            Specimens =
            [
                new(Guid.NewGuid(), "DEMO-SMP-20260830-112233", Guid.NewGuid(), Guid.NewGuid(), "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", "Collected", DateTime.UtcNow, null, DateTime.UtcNow),
                new(Guid.NewGuid(), "DEMO-SMP-20260830-445566", Guid.NewGuid(), Guid.NewGuid(), "Serum", "Sarı Kapaklı Jelli Tüp", "Received", DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow),
            ],
        };

        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<SpecimenWorklistAndScanner>();

        Assert.Contains("EĞİTİM VE DEMO AMAÇLIDIR", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-SMP-20260830-112233", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-SMP-20260830-445566", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Toplandı", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Kabul Edildi", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G03")]
    public void DisplaysScannedBarcodeDetailAndCustodyChain()
    {
        using var context = new BunitContext();
        var specimenId = Guid.NewGuid();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            ScannedSpecimen = new(
                specimenId,
                "DEMO-SMP-20260830-999888",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Venöz Tam Kan",
                "Mor Kapaklı EDTA Tüp",
                "Received",
                "Örnek tüpü tam doldu.",
                null,
                DateTime.UtcNow.AddMinutes(-20),
                Guid.NewGuid(),
                DateTime.UtcNow,
                Guid.NewGuid(),
                null,
                null,
                DateTime.UtcNow.AddMinutes(-20),
                DateTime.UtcNow,
                1,
                [
                    new(Guid.NewGuid(), specimenId, "Collected", "Collected", DateTime.UtcNow.AddMinutes(-20), Guid.NewGuid(), "Nurse", "Kan Alma Ünitesi", "Numune alındı ve barkodlandı."),
                    new(Guid.NewGuid(), specimenId, "Collected", "InTransit", DateTime.UtcNow.AddMinutes(-10), Guid.NewGuid(), "Courier", "Pnömatik Hat", "Numune kuryeye verildi."),
                    new(Guid.NewGuid(), specimenId, "InTransit", "Received", DateTime.UtcNow, Guid.NewGuid(), "LabTechnician", "Merkez Laboratuvar", "Laboratuvarda kabul edildi."),
                ]),
        };

        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<SpecimenWorklistAndScanner>();

        // Type barcode in input
        var input = cut.Find("input[placeholder*='Barkod okutun']");
        input.Change("DEMO-SMP-20260830-999888");

        var button = cut.Find("button.btn-primary");
        button.Click();

        Assert.Contains("Barkod Detayı: DEMO-SMP-20260830-999888", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Devir Teslim Zinciri", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Kan Alma Ünitesi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pnömatik Hat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Merkez Laboratuvar", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void PendingLaboratoryOrderCanStartSpecimenChain()
    {
        using var context = new BunitContext();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var fakeApi = new FakeDiagnosticsApiClient
        {
            PendingOrders =
            [
                new(orderId, "DEMO-LAB-20260830-ABC123", patientId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Laboratory", "Routine", "Placed", 1, now, now),
            ],
            OrderDetail = new(
                orderId,
                "DEMO-LAB-20260830-ABC123",
                patientId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Laboratory",
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
                [new(orderItemId, orderId, "DEMO-LAB-CBC", "Hemogram", "Hematoloji", "Pending", "Venöz Tam Kan (Mor Kapaklı EDTA Tüp)", now, null)]),
            ScannedSpecimen = new(
                Guid.NewGuid(),
                "DEMO-SMP-20260830-ABC123",
                orderId,
                patientId,
                "Venöz Tam Kan",
                "Mor Kapaklı EDTA Tüp",
                "Collected",
                null,
                null,
                now,
                Guid.NewGuid(),
                null,
                null,
                null,
                null,
                now,
                null,
                1,
                []),
        };
        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<SpecimenWorklistAndScanner>();

        cut.WaitForAssertion(() => Assert.Contains("DEMO-LAB-20260830-ABC123", cut.Markup, StringComparison.Ordinal));
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Numune Al ve Barkodla", StringComparison.Ordinal))
            .Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Venöz Tam Kan", cut.Find("#collection-specimen-type").GetAttribute("value"));
            Assert.Equal("Mor Kapaklı EDTA Tüp", cut.Find("#collection-container-type").GetAttribute("value"));
        });
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Numuneyi Kaydet", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(fakeApi.CapturedCollectRequest);
            Assert.Equal(orderId, fakeApi.CapturedCollectRequest.DiagnosticOrderId);
            Assert.Equal(patientId, fakeApi.CapturedCollectRequest.PatientId);
        });
    }

    private sealed class FakeDiagnosticsApiClient : IDiagnosticsApiClient
    {
        public List<SpecimenSummaryResponse> Specimens { get; set; } = [];
        public List<DiagnosticOrderSummaryResponse> PendingOrders { get; set; } = [];
        public DiagnosticOrderDetailResponse? OrderDetail
        {
            get; set;
        }
        public CollectSpecimenRequest? CapturedCollectRequest
        {
            get; private set;
        }
        public SpecimenDetailResponse? ScannedSpecimen
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

        public Task<SpecimenDetailResponse?> CollectSpecimenAsync(CollectSpecimenRequest request, CancellationToken cancellationToken = default)
        {
            CapturedCollectRequest = request;
            return Task.FromResult(ScannedSpecimen);
        }

        public Task<SpecimenDetailResponse?> TransitSpecimenAsync(Guid id, TransitSpecimenRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> ReceiveSpecimenAsync(Guid id, ReceiveSpecimenRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> RejectSpecimenAsync(Guid id, RejectSpecimenRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> GetSpecimenByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SpecimenDetailResponse?>(null);

        public Task<SpecimenDetailResponse?> GetSpecimenByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
            Task.FromResult(ScannedSpecimen);

        public Task<List<SpecimenSummaryResponse>> GetSpecimensByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SpecimenSummaryResponse>());

        public Task<List<SpecimenSummaryResponse>> GetSpecimenWorklistAsync(string? status = null, string? barcode = null, Guid? patientId = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(Specimens);

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

        public Task<PatientDiagnosticTimelineResponse?> GetPatientDiagnosticTimelineAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientDiagnosticTimelineResponse?>(null);

        public Task<List<PatientPortalResultSummaryResponse>> GetMyPatientPortalResultsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PatientPortalResultSummaryResponse>());
    }
}
