using Bunit;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Web.Client.Inpatient;
using HospitalManagement.Web.Client.Pages.Inpatient;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class InpatientBoardComponentTests : BunitContext
{
    private readonly FakeInpatientApiClient _fakeApi = new();

    public InpatientBoardComponentTests()
    {
        Services.AddSingleton<IInpatientApiClient>(_fakeApi);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F07-G04")]
    public void InpatientBoardRendersCardsAndRiskBadges()
    {
        var cut = Render<InpatientBoard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Yatan Hasta Klinik Panosu", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("301-A", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Yüksek Düşme Riski", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("İzolasyon: Contact", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Transfer Sürecinde", cut.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakeInpatientApiClient : IInpatientApiClient
    {
        private static readonly Guid WardId = Guid.NewGuid();
        private static readonly Guid AdmissionId = Guid.NewGuid();
        private static readonly Guid PatientId = Guid.NewGuid();
        private static readonly Guid BedId = Guid.NewGuid();

        public Task<List<WardResponse>> GetWardsAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<WardResponse>
            {
                new(WardId, "DEMO-WRD-CARD", "Kardiyoloji & Koroner Bakım", Guid.NewGuid(), "Ana Bina", "3. Kat", "Cardiology", true, 2, 4, 3),
            });
        }

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

        public Task<List<AdmissionSummaryResponse>> GetAdmissionsAsync(Guid? wardId = null, Guid? departmentId = null, string? status = null, Guid? patientId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AdmissionSummaryResponse>());

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

        public Task<List<InpatientBoardItemResponse>> GetInpatientBoardAsync(Guid? wardId = null, Guid? departmentId = null, string? riskLevel = null, bool? isolationOnly = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<InpatientBoardItemResponse>
            {
                new(
                    AdmissionId,
                    "DEMO-ADM-20260830-100200",
                    PatientId,
                    "DEMO-P-12345678",
                    "DEMO Hasta (Ahmet Y.)",
                    52,
                    "Male",
                    WardId,
                    "Kardiyoloji & Koroner Bakım",
                    BedId,
                    "301-A",
                    "301",
                    Guid.NewGuid(),
                    "DEMO Dr. Mehmet K.",
                    Guid.NewGuid(),
                    "I20.0",
                    "Anstabil Angina Pektoris",
                    "LowSodium",
                    65,
                    "High",
                    "Contact",
                    true,
                    DateTime.UtcNow.AddDays(-2),
                    3,
                    5,
                    2),
            });
        }

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

        public Task<MedicationAdministrationResponse?> ScheduleMedicationAsync(ScheduleMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> AdministerMedicationAsync(Guid id, AdministerMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> SkipMedicationAsync(Guid id, SkipMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> RefuseMedicationAsync(Guid id, RefuseMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<MedicationAdministrationResponse?> DelayMedicationAsync(Guid id, DelayMedicationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<MedicationAdministrationResponse?>(null);

        public Task<List<MedicationAdministrationResponse>> GetMedicationsByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<MedicationAdministrationResponse>());

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
