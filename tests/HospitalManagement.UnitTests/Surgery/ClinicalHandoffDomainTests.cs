using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Surgery;

public sealed class ClinicalHandoffDomainTests
{
    private readonly DbContextOptions<SurgeryDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public ClinicalHandoffDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<SurgeryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    [Trait("Roadmap", "F08-G08")]
    public void ClinicalHandoffInitiationAndIsbarValidationWorkProperly()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var staff1 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var handoff = ClinicalHandoff.Initiate(
            id,
            patientId,
            null,
            null,
            ClinicalAreaType.Emergency,
            "Kırmızı Alan Yatak 1",
            ClinicalAreaType.IntensiveCareUnit,
            "ICU Yatak 2",
            staff1,
            situation: "Perfore apandisit ve septik şok tablosu",
            background: "DM, HT, penisilin alerjisi",
            assessment: "TA: 90/55, HR: 115, SpO2: 94, Laktat: 3.2",
            recommendation: "Acil YBÜ takibi ve kan gazı kontrolü",
            criticalAlerts: "Temas izolasyonu (MRSA+)",
            nowUtc: now);

        Assert.Equal(id, handoff.Id);
        Assert.StartsWith("DEMO-HOF-", handoff.HandoffProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(HandoffStatus.PendingAcceptance, handoff.Status);
        Assert.Equal(staff1, handoff.HandingOverStaffId);
        Assert.Null(handoff.ReceivingStaffId);

        // Missing Situation throws ArgumentException
        Assert.Throws<ArgumentException>(() => ClinicalHandoff.Initiate(
            Guid.NewGuid(), patientId, null, null,
            ClinicalAreaType.Emergency, "Acil", ClinicalAreaType.InpatientWard, "Servis", staff1,
            situation: "", background: "Bg", assessment: "Ass", recommendation: "Rec", null, now));
    }

    [Fact]
    [Trait("Roadmap", "F08-G08")]
    public void ClinicalHandoffPreventsSelfAcceptanceAndEnforcesCrossTeamApproval()
    {
        var staff1 = Guid.NewGuid();
        var staff2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var handoff = ClinicalHandoff.Initiate(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            ClinicalAreaType.Emergency, "Acil", ClinicalAreaType.IntensiveCareUnit, "ICU", staff1,
            "S", "B", "A", "R", null, now);

        // Staff 1 (handing over staff) cannot self-accept -> throws InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => handoff.Accept(staff1, "Kendi kendime onaylıyorum", now.AddMinutes(5)));

        // Staff 2 (receiving staff) accepts successfully
        handoff.Accept(staff2, "Hasta stabil teslim alındı.", now.AddMinutes(10));
        Assert.Equal(HandoffStatus.Accepted, handoff.Status);
        Assert.Equal(staff2, handoff.ReceivingStaffId);
        Assert.NotNull(handoff.AcceptedAtUtc);
    }

    [Fact]
    [Trait("Roadmap", "F08-G08")]
    public void ClinicalHandoffRejectionAndCancellationBehaveCorrectly()
    {
        var staff1 = Guid.NewGuid();
        var staff2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // 1. Rejection
        var handoff1 = ClinicalHandoff.Initiate(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            ClinicalAreaType.Emergency, "Acil", ClinicalAreaType.IntensiveCareUnit, "ICU", staff1,
            "S", "B", "A", "R", null, now);

        handoff1.Reject(staff2, "Eksik tetkik var, stabilizasyon gerekli", now.AddMinutes(5));
        Assert.Equal(HandoffStatus.Rejected, handoff1.Status);
        Assert.Equal("Eksik tetkik var, stabilizasyon gerekli", handoff1.StatusReason);

        // 2. Cancellation by initiating staff
        var handoff2 = ClinicalHandoff.Initiate(
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            ClinicalAreaType.Emergency, "Acil", ClinicalAreaType.InpatientWard, "Servis", staff1,
            "S", "B", "A", "R", null, now);

        // Non-initiating staff cannot cancel
        Assert.Throws<InvalidOperationException>(() => handoff2.Cancel(staff2, "İptal", now.AddMinutes(5)));

        // Initiating staff can cancel
        handoff2.Cancel(staff1, "Hasta taburcu oldu, transfere gerek kalmadı", now.AddMinutes(5));
        Assert.Equal(HandoffStatus.Cancelled, handoff2.Status);
    }

    [Fact]
    [Trait("Roadmap", "F08-G08")]
    public async Task ClinicalHandoffServicePreventsDuplicatePendingHandoffForSamePatient()
    {
        var patientId = Guid.NewGuid();
        var staff1 = Guid.NewGuid();
        var staff2 = Guid.NewGuid();

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new ClinicalHandoffService(db, _auditPublisher, _timeProvider);

            var dto = new InitiateClinicalHandoffDto(
                patientId,
                null,
                null,
                ClinicalAreaType.Emergency,
                "Kırmızı Alan",
                ClinicalAreaType.IntensiveCareUnit,
                "ICU 1",
                "Situation",
                "Background",
                "Assessment",
                "Recommendation",
                null);

            var res1 = await service.InitiateHandoffAsync(dto, staff1);
            Assert.True(res1.IsSuccess);

            // Second pending handoff for same patient -> 409 Conflict
            var res2 = await service.InitiateHandoffAsync(dto, staff2);
            Assert.False(res2.IsSuccess);
            Assert.Equal(SurgeryOperationStatus.Conflict, res2.Status);
        }
    }

    private sealed class FakeAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
