using Bunit;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Web.Client.Pages.Surgery;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class IcuFlowsheetComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F08-G07")]
    public void IcuFlowsheetRendersTableColumnsFluidSummaryAndNotice()
    {
        var admId = Guid.NewGuid();
        var fakeApi = new FakeIcuApiClient(admId);
        Services.AddSingleton<IIcuApiClient>(fakeApi);

        var cut = Render<IcuFlowsheet>(parameters => parameters.Add(p => p.AdmissionId, admId));

        // Verify Title and Sim notice
        Assert.Contains("Yoğun Bakım Akış Sayfası (ICU Flowsheet)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Eğitim &amp; Simülasyon Uyarısı:", cut.Markup, StringComparison.Ordinal);

        // Verify Fluid Summary
        Assert.Contains("IV Sıvı Girişi (24s)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Net Sıvı Dengesi (24s)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("+150 ml", cut.Markup, StringComparison.Ordinal);

        // Verify Table Headers & Content
        Assert.Contains("HR (bpm)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tansiyon (MAP)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Ventilasyon &amp; Parametreler", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("85 bpm", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("120/80", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("InvasiveMechanical", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeIcuApiClient : IIcuApiClient
    {
        private readonly Guid _admissionId;

        public FakeIcuApiClient(Guid admissionId)
        {
            _admissionId = admissionId;
        }

        public Task<IcuAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IcuAdmissionResponse?>(new(
                Id: _admissionId,
                AdmissionProtocolNumber: "DEMO-ICU-20260831-002001",
                InpatientStayId: Guid.NewGuid(),
                PatientId: Guid.NewGuid(),
                EncounterId: null,
                IcuBedId: Guid.NewGuid(),
                IcuBedCode: "DEMO-ICU-01",
                AttendingDoctorId: Guid.NewGuid(),
                PrimaryNurseId: null,
                AdmissionReason: "Post-Op ARDS",
                AcuityLevel: "Level3MultiOrganSupport",
                MonitoringFrequencyMinutes: 15,
                VentilationMode: "InvasiveMechanical",
                Status: "Active",
                CarePlanNotes: "Sedatize",
                AdmittedAtUtc: DateTime.UtcNow.AddHours(-10),
                DischargedAtUtc: null,
                DischargeNotes: null,
                CreatedAtUtc: DateTime.UtcNow.AddHours(-10),
                UpdatedAtUtc: DateTime.UtcNow.AddHours(-10),
                Version: 1));
        }

        public Task<List<IcuFlowsheetEntryResponse>> GetFlowsheetEntriesAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<IcuFlowsheetEntryResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    IcuAdmissionId: _admissionId,
                    RecordedAtUtc: DateTime.UtcNow.AddHours(-1),
                    RecordedByStaffId: Guid.NewGuid(),
                    HeartRateBpm: 85,
                    SystolicBpMmHg: 120,
                    DiastolicBpMmHg: 80,
                    MeanArterialPressureMmHg: 93,
                    RespiratoryRateBpm: 16,
                    OxygenSaturationPct: 98m,
                    BodyTemperatureCelsius: 37m,
                    GlasgowComaScale: 15,
                    RichmondAgitationSedationScale: 0,
                    VentilationMode: "InvasiveMechanical",
                    FractionOfInspiredOxygenPct: 40,
                    PositiveEndExpiratoryPressure: 5,
                    TidalVolumeMl: 450,
                    PeakInspiratoryPressure: 20,
                    IvFluidIntakeMl: 100,
                    EnteralNutritionIntakeMl: 50,
                    UrineOutputMl: 80,
                    DrainOutputMl: 10,
                    TotalIntakeMl: 150,
                    TotalOutputMl: 90,
                    NetFluidBalanceMl: 60,
                    ClinicalNotes: "Stabil",
                    CreatedAtUtc: DateTime.UtcNow.AddHours(-1)),
            });
        }

        public Task<IcuFluidBalanceSummaryResponse?> GetFluidBalanceSummaryAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IcuFluidBalanceSummaryResponse?>(new(
                IcuAdmissionId: _admissionId,
                FromUtc: DateTime.UtcNow.AddHours(-24),
                ToUtc: DateTime.UtcNow,
                TotalIvIntakeMl: 200,
                TotalEnteralIntakeMl: 100,
                TotalIntakeMl: 300,
                TotalUrineOutputMl: 120,
                TotalDrainOutputMl: 30,
                TotalOutputMl: 150,
                NetBalanceMl: 150,
                EntryCount: 1));
        }

        public Task<List<IcuBedResponse>> GetIcuBedsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<IcuBedResponse>());
        public Task<List<IcuAdmissionResponse>> GetActiveAdmissionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<IcuAdmissionResponse>());
        public Task<IcuAdmissionResponse?> AdmitToIcuAsync(CreateIcuAdmissionRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IcuAdmissionResponse?>(null);
        public Task<IcuAdmissionResponse?> UpdateCarePlanAsync(Guid id, UpdateIcuCarePlanRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IcuAdmissionResponse?>(null);
        public Task<IcuAdmissionResponse?> DischargeOrTransferAsync(Guid id, IcuDischargeOrTransferRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IcuAdmissionResponse?>(null);
        public Task<IcuFlowsheetEntryResponse?> AddFlowsheetEntryAsync(Guid admissionId, CreateIcuFlowsheetEntryRequest request, CancellationToken cancellationToken = default) => Task.FromResult<IcuFlowsheetEntryResponse?>(null);
    }
}
