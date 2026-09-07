using Bunit;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Web.Client.Emergency;
using HospitalManagement.Web.Client.Pages.Emergency;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class EmergencyTrackingBoardComponentTests : BunitContext
{
    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-G02")]
    public void EmergencyTrackingBoardRendersKpiSummaryAndWorklist()
    {
        var mockApi = new MockEmergencyBoardApiClient();
        Services.AddSingleton<IEmergencyApiClient>(mockApi);

        var cut = Render<EmergencyTrackingBoard>();

        // Verify Title and Subtitle
        Assert.Contains("Acil Servis Takip Panosu", cut.Markup, StringComparison.Ordinal);

        // Verify KPI summary cards
        Assert.Contains("Aktif Acil Hastası", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Triyaj Bekleyen", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Hekim Bekleyen", cut.Markup, StringComparison.Ordinal);

        // Verify Protocol in table
        Assert.Contains("DEMO-EMG-20260831-000001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Akut göğüs ağrısı", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Resüsitasyon 1", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class MockEmergencyBoardApiClient : IEmergencyApiClient
    {
        public Task<EmergencyBoardSummaryResponse?> GetBoardSummaryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyBoardSummaryResponse?>(new EmergencyBoardSummaryResponse(
                TotalActiveAdmissions: 2,
                WaitingTriageCount: 1,
                TriagedWaitingDoctorCount: 1,
                InEvaluationCount: 0,
                InObservationCount: 0,
                AdmittedPendingTransferCount: 0,
                TodayDischargedCount: 3,
                Red1Count: 1,
                Red2Count: 0,
                YellowCount: 1,
                GreenCount: 0,
                BlackCount: 0,
                AverageWaitMinutesTriage: 12.5,
                AverageWaitMinutesDoctor: 25.0,
                AverageLengthOfStayMinutes: 45.0));

        public Task<List<EmergencyBoardWorklistItemResponse>> GetBoardWorklistAsync(
            string? status = null,
            string? triageLevel = null,
            string? zone = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyBoardWorklistItemResponse>
            {
                new(
                    AdmissionId: Guid.NewGuid(),
                    EmergencyProtocolNumber: "DEMO-EMG-20260831-000001",
                    PatientId: Guid.NewGuid(),
                    ArrivalType: "Ambulance",
                    ChiefComplaint: "Akut göğüs ağrısı",
                    Status: "TriagedWaitingDoctor",
                    TriageLevel: "Red1Resuscitation",
                    TriageCategoryReason: "Akut koroner sendrom şüphesi",
                    AdmittedAtUtc: DateTime.UtcNow.AddMinutes(-30),
                    TriagedAtUtc: DateTime.UtcNow.AddMinutes(-20),
                    AssignedDoctorId: null,
                    AssignedBedOrZone: "Resüsitasyon 1",
                    WaitingMinutes: 30,
                    DoctorWaitingMinutes: 20),
            });

        public Task<EmergencyAdmissionResponse?> CreateAdmissionAsync(CreateEmergencyAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> RecordTriageAsync(Guid id, RecordTriageRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> AssignDoctorAsync(Guid id, AssignEmergencyDoctorRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> UpdateStatusAsync(Guid id, UpdateEmergencyAdmissionStatusRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyAdmissionResponse?> GetActiveAdmissionByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<List<EmergencyAdmissionSummaryResponse>> GetAdmissionsAsync(
            string? status = null,
            string? triageLevel = null,
            Guid? patientId = null,
            Guid? assignedDoctorId = null,
            DateTime? fromDateUtc = null,
            DateTime? toDateUtc = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyAdmissionSummaryResponse>());

        public Task<EmergencyAdmissionResponse?> RecordDispositionAsync(Guid id, RecordEmergencyDispositionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyAdmissionResponse?>(null);

        public Task<EmergencyOrderResponse?> CreateOrderAsync(CreateEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<EmergencyOrderResponse?> CompleteOrderAsync(Guid id, CompleteEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<EmergencyOrderResponse?> CancelOrderAsync(Guid id, CancelEmergencyOrderRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyOrderResponse?>(null);

        public Task<List<EmergencyOrderResponse>> GetOrdersByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyOrderResponse>());

        public Task<EmergencyConsultationResponse?> RequestConsultationAsync(RequestEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> AcceptConsultationAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> RespondConsultationAsync(Guid id, RespondEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<EmergencyConsultationResponse?> CancelConsultationAsync(Guid id, CancelEmergencyConsultationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<EmergencyConsultationResponse?>(null);

        public Task<List<EmergencyConsultationResponse>> GetConsultationsByAdmissionIdAsync(Guid admissionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<EmergencyConsultationResponse>());
    }
}
