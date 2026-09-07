using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Web.Client.Pages.Specialty;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class PregnancyTrackingComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F09-G01")]
    public void PregnancyTrackingRendersActiveEpisodesVisitsAndDisclaimerBanner()
    {
        var fakeSpecialtyApi = new FakeSpecialtyCareApiClient();
        Services.AddSingleton<ISpecialtyCareApiClient>(fakeSpecialtyApi);

        var cut = Render<PregnancyTracking>();

        // Verify Title and Disclaimer banner
        Assert.Contains("Gebelik ve Antenatal Takip Panosu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klinik Simülasyon Bildirimi:", cut.Markup, StringComparison.Ordinal);

        // Verify Protocol & Active Episode
        Assert.Contains("DEMO-OBS-20260301-000001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("G2 P1 A0 L1", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Düşük Risk", cut.Markup, StringComparison.Ordinal);

        // Verify Action Buttons
        Assert.Contains("Yeni Gebelik Takibi Başlat", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Yeni Gebelik Takibi Başlat", StringComparison.Ordinal))
            .Click();
        Assert.Contains("Başlangıç Karşılaşma ID *", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("DEMO-OBS-20260301-000001", StringComparison.Ordinal))
            .Click();
        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Vizit Ekle", StringComparison.Ordinal))
            .Click();
        Assert.Contains("Karşılaşma ID *", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSpecialtyCareApiClient : ISpecialtyCareApiClient
    {
        public Task<List<PregnancyEpisodeResponse>> GetActivePregnancyEpisodesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<PregnancyEpisodeResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    PatientId: Guid.NewGuid(),
                    OpeningEncounterId: Guid.NewGuid(),
                    EpisodeProtocolNumber: "DEMO-OBS-20260301-000001",
                    Gravida: 2,
                    Para: 1,
                    Abortus: 0,
                    LivingChildren: 1,
                    LastMenstrualPeriodUtc: DateTime.UtcNow.AddDays(-100),
                    EstimatedDeliveryDateUtc: DateTime.UtcNow.AddDays(180),
                    BloodGroupAndRh: "A Rh(+)",
                    RiskCategory: "LowRisk",
                    RiskFactorsNotes: "Normal takip",
                    Status: "Active",
                    AssignedDoctorId: Guid.NewGuid(),
                    AssignedMidwifeId: null,
                    CreatedAtUtc: DateTime.UtcNow,
                    UpdatedAtUtc: DateTime.UtcNow,
                    AntenatalVisits:
                    [
                        new AntenatalVisitResponse(
                            Id: Guid.NewGuid(),
                            PregnancyEpisodeId: Guid.NewGuid(),
                            EncounterId: Guid.NewGuid(),
                            VisitDateUtc: DateTime.UtcNow.AddDays(-10),
                            GestationalAgeWeeks: 12,
                            GestationalAgeDays: 4,
                            MaternalWeightKg: 64.0m,
                            SystolicBpMmHg: 115,
                            DiastolicBpMmHg: 75,
                            FundalHeightCm: 12.0m,
                            FetalHeartRateBpm: 155,
                            FetalPresentation: "Undetermined",
                            EdemaLevel: "None",
                            UrineProteinPresent: false,
                            UrineGlucosePresent: false,
                            StaffId: Guid.NewGuid(),
                            ClinicalNotes: "Fetal hareketler mevcut",
                            NextVisitRecommendedDateUtc: DateTime.UtcNow.AddDays(18),
                            CreatedAtUtc: DateTime.UtcNow)
                    ]),
            });
        }

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

        public Task<DeliveryRecordResponse> CreateDeliveryRecordAsync(CreateDeliveryRecordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<DeliveryRecordResponse?> GetDeliveryRecordByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<DeliveryRecordResponse?>(null);

        public Task<List<DeliveryRecordResponse>> GetDeliveryRecordsByMotherPatientIdAsync(Guid motherPatientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DeliveryRecordResponse>());

        public Task<NewbornResponse> AddNewbornAsync(Guid deliveryId, AddNewbornRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<ToothConditionResponse>> GetLatestOdontogramAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ToothConditionResponse>());

        public Task<ToothConditionResponse> RecordToothConditionAsync(Guid patientId, RecordToothConditionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<ToothConditionResponse>> GetToothHistoryAsync(Guid patientId, int toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ToothConditionResponse>());

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

        public Task<HomeHealthVisitResponse> RequestHomeHealthVisitAsync(RequestHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<HomeHealthVisitResponse>> GetActiveHomeHealthVisitsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HomeHealthVisitResponse>());

        public Task<HomeHealthVisitResponse?> GetHomeHealthVisitByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<HomeHealthVisitResponse?>(null);

        public Task<List<HomeHealthVisitResponse>> GetHomeHealthVisitsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HomeHealthVisitResponse>());

        public Task<HomeHealthVisitResponse> AssignHomeHealthTeamAsync(Guid id, AssignHomeHealthTeamRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> StartHomeHealthVisitAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> CompleteHomeHealthVisitAsync(Guid id, CompleteHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<HomeHealthVisitResponse> CancelHomeHealthVisitAsync(Guid id, CancelHomeHealthVisitRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<SpecialtyOperationalSummaryResponse> GetSpecialtyOperationalSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
