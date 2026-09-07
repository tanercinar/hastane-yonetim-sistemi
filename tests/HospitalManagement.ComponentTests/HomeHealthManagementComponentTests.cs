using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Web.Client.Pages.Specialty;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class HomeHealthManagementComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F09-G04")]
    public void HomeHealthManagementRendersPrivacyBannerTableAndControls()
    {
        var fakeSpecialtyApi = new FakeSpecialtyCareApiClient();
        Services.AddSingleton<ISpecialtyCareApiClient>(fakeSpecialtyApi);

        var cut = Render<HomeHealthManagement>();

        // Verify Title and Privacy Banner
        Assert.Contains("Evde Sağlık Planlama ve Ziyaret Yönetimi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Adres Gizliliği ve Minimum Konum Bildirimi:", cut.Markup, StringComparison.Ordinal);

        // Verify Filter Buttons
        Assert.Contains("Tümü", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Talep", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Atandı", cut.Markup, StringComparison.Ordinal);

        // Verify Action Button
        Assert.Contains("Yeni Ziyaret Talebi", cut.Markup, StringComparison.Ordinal);

        // Verify Visit Protocol and Data
        Assert.Contains("DEMO-HOM-20260301-000001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Yara Bakımı ve Pansuman", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("İstanbul / Kadıköy", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Tamamla", StringComparison.Ordinal))
            .Click();
        Assert.Contains("Evde Sağlık Encounter ID *", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("HomeHealth türünde", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSpecialtyCareApiClient : ISpecialtyCareApiClient
    {
        public Task<List<HomeHealthVisitResponse>> GetActiveHomeHealthVisitsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<HomeHealthVisitResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    PatientId: Guid.NewGuid(),
                    EncounterId: null,
                    ProtocolNumber: "DEMO-HOM-20260301-000001",
                    ServiceType: "WoundDressing",
                    Priority: "Urgent",
                    Status: "InProgress",
                    RequestedDateUtc: DateTime.UtcNow,
                    ScheduledDateUtc: null,
                    VisitStartedAtUtc: null,
                    VisitCompletedAtUtc: null,
                    City: "İstanbul",
                    District: "Kadıköy",
                    AddressDetail: "Moda Cad. No: 12",
                    ContactPhone: "0532 555 0122",
                    RequestedByStaffId: Guid.NewGuid(),
                    AssignedStaffId: null,
                    ClinicalNotes: "Diyabetik yara pansumanı",
                    VitalsSummaryNotes: null,
                    CreatedAtUtc: DateTime.UtcNow,
                    UpdatedAtUtc: DateTime.UtcNow)
            });
        }

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

        public Task<SpecialtyOperationalSummaryResponse> GetSpecialtyOperationalSummaryAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
