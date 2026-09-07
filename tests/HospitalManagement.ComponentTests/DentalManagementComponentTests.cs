using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Web.Client.Pages.Specialty;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class DentalManagementComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F09-G03")]
    public void DentalManagementRendersOdontogramTeethDisclaimerAndControls()
    {
        var fakeSpecialtyApi = new FakeSpecialtyCareApiClient();
        Services.AddSingleton<ISpecialtyCareApiClient>(fakeSpecialtyApi);

        var cut = Render<DentalManagement>();

        // Verify Title and Disclaimer
        Assert.Contains("Diş Hekimliği ve Odontogram Panosu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klinik Simülasyon Bildirimi:", cut.Markup, StringComparison.Ordinal);

        // Verify Odontogram Section
        Assert.Contains("Etkileşimli Odontogram", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Üst Çene (Maksilla)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Alt Çene (Mandibula)", cut.Markup, StringComparison.Ordinal);

        // Verify Tooth Buttons FDI numbers (18, 16, 21, 36, 48)
        Assert.Contains("Diş 16", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Diş 21", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Diş 48", cut.Markup, StringComparison.Ordinal);

        // Verify Procedure in table
        Assert.Contains("DEMO-DNT-20260301-000001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Kompozit Dolgu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("750", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("₺", cut.Markup, StringComparison.Ordinal);

        // Verify Action Buttons
        Assert.Contains("Yeni Muayene", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tedavi / İşlem Planla", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeSpecialtyCareApiClient : ISpecialtyCareApiClient
    {
        public Task<List<ToothConditionResponse>> GetLatestOdontogramAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<ToothConditionResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    PatientId: patientId,
                    ToothNumber: 16,
                    Condition: "Caries",
                    AffectedSurfaces: 4,
                    Notes: "Oklüzal derin çürük",
                    RecordedAtUtc: DateTime.UtcNow.AddDays(-3),
                    RecordedByStaffId: Guid.NewGuid(),
                    Version: 1),
                new(
                    Id: Guid.NewGuid(),
                    PatientId: patientId,
                    ToothNumber: 21,
                    Condition: "Filled",
                    AffectedSurfaces: 1,
                    Notes: "Eski kompozit dolgu",
                    RecordedAtUtc: DateTime.UtcNow.AddDays(-10),
                    RecordedByStaffId: Guid.NewGuid(),
                    Version: 2)
            });
        }

        public Task<List<ToothConditionResponse>> GetToothHistoryAsync(Guid patientId, int toothNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ToothConditionResponse>());

        public Task<ToothConditionResponse> RecordToothConditionAsync(Guid patientId, RecordToothConditionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<DentalProcedureResponse>> GetDentalProceduresAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<DentalProcedureResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    PatientId: patientId,
                    EncounterId: null,
                    ProcedureProtocolNumber: "DEMO-DNT-20260301-000001",
                    ToothNumber: 16,
                    Surfaces: 4,
                    ProcedureCode: "DNT-FILLING",
                    ProcedureName: "Kompozit Dolgu",
                    Status: "Planned",
                    EstimatedCost: 750,
                    PerformedByDoctorId: Guid.NewGuid(),
                    ScheduledDateUtc: DateTime.UtcNow.AddDays(2),
                    CompletedDateUtc: null,
                    ClinicalNotes: "16 no'lu diş oklüzal dolgu",
                    CreatedAtUtc: DateTime.UtcNow,
                    UpdatedAtUtc: DateTime.UtcNow)
            });
        }

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
