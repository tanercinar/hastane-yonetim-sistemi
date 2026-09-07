using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Web.Client.Pages.Specialty;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class SpecialtyReportsComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F09-G05")]
    public void SpecialtyReportsRendersKPICardsPrivacyBannerAndControls()
    {
        var fakeSpecialtyApi = new FakeSpecialtyCareApiClient();
        Services.AddSingleton<ISpecialtyCareApiClient>(fakeSpecialtyApi);

        var cut = Render<SpecialtyReports>();

        // Verify Title and Privacy Banner
        Assert.Contains("Uzmanlık Operasyonel Göstergeleri ve Raporlar", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Gizlilik ve Güvenlik Güvencesi:", cut.Markup, StringComparison.Ordinal);

        // Verify Section Headers
        Assert.Contains("Kadın Doğum ve Gebelik Göstergeleri", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Diş Hekimliği ve Tedavi Göstergeleri", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Evde Sağlık Ziyaret Göstergeleri", cut.Markup, StringComparison.Ordinal);

        // Verify KPI Metrics
        Assert.Contains("15", cut.Markup, StringComparison.Ordinal); // Active Pregnancies
        Assert.Contains("42", cut.Markup, StringComparison.Ordinal); // Total Deliveries
        Assert.Contains("120", cut.Markup, StringComparison.Ordinal); // Dental Procedures
        Assert.Contains("18", cut.Markup, StringComparison.Ordinal); // Active Home Visits

        // Verify Action Button
        Assert.Contains("Verileri Yenile", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSpecialtyCareApiClient : ISpecialtyCareApiClient
    {
        public Task<SpecialtyOperationalSummaryResponse> GetSpecialtyOperationalSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SpecialtyOperationalSummaryResponse(
                ActivePregnanciesCount: 15,
                HighRiskPregnanciesCount: 3,
                TotalDeliveriesCount: 42,
                CesareanDeliveriesCount: 14,
                NormalDeliveriesCount: 28,
                TotalDentalProceduresCount: 120,
                CompletedDentalProceduresCount: 95,
                PlannedDentalProceduresCount: 25,
                TotalDentalExaminationsCount: 88,
                ActiveHomeVisitsCount: 18,
                PendingHomeVisitRequestsCount: 5,
                AssignedHomeVisitsCount: 7,
                CompletedHomeVisitsCount: 34,
                UrgentHomeVisitsCount: 2,
                GeneratedAtUtc: DateTime.UtcNow));
        }

        public Task<List<HomeHealthVisitResponse>> GetActiveHomeHealthVisitsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HomeHealthVisitResponse>());

        public Task<HomeHealthVisitResponse?> GetHomeHealthVisitByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<HomeHealthVisitResponse?>(null);

        public Task<List<HomeHealthVisitResponse>> GetHomeHealthVisitsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HomeHealthVisitResponse>());

        public Task<HomeHealthVisitResponse> RequestHomeHealthVisitAsync(RequestHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> AssignHomeHealthTeamAsync(Guid id, AssignHomeHealthTeamRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> StartHomeHealthVisitAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> CompleteHomeHealthVisitAsync(Guid id, CompleteHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> CancelHomeHealthVisitAsync(Guid id, CancelHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<ToothConditionResponse>> GetLatestOdontogramAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ToothConditionResponse>());

        public Task<List<ToothConditionResponse>> GetToothHistoryAsync(Guid patientId, int toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ToothConditionResponse>());

        public Task<ToothConditionResponse> RecordToothConditionAsync(Guid patientId, RecordToothConditionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<DentalProcedureResponse>> GetDentalProceduresAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DentalProcedureResponse>());

        public Task<DentalProcedureResponse> PlanDentalProcedureAsync(PlanDentalProcedureRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<DentalProcedureResponse> CompleteDentalProcedureAsync(Guid procedureId, CompleteDentalProcedureRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<DentalExaminationResponse>> GetDentalExaminationsAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DentalExaminationResponse>());

        public Task<DentalExaminationResponse> CreateDentalExaminationAsync(CreateDentalExaminationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<DeliveryRecordResponse>> GetDeliveryRecordsByMotherPatientIdAsync(Guid motherPatientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DeliveryRecordResponse>());

        public Task<DeliveryRecordResponse?> GetDeliveryRecordByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DeliveryRecordResponse?>(null);

        public Task<DeliveryRecordResponse> CreateDeliveryRecordAsync(CreateDeliveryRecordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<NewbornResponse> AddNewbornAsync(Guid deliveryId, AddNewbornRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<PregnancyEpisodeResponse>> GetActivePregnancyEpisodesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PregnancyEpisodeResponse>());

        public Task<List<PregnancyEpisodeResponse>> GetPregnancyEpisodesByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PregnancyEpisodeResponse>());

        public Task<PregnancyEpisodeResponse?> GetPregnancyEpisodeByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<PregnancyEpisodeResponse?>(null);

        public Task<PregnancyEpisodeResponse> CreatePregnancyEpisodeAsync(CreatePregnancyEpisodeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AntenatalVisitResponse> RecordAntenatalVisitAsync(Guid episodeId, RecordAntenatalVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PregnancyEpisodeResponse> UpdatePregnancyRiskCategoryAsync(Guid episodeId, UpdatePregnancyRiskCategoryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PregnancyEpisodeResponse> CompletePregnancyEpisodeAsync(Guid episodeId, CompletePregnancyEpisodeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
