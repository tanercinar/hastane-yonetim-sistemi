using Bunit;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Contracts.Patients;
using HospitalManagement.Web.Client.Emergency;
using HospitalManagement.Web.Client.Pages.Emergency;
using HospitalManagement.Web.Client.Patients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class EmergencyAdmissionsComponentTests : BunitContext
{
    private static readonly Guid PatientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid AdmissionId = Guid.Parse("60000000-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F08-G01")]
    public void EmergencyAdmissionsPageRendersListAndStatCountsCorrectly()
    {
        Services.AddSingleton<IEmergencyApiClient>(new FakeEmergencyApiClient());
        Services.AddSingleton<IPatientApiClient>(new FakePatientApiClient());

        var cut = Render<EmergencyAdmissions>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Acil Başvuru &amp; Triyaj Yönetimi", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("DEMO-EMG-20260831-100001", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Göğüs ağrısı", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Kırmızı 2", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Ambulans (112)", cut.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class FakeEmergencyApiClient : IEmergencyApiClient
    {
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

        public Task<List<EmergencyAdmissionSummaryResponse>> GetAdmissionsAsync(string? status = null, string? triageLevel = null, Guid? patientId = null, Guid? assignedDoctorId = null, DateTime? fromDateUtc = null, DateTime? toDateUtc = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<EmergencyAdmissionSummaryResponse>
            {
                new(
                    AdmissionId,
                    "DEMO-EMG-20260831-100001",
                    PatientId,
                    "Ambulance",
                    "Göğüs ağrısı ve nefes darlığı",
                    "TriagedWaitingDoctor",
                    "Red2Emergency",
                    DateTime.UtcNow.AddMinutes(-30),
                    null,
                    "Kırmızı Alan - Yatak 1"),
            });
        }

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

    private sealed class FakePatientApiClient : IPatientApiClient
    {
        public Task<PatientListResponse?> SearchPatientsAsync(string? query, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientListResponse?>(new PatientListResponse([], 0, 1, 50));

        public Task<PatientDetailResponse?> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientDetailResponse?>(null);

        public Task<PatientDetailResponse?> GetPatientByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PatientDetailResponse?>(null);

        public Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> RegisterPatientAsync(PatientRegistrationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, PatientDetailResponse?, string?)>((true, null, null));

        public Task<(bool Succeeded, PatientDetailResponse? Patient, string? ErrorMessage)> UpdatePatientAsync(Guid id, PatientUpdateRequest request, long version, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, PatientDetailResponse?, string?)>((true, null, null));

        public Task<DuplicateCheckResponse?> CheckDuplicateAsync(DuplicatePatientCheckRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<DuplicateCheckResponse?>(null);
    }
}
