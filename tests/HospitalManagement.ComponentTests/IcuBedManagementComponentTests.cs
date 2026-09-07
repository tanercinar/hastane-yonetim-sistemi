using Bunit;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Web.Client.Pages.Surgery;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class IcuBedManagementComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F08-G06")]
    public void IcuBedManagementRendersBedsAcuityAndSummaryCounters()
    {
        var fakeApi = new FakeIcuApiClient();
        Services.AddSingleton<IIcuApiClient>(fakeApi);

        var cut = Render<IcuBedManagement>();

        // Verify Title and KPI labels
        Assert.Contains("Yoğun Bakım Ünitesi &amp; Yatak Yönetimi", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Toplam ICU Yatak", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Dolu Yatak", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Müsait Yatak", cut.Markup, StringComparison.Ordinal);

        // Verify Bed Codes rendered
        Assert.Contains("DEMO-ICU-01", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-ICU-02", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("DEMO-ICU-03", cut.Markup, StringComparison.Ordinal);

        // Verify Active Admission details
        Assert.Contains("DEMO-ICU-20260831-001001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Akut Solunum Sıkıntısı Sendromu (ARDS)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Basamak 3 (Organ Desteği)", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("İnvaziv Mekanik", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeIcuApiClient : IIcuApiClient
    {
        private readonly Guid _bed1Id = Guid.Parse("00000000-0000-0000-0000-000000000501");
        private readonly Guid _bed2Id = Guid.Parse("00000000-0000-0000-0000-000000000502");
        private readonly Guid _bed3Id = Guid.Parse("00000000-0000-0000-0000-000000000503");
        private readonly Guid _admId = Guid.NewGuid();

        public Task<List<IcuBedResponse>> GetIcuBedsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<IcuBedResponse>
            {
                new(_bed1Id, "DEMO-ICU-01", "Yoğun Bakım Yatak 1", "Genel Yoğun Bakım Ünitesi", true, true, _admId, "DEMO-ICU-20260831-001001"),
                new(_bed2Id, "DEMO-ICU-02", "Yoğun Bakım Yatak 2", "Genel Yoğun Bakım Ünitesi", true, false, null, null),
                new(_bed3Id, "DEMO-ICU-03", "Yoğun Bakım Yatak 3", "Genel Yoğun Bakım Ünitesi", true, false, null, null),
            });
        }

        public Task<List<IcuAdmissionResponse>> GetActiveAdmissionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<IcuAdmissionResponse>
            {
                new(
                    Id: _admId,
                    AdmissionProtocolNumber: "DEMO-ICU-20260831-001001",
                    InpatientStayId: Guid.NewGuid(),
                    PatientId: Guid.NewGuid(),
                    EncounterId: null,
                    IcuBedId: _bed1Id,
                    IcuBedCode: "DEMO-ICU-01",
                    AttendingDoctorId: Guid.NewGuid(),
                    PrimaryNurseId: null,
                    AdmissionReason: "Akut Solunum Sıkıntısı Sendromu (ARDS)",
                    AcuityLevel: "Level3MultiOrganSupport",
                    MonitoringFrequencyMinutes: 15,
                    VentilationMode: "InvasiveMechanical",
                    Status: "Active",
                    CarePlanNotes: "Sedasyon altında, PEEP 10 cmH2O",
                    AdmittedAtUtc: DateTime.UtcNow.AddHours(-12),
                    DischargedAtUtc: null,
                    DischargeNotes: null,
                    CreatedAtUtc: DateTime.UtcNow.AddHours(-12),
                    UpdatedAtUtc: DateTime.UtcNow.AddHours(-12),
                    Version: 1),
            });
        }

        public Task<IcuAdmissionResponse?> GetAdmissionByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuAdmissionResponse?>(null);

        public Task<IcuAdmissionResponse?> AdmitToIcuAsync(CreateIcuAdmissionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuAdmissionResponse?>(null);

        public Task<IcuAdmissionResponse?> UpdateCarePlanAsync(Guid id, UpdateIcuCarePlanRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuAdmissionResponse?>(null);

        public Task<IcuAdmissionResponse?> DischargeOrTransferAsync(Guid id, IcuDischargeOrTransferRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuAdmissionResponse?>(null);

        public Task<IcuFlowsheetEntryResponse?> AddFlowsheetEntryAsync(Guid admissionId, CreateIcuFlowsheetEntryRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuFlowsheetEntryResponse?>(null);

        public Task<List<IcuFlowsheetEntryResponse>> GetFlowsheetEntriesAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<IcuFlowsheetEntryResponse>());

        public Task<IcuFluidBalanceSummaryResponse?> GetFluidBalanceSummaryAsync(Guid admissionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IcuFluidBalanceSummaryResponse?>(null);
    }
}
