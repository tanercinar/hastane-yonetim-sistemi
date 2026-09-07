using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Web.Client.Pages.Specialty;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class DeliveryManagementComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F09-G02")]
    public void DeliveryManagementRendersDeliveriesNewbornsAndDisclaimerBanner()
    {
        var fakeSpecialtyApi = new FakeSpecialtyCareApiClient();
        Services.AddSingleton<ISpecialtyCareApiClient>(fakeSpecialtyApi);

        var cut = Render<DeliveryManagement>();

        // Verify Title and Disclaimer banner
        Assert.Contains("Doğum ve Yenidoğan Kayıt Panosu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klinik Simülasyon Bildirimi:", cut.Markup, StringComparison.Ordinal);

        // Verify Protocol & Delivery Mode
        Assert.Contains("DEMO-DEL-20260301-000001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Spontan Vajinal Doğum", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("39+3", cut.Markup, StringComparison.Ordinal);

        // Verify Newborn data
        Assert.Contains("3,450 g", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Kız", cut.Markup, StringComparison.Ordinal);

        // Verify Action Buttons
        Assert.Contains("Yeni Doğum Kaydı Başlat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Bebek Ekle", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Bebek Ekle", StringComparison.Ordinal))
            .Click();
        Assert.Contains("Yenidoğan Patient ID *", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("anne kaydından ayrı sentetik Patient kimliği", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSpecialtyCareApiClient : ISpecialtyCareApiClient
    {
        public Task<List<DeliveryRecordResponse>> GetDeliveryRecordsByMotherPatientIdAsync(Guid motherPatientId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<DeliveryRecordResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    PregnancyEpisodeId: Guid.NewGuid(),
                    MotherPatientId: motherPatientId,
                    EncounterId: null,
                    DeliveryProtocolNumber: "DEMO-DEL-20260301-000001",
                    DeliveryMode: "SpontaneousVaginal",
                    DeliveryTimeUtc: DateTime.UtcNow.AddDays(-5),
                    GestationalAgeWeeks: 39,
                    GestationalAgeDays: 3,
                    PerinealTear: "FirstDegree",
                    EstimatedBloodLossMl: 250,
                    AttendingDoctorId: Guid.NewGuid(),
                    AssistingMidwifeId: Guid.NewGuid(),
                    PediatricianDoctorId: null,
                    MaternalComplicationsNotes: null,
                    DeliverySummaryNotes: "Normal sorunsuz doğum",
                    CreatedAtUtc: DateTime.UtcNow,
                    UpdatedAtUtc: DateTime.UtcNow,
                    Newborns:
                    [
                        new NewbornResponse(
                            Id: Guid.NewGuid(),
                            DeliveryRecordId: Guid.NewGuid(),
                            NewbornPatientId: Guid.NewGuid(),
                            BirthOrder: 1,
                            BirthTimeUtc: DateTime.UtcNow.AddDays(-5),
                            Gender: "Female",
                            BirthWeightGrams: 3450,
                            BirthLengthCm: 50.0m,
                            HeadCircumferenceCm: 35.0m,
                            ApgarScore1Min: 8,
                            ApgarScore5Min: 9,
                            ApgarScore10Min: 10,
                            ResuscitationGiven: "None",
                            CordBloodPh: "7.36",
                            ComplicationsNotes: "Sağlıklı kız bebek",
                            CreatedAtUtc: DateTime.UtcNow)
                    ]),
            });
        }

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
