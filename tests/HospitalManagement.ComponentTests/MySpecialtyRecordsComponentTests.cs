using System.Net;
using Bunit;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.UI.Components;
using HospitalManagement.Web.Client.Pages.Patient;
using HospitalManagement.Web.Client.Specialty;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.ComponentTests;

public sealed class MySpecialtyRecordsComponentTests : BunitContext
{
    public MySpecialtyRecordsComponentTests()
    {
        Services.AddLocalization();
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F09-G05")]
    public void PublishedSummariesRenderWithoutInternalClinicalContent()
    {
        Services.AddSingleton<IPatientSpecialtyPortalApiClient>(new SuccessfulPortalClient());

        var cut = Render<MySpecialtyRecords>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("DEMO-OBS-PORTAL", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Kompozit Dolgu", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Kadıköy", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-GIZLI-KLINIK-NOT", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F09-G05")]
    public void ApiFailureRendersSafeRetryableError()
    {
        Services.AddSingleton<IPatientSpecialtyPortalApiClient>(new FailingPortalClient());

        var cut = Render<MySpecialtyRecords>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Uzmanlık kayıtları yüklenemedi", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F09-KAPI")]
    public void ForbiddenApiResponseRendersDedicatedStateWithoutResourceDetails()
    {
        Services.AddSingleton<IPatientSpecialtyPortalApiClient>(new ForbiddenPortalClient());

        var cut = Render<MySpecialtyRecords>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.FindComponent<ForbiddenState>());
            Assert.Contains("role=\"status\"", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-GIZLI-KLINIK-NOT", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Yayınlanmış uzmanlık kaydı bulunamadı", cut.Markup, StringComparison.Ordinal);
        });
    }

    private sealed class SuccessfulPortalClient : IPatientSpecialtyPortalApiClient
    {
        public Task<PatientSpecialtyPortalResponse> GetMyPublishedRecordsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new PatientSpecialtyPortalResponse(
                [new(Guid.NewGuid(), "DEMO-OBS-PORTAL", "Active", DateTime.UtcNow.AddMonths(3), 2, DateTime.UtcNow.AddDays(-7))],
                [],
                [],
                [new(Guid.NewGuid(), "DEMO-DNT-PORTAL", 16, "Kompozit Dolgu", "Completed", DateTime.UtcNow)],
                [new(Guid.NewGuid(), "DEMO-HOM-PORTAL", "WoundDressing", "Normal", "Assigned", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, "İstanbul", "Kadıköy")],
                DateTime.UtcNow));
    }

    private sealed class FailingPortalClient : IPatientSpecialtyPortalApiClient
    {
        public Task<PatientSpecialtyPortalResponse> GetMyPublishedRecordsAsync(CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("DEMO-GIZLI-KLINIK-NOT");
    }

    private sealed class ForbiddenPortalClient : IPatientSpecialtyPortalApiClient
    {
        public Task<PatientSpecialtyPortalResponse> GetMyPublishedRecordsAsync(CancellationToken cancellationToken = default) =>
            throw new HttpRequestException(
                "DEMO-GIZLI-KLINIK-NOT",
                inner: null,
                HttpStatusCode.Forbidden);
    }
}
