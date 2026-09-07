using Bunit;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Web.Client.Pages.Surgery;
using HospitalManagement.Web.Client.Surgery;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.ComponentTests;

public sealed class ClinicalHandoffComponentTests : BunitContext
{
    [Fact]
    [Trait("Roadmap", "F08-G08")]
    public void ClinicalHandoffRendersPendingCardsIsbarAndSafetyBanner()
    {
        var fakeApi = new FakeClinicalHandoffApiClient();
        Services.AddSingleton<IClinicalHandoffApiClient>(fakeApi);

        var cut = Render<ClinicalHandoffManagement>();

        // Verify Title and Safety banner
        Assert.Contains("Klinik Devir Teslim &amp; ISBAR Panosu", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Klinik Sorumluluk &amp; Sahiplik İlkesi:", cut.Markup, StringComparison.Ordinal);

        // Verify Handoff Protocol & ISBAR sections
        Assert.Contains("DEMO-HOF-20260831-003001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("S - Durum (Situation):", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("B - Geçmiş (Background):", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("A - Değerlendirme &amp; Bulgular (Assessment):", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("R - Öneriler &amp; Açık Görevler (Recommendation):", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Akut Solunum Yetmezliği", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Zor Hava Yolu", cut.Markup, StringComparison.Ordinal);

        // Verify Action Buttons
        Assert.Contains("Kabul Et &amp; Devral", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Reddet / Revizyon İste", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FakeClinicalHandoffApiClient : IClinicalHandoffApiClient
    {
        public Task<List<ClinicalHandoffResponse>> GetPendingHandoffsAsync(string? destinationArea = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<ClinicalHandoffResponse>
            {
                new(
                    Id: Guid.NewGuid(),
                    HandoffProtocolNumber: "DEMO-HOF-20260831-003001",
                    PatientId: Guid.NewGuid(),
                    InpatientStayId: null,
                    EncounterId: null,
                    SourceArea: "Emergency",
                    SourceLocationDetails: "Kırmızı Alan Yatak 1",
                    DestinationArea: "IntensiveCareUnit",
                    DestinationLocationDetails: "ICU Yatak 3",
                    HandingOverStaffId: Guid.NewGuid(),
                    ReceivingStaffId: null,
                    Situation: "Akut Solunum Yetmezliği",
                    Background: "KOAH alevlenme, sigara öyküsü",
                    Assessment: "TA: 130/85, SpO2: 88, PEEP: 8 cmH2O",
                    Recommendation: "Yakın kan gazı takibi ve sedasyon",
                    CriticalAlerts: "Zor Hava Yolu",
                    Status: "PendingAcceptance",
                    StatusReason: null,
                    HandedOverAtUtc: DateTime.UtcNow.AddMinutes(-15),
                    AcceptedAtUtc: null,
                    CreatedAtUtc: DateTime.UtcNow.AddMinutes(-15),
                    UpdatedAtUtc: DateTime.UtcNow.AddMinutes(-15),
                    Version: 1),
            });
        }

        public Task<List<ClinicalHandoffResponse>> GetHandoffsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ClinicalHandoffResponse>());

        public Task<ClinicalHandoffResponse?> GetHandoffByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClinicalHandoffResponse?>(null);

        public Task<ClinicalHandoffResponse?> InitiateHandoffAsync(InitiateClinicalHandoffRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClinicalHandoffResponse?>(null);

        public Task<ClinicalHandoffResponse?> AcceptHandoffAsync(Guid id, AcceptClinicalHandoffRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClinicalHandoffResponse?>(null);

        public Task<ClinicalHandoffResponse?> RejectHandoffAsync(Guid id, RejectClinicalHandoffRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClinicalHandoffResponse?>(null);

        public Task<ClinicalHandoffResponse?> CancelHandoffAsync(Guid id, CancelClinicalHandoffRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<ClinicalHandoffResponse?>(null);
    }
}
