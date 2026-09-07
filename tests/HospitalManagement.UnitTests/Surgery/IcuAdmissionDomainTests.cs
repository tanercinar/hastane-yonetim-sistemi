using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Surgery;

public sealed class IcuAdmissionDomainTests
{
    private readonly DbContextOptions<SurgeryDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public IcuAdmissionDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<SurgeryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    [Trait("Roadmap", "F08-G06")]
    public void IcuAdmissionCreationAndCarePlanTransitionsWorkAsExpected()
    {
        var id = Guid.NewGuid();
        var stayId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var bedId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var admission = IcuAdmission.Create(
            id,
            stayId,
            patientId,
            null,
            bedId,
            "DEMO-ICU-01",
            doctorId,
            null,
            "Post-op septik şok",
            IcuAcuityLevel.Level3MultiOrganSupport,
            15,
            IcuVentilationMode.InvasiveMechanical,
            "Arteryel kateter takıldı",
            now);

        Assert.Equal(id, admission.Id);
        Assert.StartsWith("DEMO-ICU-", admission.AdmissionProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(IcuAdmissionStatus.Active, admission.Status);
        Assert.Equal(IcuAcuityLevel.Level3MultiOrganSupport, admission.AcuityLevel);
        Assert.Equal(IcuVentilationMode.InvasiveMechanical, admission.VentilationMode);
        Assert.Equal(15, admission.MonitoringFrequencyMinutes);

        // Update care plan
        var nurseId = Guid.NewGuid();
        admission.UpdateCarePlan(
            IcuAcuityLevel.Level2IntensiveMonitoring,
            30,
            IcuVentilationMode.NonInvasiveCpapBiPap,
            nurseId,
            "Extübe edildi, BiPAP idamesine geçildi",
            now.AddHours(24));

        Assert.Equal(IcuAcuityLevel.Level2IntensiveMonitoring, admission.AcuityLevel);
        Assert.Equal(IcuVentilationMode.NonInvasiveCpapBiPap, admission.VentilationMode);
        Assert.Equal(30, admission.MonitoringFrequencyMinutes);
        Assert.Equal(nurseId, admission.PrimaryNurseId);

        // Transfer to regular ward
        admission.TransferOutOrDischarge(
            IcuAdmissionStatus.TransferredToWard,
            "Hemodinamik stabilite sağlandı, cerrahi servise devredildi.",
            now.AddHours(48));

        Assert.Equal(IcuAdmissionStatus.TransferredToWard, admission.Status);
        Assert.NotNull(admission.DischargedAtUtc);
        Assert.Equal("Hemodinamik stabilite sağlandı, cerrahi servise devredildi.", admission.DischargeNotes);

        // Further modifications on closed admission throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => admission.UpdateCarePlan(
            IcuAcuityLevel.Level1HighDependency,
            60,
            IcuVentilationMode.NoneSpontaneous,
            null,
            null,
            now.AddHours(50)));
    }

    [Fact]
    [Trait("Roadmap", "F08-G06")]
    public async Task IcuAdmissionServicePreventsBedOccupancyCollision()
    {
        var bed = IcuBed.Create(Guid.NewGuid(), "DEMO-ICU-01", "Yoğun Bakım Yatak 1");

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            db.IcuBeds.Add(bed);
            await db.SaveChangesAsync();
        }

        var stay1 = Guid.NewGuid();
        var stay2 = Guid.NewGuid();
        var patient1 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // 1. First patient admitted to Bed 1 -> Success
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var dto1 = new CreateIcuAdmissionDto(
                stay1,
                patient1,
                null,
                bed.Id,
                doctorId,
                null,
                "Solunum yetmezliği",
                IcuAcuityLevel.Level2IntensiveMonitoring,
                60,
                IcuVentilationMode.HighFlowNasalCannula,
                null);

            var res1 = await service.AdmitToIcuAsync(dto1, doctorId);
            Assert.True(res1.IsSuccess);
            Assert.NotNull(res1.Value);
        }

        // 2. Second patient attempts to admit to Bed 1 while first is Active -> 409 Conflict
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var dto2 = new CreateIcuAdmissionDto(
                stay2,
                patient2,
                null,
                bed.Id,
                doctorId,
                null,
                "Kalp yetmezliği",
                IcuAcuityLevel.Level3MultiOrganSupport,
                15,
                IcuVentilationMode.InvasiveMechanical,
                null);

            var res2 = await service.AdmitToIcuAsync(dto2, doctorId);
            Assert.False(res2.IsSuccess);
            Assert.Equal(SurgeryOperationStatus.Conflict, res2.Status);
            Assert.Contains("DEMO-ICU-01", res2.ErrorMessage, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("Roadmap", "F08-G06")]
    public async Task IcuAdmissionServicePreventsDuplicateActivePatientAdmission()
    {
        var bed1 = IcuBed.Create(Guid.NewGuid(), "DEMO-ICU-01", "Yoğun Bakım Yatak 1");
        var bed2 = IcuBed.Create(Guid.NewGuid(), "DEMO-ICU-02", "Yoğun Bakım Yatak 2");

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            db.IcuBeds.AddRange(bed1, bed2);
            await db.SaveChangesAsync();
        }

        var stayId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // 1. Patient admitted to Bed 1
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var dto1 = new CreateIcuAdmissionDto(
                stayId,
                patientId,
                null,
                bed1.Id,
                doctorId,
                null,
                "Sepsis",
                IcuAcuityLevel.Level2IntensiveMonitoring,
                30,
                IcuVentilationMode.NoneSpontaneous,
                null);

            var res1 = await service.AdmitToIcuAsync(dto1, doctorId);
            Assert.True(res1.IsSuccess);
        }

        // 2. Same patient admitted to Bed 2 without discharge -> 409 Conflict
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var dto2 = new CreateIcuAdmissionDto(
                stayId,
                patientId,
                null,
                bed2.Id,
                doctorId,
                null,
                "Yeni takip",
                IcuAcuityLevel.Level2IntensiveMonitoring,
                30,
                IcuVentilationMode.NoneSpontaneous,
                null);

            var res2 = await service.AdmitToIcuAsync(dto2, doctorId);
            Assert.False(res2.IsSuccess);
            Assert.Equal(SurgeryOperationStatus.Conflict, res2.Status);
        }
    }

    [Fact]
    [Trait("Roadmap", "F08-G06")]
    public async Task IcuAdmissionServiceFullFlowAdmitUpdateCarePlanAndTransferOutSucceeds()
    {
        var bed = IcuBed.Create(Guid.NewGuid(), "DEMO-ICU-01", "Yoğun Bakım Yatak 1");

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            db.IcuBeds.Add(bed);
            await db.SaveChangesAsync();
        }

        var stayId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        Guid admissionId;

        // 1. Admit
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var dto = new CreateIcuAdmissionDto(
                stayId,
                patientId,
                null,
                bed.Id,
                doctorId,
                null,
                "Post-Op İzlem",
                IcuAcuityLevel.Level3MultiOrganSupport,
                15,
                IcuVentilationMode.InvasiveMechanical,
                "İlk 24 saat sedasyon");

            var res = await service.AdmitToIcuAsync(dto, doctorId);
            Assert.True(res.IsSuccess);
            admissionId = res.Value!.Id;
        }

        // 2. Update care plan
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var updateDto = new UpdateIcuCarePlanDto(
                IcuAcuityLevel.Level1HighDependency,
                60,
                IcuVentilationMode.NoneSpontaneous,
                Guid.NewGuid(),
                "Spontana alındı");

            var res = await service.UpdateCarePlanAsync(admissionId, updateDto, doctorId);
            Assert.True(res.IsSuccess);
            Assert.Equal(IcuAcuityLevel.Level1HighDependency, res.Value!.AcuityLevel);
        }

        // 3. Transfer Out to Ward
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuAdmissionService(db, _auditPublisher, _timeProvider);
            var transferDto = new IcuDischargeOrTransferDto(
                IcuAdmissionStatus.TransferredToWard,
                "Cerrahi servise devredildi.");

            var res = await service.DischargeOrTransferAsync(admissionId, transferDto, doctorId);
            Assert.True(res.IsSuccess);
            Assert.Equal(IcuAdmissionStatus.TransferredToWard, res.Value!.Status);

            // Bed is now free
            var beds = await service.GetIcuBedsAsync();
            Assert.False(beds.First().IsOccupied);
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
