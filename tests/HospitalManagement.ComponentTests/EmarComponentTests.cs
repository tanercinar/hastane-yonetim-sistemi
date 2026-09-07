using Bunit;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Web.Client.Inpatient;
using HospitalManagement.Web.Client.Pages.Inpatient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class EmarComponentTests : BunitContext
{
    private static readonly Guid AdmissionId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid PrescriptionId = Guid.Parse("00000000-0000-0000-0000-000000000201");
    private static readonly Guid PrescriptionItemId = Guid.Parse("00000000-0000-0000-0000-000000000202");

    [Fact]
    [Trait("Roadmap", "F07-G06")]
    public void EmarRendersDoseListAndTabsCorrectly()
    {
        Services.AddSingleton<IInpatientApiClient>(new FakeEmarInpatientApiClient());

        var cut = Render<Emar>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("İlaç Uygulama Kaydı (eMAR)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-Parasetamol 500mg Tablet", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("500 mg", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Planlanan Dozlar", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Roadmap", "F07-G06")]
    public void ScheduleModalDisablesSubmissionWhenNoActiveMedicationOrderExists()
    {
        Services.AddSingleton<IInpatientApiClient>(new FakeEmarInpatientApiClient());
        var cut = Render<Emar>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Single(button => button.TextContent.Contains("Yeni İlaç Dozu Planla", StringComparison.Ordinal)).Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Serbest metinle doz planlanamaz", cut.Markup, StringComparison.Ordinal);
            Assert.True(cut.FindAll("button").Single(button => button.TextContent.Contains("Dozu Kaydet", StringComparison.Ordinal)).HasAttribute("disabled"));
        });
    }

    [Fact]
    [Trait("Roadmap", "F07-G06")]
    public void SchedulingUsesTheSelectedActivePrescriptionOrder()
    {
        var api = new FakeEmarInpatientApiClient(hasActiveMedicationOrder: true);
        Services.AddSingleton<IInpatientApiClient>(api);
        var cut = Render<Emar>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button").Single(button => button.TextContent.Contains("Yeni İlaç Dozu Planla", StringComparison.Ordinal)).Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-Parasetamol 500mg Tablet", cut.Find("#activeMedicationOrder").TextContent, StringComparison.Ordinal);
            cut.FindAll("button").Single(button => button.TextContent.Contains("Dozu Kaydet", StringComparison.Ordinal)).Click();
        });

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(api.LastScheduleRequest);
            Assert.Equal(AdmissionId, api.LastScheduleRequest.AdmissionId);
            Assert.Equal(PrescriptionId, api.LastScheduleRequest.PrescriptionId);
            Assert.Equal("DEMO-Parasetamol 500mg Tablet", api.LastScheduleRequest.MedicationName);
            Assert.Equal("500 mg", api.LastScheduleRequest.Dose);
            Assert.Equal("Oral", api.LastScheduleRequest.Route);
        });
    }

    private sealed class FakeEmarInpatientApiClient : IInpatientApiClient
    {
        private readonly bool _hasActiveMedicationOrder;

        public FakeEmarInpatientApiClient(bool hasActiveMedicationOrder = false)
        {
            _hasActiveMedicationOrder = hasActiveMedicationOrder;
        }

        public ScheduleMedicationRequest? LastScheduleRequest
        {
            get; private set;
        }

        public Task<List<WardResponse>> GetWardsAsync(bool? isActive = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<WardResponse>());

        public Task<WardResponse?> GetWardByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<WardResponse?>(null);

        public Task<List<RoomResponse>> GetRoomsByWardIdAsync(Guid wardId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<RoomResponse>());

        public Task<List<BedResponse>> GetBedsAsync(Guid? wardId = null, Guid? roomId = null, string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<BedResponse>());

        public Task<BedResponse?> GetBedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<BedResponse?>(null);

        public Task<BedResponse?> UpdateBedStatusAsync(Guid id, UpdateBedStatusRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<BedResponse?>(null);

        public Task<BedOccupancySummaryResponse?> GetOccupancySummaryAsync(Guid? wardId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<BedOccupancySummaryResponse?>(null);

        public Task<AdmissionResponse?> RequestAdmissionAsync(CreateAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> AcceptAdmissionAsync(Guid id, AcceptAdmissionRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> AdmitPatientAsync(Guid id, AdmitPatientRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> CancelAdmissionAsync(Guid id, CancelAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> UpdateCareDetailsAsync(Guid id, UpdateCareDetailsRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<AdmissionResponse?> GetActiveAdmissionByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AdmissionResponse?>(null);

        public Task<List<AdmissionSummaryResponse>> GetAdmissionsAsync(Guid? wardId = null, Guid? departmentId = null, string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<AdmissionSummaryResponse>
            {
                new(
                    AdmissionId,
                    "DEMO-ADM-20260830-100300",
                    PatientId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Kardiyoloji & Koroner Bakım",
                    Guid.NewGuid(),
                    "301-A",
                    "301",
                    "Admitted",
                    "Akut Koroner Sendrom",
                    "Contact",
                    55,
                    "LowSodium",
                    DateTime.UtcNow.AddDays(-1),
                    null),
            });
        }

        public Task<TransferResponse?> RequestTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<TransferResponse?> AcceptTransferAsync(Guid id, AcceptTransferRequest? request = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<TransferResponse?> CompleteTransferAsync(Guid id, CompleteTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<TransferResponse?> CancelTransferAsync(Guid id, CancelTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<TransferResponse?> GetTransferByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransferResponse?>(null);

        public Task<List<TransferSummaryResponse>> GetTransfersAsync(Guid? admissionId = null, Guid? sourceWardId = null, Guid? targetWardId = null, string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<TransferSummaryResponse>());

        public Task<List<InpatientBoardItemResponse>> GetInpatientBoardAsync(Guid? wardId = null, Guid? departmentId = null, string? riskLevel = null, bool? isolationOnly = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<InpatientBoardItemResponse>());

        public Task<InpatientPatientSummaryResponse?> GetPatientSummaryAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<InpatientPatientSummaryResponse?>(null);

        public Task<NursingObservationResponse?> RecordObservationAsync(RecordObservationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingObservationResponse?>(null);

        public Task<NursingObservationResponse?> RecordObservationCorrectionAsync(Guid id, CorrectObservationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingObservationResponse?>(null);

        public Task<List<NursingObservationResponse>> GetObservationsByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<NursingObservationResponse>());

        public Task<NursingCarePlanResponse?> CreateCarePlanAsync(CreateCarePlanRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingCarePlanResponse?>(null);

        public Task<NursingCareTaskResponse?> AddTaskToCarePlanAsync(Guid carePlanId, AddCareTaskRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingCareTaskResponse?>(null);

        public Task<NursingCareTaskResponse?> CompleteCareTaskAsync(Guid taskId, CompleteCareTaskRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingCareTaskResponse?>(null);

        public Task<NursingCareTaskResponse?> CancelCareTaskAsync(Guid taskId, CancelCareTaskRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<NursingCareTaskResponse?>(null);

        public Task<List<NursingCarePlanResponse>> GetCarePlansByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<NursingCarePlanResponse>());

        public Task<List<NursingCareTaskResponse>> GetOverdueTasksAsync(Guid? admissionId = null, Guid? wardId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<NursingCareTaskResponse>());

        public Task<List<ActiveMedicationOrderResponse>> GetActiveMedicationOrdersAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_hasActiveMedicationOrder
                ? new List<ActiveMedicationOrderResponse>
                {
                    new(
                        PrescriptionId,
                        PrescriptionItemId,
                        "DEMO-Parasetamol 500mg Tablet",
                        "500 mg",
                        "Oral",
                        DateTime.UtcNow.AddDays(2)),
                }
                : new List<ActiveMedicationOrderResponse>());

        public Task<MedicationAdministrationResponse?> ScheduleMedicationAsync(ScheduleMedicationRequest request, CancellationToken cancellationToken = default)
        {
            LastScheduleRequest = request;
            return Task.FromResult<MedicationAdministrationResponse?>(new(
                Guid.NewGuid(),
                request.AdmissionId,
                PatientId,
                request.PrescriptionId,
                request.MedicationName,
                request.Dose,
                request.Route,
                request.ScheduledTimeUtc,
                "Scheduled",
                null,
                null,
                false,
                null,
                null,
                DateTime.UtcNow,
                1));
        }

        public Task<MedicationAdministrationResponse?> AdministerMedicationAsync(Guid id, AdministerMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> SkipMedicationAsync(Guid id, SkipMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> RefuseMedicationAsync(Guid id, RefuseMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> DelayMedicationAsync(Guid id, DelayMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<List<MedicationAdministrationResponse>> GetMedicationsByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<MedicationAdministrationResponse>
            {
                new(
                    Guid.NewGuid(),
                    admissionId,
                    PatientId,
                    null,
                    "DEMO-Parasetamol 500mg Tablet",
                    "500 mg",
                    "Oral",
                    DateTime.UtcNow.AddHours(2),
                    "Scheduled",
                    null,
                    null,
                    false,
                    null,
                    null,
                    DateTime.UtcNow.AddHours(-1),
                    1),
            });
        }

        public Task<List<MedicationAdministrationResponse>> GetDueMedicationsAsync(Guid? admissionId = null, Guid? wardId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<MedicationAdministrationResponse>());

        public Task<InpatientDischargeResponse?> ProcessDischargeAsync(DischargeAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<InpatientDischargeResponse?>(null);

        public Task<InpatientDischargeResponse?> GetDischargeByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<InpatientDischargeResponse?>(null);

        public Task<List<InpatientDischargeResponse>> GetDischargesAsync(Guid? patientId = null, DateTime? fromDateUtc = null, DateTime? toDateUtc = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<InpatientDischargeResponse>());

        public Task<InpatientDashboardResponse?> GetDashboardSummaryAsync(Guid? wardId = null, Guid? departmentId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<InpatientDashboardResponse?>(null);
    }
}
