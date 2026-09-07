using Bunit;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Web.Client.Emergency;
using HospitalManagement.Web.Client.Pages.Emergency;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class EmergencyEncounterComponentTests : BunitContext
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-G03")]
    public void EmergencyEncounterFlowRendersPatientBannerOrdersAndDisposition()
    {
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var mockApi = new MockEmergencyEncounterApiClient(admissionId, patientId);
        Services.AddSingleton<IEmergencyApiClient>(mockApi);

        var cut = Render<EmergencyEncounterFlow>(parameters => parameters.Add(p => p.AdmissionId, admissionId));

        // Verify Header Protocol and Patient ID
        Assert.Contains("DEMO-EMG-20260831-999888", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(patientId.ToString(), cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Akut Koroner Sendrom Şüphesi", cut.Markup, StringComparison.Ordinal);

        // Verify Vitals
        Assert.Contains("140/90 mmHg", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("95 bpm", cut.Markup, StringComparison.Ordinal);

        // Verify Orders Tab
        Assert.Contains("Hızlı Tetkik", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("LAB-TROP", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Troponin I (Kardiyak)", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class MockEmergencyEncounterApiClient : IEmergencyApiClient
    {
        private readonly Guid _admissionId;
        private readonly Guid _patientId;

        public MockEmergencyEncounterApiClient(Guid admissionId, Guid patientId)
        {
            _admissionId = admissionId;
            _patientId = patientId;
        }

        public Task<EmergencyAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(new EmergencyAdmissionResponse(
                Id: _admissionId,
                EmergencyProtocolNumber: "DEMO-EMG-20260831-999888",
                PatientId: _patientId,
                ArrivalType: "Ambulance",
                ChiefComplaint: "Akut Koroner Sendrom Şüphesi",
                AdmissionNotes: null,
                Status: "InEvaluation",
                AdmittedAtUtc: DateTime.UtcNow.AddMinutes(-45),
                AdmittingStaffId: Guid.NewGuid(),
                Triage: new EmergencyTriageResponse(
                    TriageLevel: "Red1Resuscitation",
                    TriageCategoryReason: "Tipik göğüs ağrısı ve EKG değişikliği",
                    TriagedAtUtc: DateTime.UtcNow.AddMinutes(-35),
                    TriageNurseId: Guid.NewGuid(),
                    EducationalClassificationAssisted: true,
                    SystolicBp: 140,
                    DiastolicBp: 90,
                    HeartRate: 95,
                    BodyTemperatureCelsius: 36.8m,
                    RespiratoryRate: 18,
                    OxygenSaturationPercent: 98,
                    PainScale: 8,
                    Consciousness: "Açık",
                    ClinicalNotes: null),
                AssignedDoctorId: Guid.NewGuid(),
                AssignedBedOrZone: "Resüsitasyon 1",
                CompletedAtUtc: null,
                DischargeOrDispositionNotes: null,
                CreatedAtUtc: DateTime.UtcNow.AddMinutes(-45),
                UpdatedAtUtc: DateTime.UtcNow.AddMinutes(-35),
                Version: 1));

        public Task<List<EmergencyOrderResponse>> GetOrdersByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyOrderResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    AdmissionId: _admissionId,
                    OrderType: "Laboratory",
                    OrderCatalogCode: "LAB-TROP",
                    OrderCatalogName: "Troponin I (Kardiyak)",
                    Priority: "Stat",
                    OrderedByDoctorId: Guid.NewGuid(),
                    OrderedAtUtc: DateTime.UtcNow.AddMinutes(-20),
                    Status: "Ordered",
                    ClinicalInstructions: "Acil bakılsın",
                    ResultSummary: null,
                    CompletedAtUtc: null,
                    CancellationReason: null,
                    Version: 1),
            });

        public Task<List<EmergencyConsultationResponse>> GetConsultationsByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyConsultationResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    AdmissionId: _admissionId,
                    DepartmentId: Guid.NewGuid(),
                    DepartmentName: "Kardiyoloji",
                    RequestedByDoctorId: Guid.NewGuid(),
                    RequestedAtUtc: DateTime.UtcNow.AddMinutes(-15),
                    Urgency: "Immediate15Min",
                    ClinicalReason: "STEMI şüphesi",
                    Status: "Requested",
                    ConsultantDoctorId: null,
                    ConsultationResponseNotes: null,
                    RespondedAtUtc: null,
                    CancellationReason: null,
                    Version: 1),
            });

        public Task<EmergencyAdmissionResponse?> CreateAdmissionAsync(CreateEmergencyAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> RecordTriageAsync(Guid id, RecordTriageRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> AssignDoctorAsync(Guid id, AssignEmergencyDoctorRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> UpdateStatusAsync(Guid id, UpdateEmergencyAdmissionStatusRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> GetActiveAdmissionByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<List<EmergencyAdmissionSummaryResponse>> GetAdmissionsAsync(string? status = null, string? triageLevel = null, Guid? patientId = null, Guid? assignedDoctorId = null, DateTime? fromDateUtc = null, DateTime? toDateUtc = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyAdmissionSummaryResponse>());

        public Task<EmergencyBoardSummaryResponse?> GetBoardSummaryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyBoardSummaryResponse?>(null);

        public Task<List<EmergencyBoardWorklistItemResponse>> GetBoardWorklistAsync(string? status = null, string? triageLevel = null, string? zone = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyBoardWorklistItemResponse>());

        public Task<EmergencyAdmissionResponse?> RecordDispositionAsync(Guid id, RecordEmergencyDispositionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyOrderResponse?> CreateOrderAsync(CreateEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<EmergencyOrderResponse?> CompleteOrderAsync(Guid id, CompleteEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<EmergencyOrderResponse?> CancelOrderAsync(Guid id, CancelEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<EmergencyConsultationResponse?> RequestConsultationAsync(RequestEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> AcceptConsultationAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> RespondConsultationAsync(Guid id, RespondEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> CancelConsultationAsync(Guid id, CancelEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);
    }
}
