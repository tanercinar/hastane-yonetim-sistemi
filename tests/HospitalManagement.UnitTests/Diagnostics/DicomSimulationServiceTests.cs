using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class DicomSimulationServiceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G07")]
    public async Task RenderPreviewImageWithValidTokenReturnsSvgContentAndWatermark()
    {
        var options = new DbContextOptionsBuilder<DiagnosticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new DiagnosticsDbContext(options);
        var timeProvider = TimeProvider.System;
        var audit = new FakeAuditPublisher();
        var service = new DicomSimulationService(
            db,
            audit,
            timeProvider,
            new PermitAllDiagnosticsAccessContext(),
            new DicomPreviewTokenProtector());

        var patientId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var study = RadiologyStudy.Create(
            Guid.NewGuid(),
            orderId,
            orderItemId,
            patientId,
            "DEMO-ACC-20260830-999888",
            RadiologyModality.CT,
            "DEMO-RAD-THORAX-CT",
            "Toraks BT",
            "Toraks",
            DateTime.UtcNow);

        study.Schedule(DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);
        study.CompleteAcquisition(Guid.NewGuid(), null, DateTime.UtcNow);
        study.FinalizeReport(Guid.NewGuid(), "Rapor", "Kanaat", DateTime.UtcNow);
        var order = DiagnosticOrder.CreateDraft(
            orderId,
            "DEMO-RAD-ORDER-1",
            patientId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Radiology,
            DiagnosticOrderPriority.Routine,
            "DEMO",
            null,
            DateTime.UtcNow);
        order.AddItem(orderItemId, "DEMO-RAD-THORAX-CT", "Toraks BT", "Radiology", null, DateTime.UtcNow);
        order.Place(Guid.NewGuid(), DateTime.UtcNow);
        db.DiagnosticOrders.Add(order);
        db.RadiologyStudies.Add(study);
        await db.SaveChangesAsync();

        var radActor = CreateStaffPrincipal(Guid.NewGuid(), "RAD");
        var metaResult = await service.GetStudyMetadataAsync(radActor, study.Id);

        Assert.True(metaResult.Succeeded);
        var firstInstance = metaResult.Value!.Series[0].Instances[0];
        Assert.NotNull(firstInstance.ViewToken);

        var renderResult = await service.RenderPreviewImageAsync(radActor, firstInstance.ViewToken);

        Assert.True(renderResult.Succeeded);
        Assert.Equal("image/svg+xml", renderResult.Value.ContentType);
        var svgStr = System.Text.Encoding.UTF8.GetString(renderResult.Value.Content);
        Assert.Contains("MOCK DICOM PREVIEW", svgStr, StringComparison.Ordinal);
        Assert.Contains("SENTETIK PACS SIMULASYONU", svgStr, StringComparison.Ordinal);
        Assert.Contains("DEMO-ACC-20260830-999888", svgStr, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G07")]
    public async Task RenderPreviewImageWithTamperedTokenReturnsValidationFailure()
    {
        var options = new DbContextOptionsBuilder<DiagnosticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new DiagnosticsDbContext(options);
        var timeProvider = TimeProvider.System;
        var audit = new FakeAuditPublisher();
        var service = new DicomSimulationService(
            db,
            audit,
            timeProvider,
            new PermitAllDiagnosticsAccessContext(),
            new DicomPreviewTokenProtector());

        var fakeToken = "eyJTdHVk...tampered";
        var result = await service.RenderPreviewImageAsync(CreateStaffPrincipal(Guid.NewGuid(), "RAD"), fakeToken);

        Assert.False(result.Succeeded);
        Assert.Equal(DiagnosticOperationStatus.ValidationFailed, result.Status);
    }

    private static ClaimsPrincipal CreateStaffPrincipal(Guid userId, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(HospitalClaimTypes.PersonId, userId.ToString()),
            ],
            "TestAuth"));

    private sealed class FakeAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> Events { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class PermitAllDiagnosticsAccessContext : IDiagnosticsAccessContext
    {
        public Task<DiagnosticEncounterContext?> FindEncounterAsync(
            Guid encounterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DiagnosticEncounterContext?>(null);

        public Task<bool> CanAccessEncounterAsync(
            ClaimsPrincipal actor,
            DiagnosticEncounterContext encounter,
            string permission,
            bool allowPatientOwnRecord,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> CanAccessPatientAsync(
            ClaimsPrincipal actor,
            Guid patientId,
            string permission,
            bool allowPatientOwnRecord,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> CanAccessResourceAsync(
            ClaimsPrincipal actor,
            DiagnosticResourceContext resource,
            string permission,
            bool allowPatientOwnFinalResult,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> CanAccessDiagnosticAreaAsync(
            ClaimsPrincipal actor,
            DiagnosticOrderType orderType,
            string permission,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
