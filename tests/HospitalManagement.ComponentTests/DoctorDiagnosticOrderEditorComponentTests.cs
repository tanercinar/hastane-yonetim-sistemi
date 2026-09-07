using Bunit;

using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Diagnostics;
using HospitalManagement.Web.Client.Diagnostics;
using HospitalManagement.Web.Client.Pages.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class DoctorDiagnosticOrderEditorComponentTests
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G02")]
    public void RendersDemoDisclaimerAndCatalogItems()
    {
        using var context = new BunitContext();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            CatalogItems =
            [
                new(Guid.NewGuid(), "DEMO-LAB-CBC", "Tam Kan Sayımı (Hemogram)", "Hematoloji", "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", true, 30, true, 7),
                new(Guid.NewGuid(), "DEMO-LAB-GLU", "Açlık Kan Şekeri", "Klinik Biyokimya", "Serum", "Sarı Kapaklı Jelli Tüp", false, 45, true, 1),
            ],
        };

        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<DoctorDiagnosticOrderEditor>(parameters => parameters
            .Add(p => p.EncounterId, Guid.NewGuid())
            .Add(p => p.PatientId, Guid.NewGuid())
            .Add(p => p.DepartmentId, Guid.NewGuid())
            .Add(p => p.IsReadOnly, false));

        Assert.Contains("EĞİTİM VE DEMO AMAÇLIDIR", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tam Kan Sayımı (Hemogram)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Açlık Kan Şekeri", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-G02")]
    public void DisplaysExistingEncounterOrders()
    {
        using var context = new BunitContext();
        var encId = Guid.NewGuid();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            EncounterOrders =
            [
                new(Guid.NewGuid(), "DEMO-LAB-20260830-112233", Guid.NewGuid(), encId, Guid.NewGuid(), Guid.NewGuid(), "Laboratory", "Routine", "Placed", 2, DateTime.UtcNow, DateTime.UtcNow),
            ],
        };

        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<DoctorDiagnosticOrderEditor>(parameters => parameters
            .Add(p => p.EncounterId, encId)
            .Add(p => p.PatientId, Guid.NewGuid())
            .Add(p => p.DepartmentId, Guid.NewGuid())
            .Add(p => p.IsReadOnly, false));

        Assert.Contains("DEMO-LAB-20260830-112233", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("İletildi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("2 Kalem", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void RadiologySelectionLoadsCatalogAndCreatesRadiologyDraftRequest()
    {
        using var context = new BunitContext();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            RadiologyCatalogItems =
            [
                new(
                    Guid.NewGuid(),
                    "DEMO-RAD-CT-CHEST",
                    "Toraks Bilgisayarlı Tomografi",
                    "CT",
                    "Toraks",
                    "Sentetik demo tetkiki",
                    "Demo hazırlık",
                    true,
                    20,
                    true),
            ],
        };

        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<DoctorDiagnosticOrderEditor>(parameters => parameters
            .Add(p => p.EncounterId, Guid.NewGuid())
            .Add(p => p.PatientId, Guid.NewGuid())
            .Add(p => p.DepartmentId, Guid.NewGuid())
            .Add(p => p.IsReadOnly, false));

        cut.Find("[data-testid='diagnostic-order-type']").Change("Radiology");
        cut.WaitForAssertion(() => Assert.Contains("Toraks Bilgisayarlı Tomografi", cut.Markup, StringComparison.Ordinal));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("+ Ekle", StringComparison.Ordinal))
            .Click();
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Taslak Kaydet", StringComparison.Ordinal))
            .Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(fakeApi.CapturedDraftRequest);
            Assert.Equal("Radiology", fakeApi.CapturedDraftRequest.OrderType);
            Assert.Equal("DEMO-RAD-CT-CHEST", Assert.Single(fakeApi.CapturedDraftRequest.Items).CatalogCode);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void RoutedPageLoadsEncounterAndRendersDiagnosticEditor()
    {
        using var context = new BunitContext();
        var encounterId = Guid.NewGuid();
        var fakeApi = new FakeDiagnosticsApiClient
        {
            Encounter = new EncounterDetailResponse(
                encounterId,
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Outpatient",
                "InProgress",
                DateTime.UtcNow,
                DateTime.UtcNow,
                null,
                "DEMO yakınma",
                null,
                null,
                null,
                null,
                null,
                1,
                DateTime.UtcNow,
                null,
                []),
        };
        context.Services.AddSingleton<IDiagnosticsApiClient>(fakeApi);

        var cut = context.Render<DoctorDiagnosticOrderPage>(parameters => parameters
            .Add(page => page.EncounterId, encounterId));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Klinik Tanısal İstem Yönetimi", cut.Markup, StringComparison.Ordinal);
            Assert.NotNull(cut.Find("[data-testid='doctor-diagnostic-order-editor']"));
        });
    }

    private sealed class FakeDiagnosticsApiClient : IDiagnosticsApiClient
    {
        public List<LabCatalogSummaryResponse> CatalogItems { get; set; } = [];
        public List<RadiologyCatalogItemResponse> RadiologyCatalogItems { get; set; } = [];
        public List<DiagnosticOrderSummaryResponse> EncounterOrders { get; set; } = [];
        public CreateDiagnosticOrderDraftRequest? CapturedDraftRequest
        {
            get; private set;
        }
        public EncounterDetailResponse? Encounter
        {
            get; set;
        }

        public Task<EncounterDetailResponse?> GetEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Encounter?.Id == encounterId ? Encounter : null);

        public Task<List<LabCatalogSummaryResponse>> SearchLabCatalogAsync(string? query = null, string? category = null, bool? isActive = null, int maxResults = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult(CatalogItems);

        public Task<LabCatalogItemResponse?> GetLabCatalogItemByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<LabCatalogItemResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> CreateDiagnosticOrderDraftAsync(CreateDiagnosticOrderDraftRequest request, CancellationToken cancellationToken = default)
        {
            CapturedDraftRequest = request;
            return Task.FromResult<DiagnosticOrderDetailResponse?>(null);
        }

        public Task<DiagnosticOrderDetailResponse?> UpdateDiagnosticOrderDraftAsync(Guid id, UpdateDiagnosticOrderDraftRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> PlaceDiagnosticOrderAsync(Guid id, PlaceDiagnosticOrderRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> CancelDiagnosticOrderAsync(Guid id, CancelDiagnosticOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<DiagnosticOrderDetailResponse?> GetDiagnosticOrderByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticOrderDetailResponse?>(null);

        public Task<List<DiagnosticOrderSummaryResponse>> GetDiagnosticOrdersByEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default) =>
            Task.FromResult(EncounterOrders);

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
            Task.FromResult(RadiologyCatalogItems);

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
