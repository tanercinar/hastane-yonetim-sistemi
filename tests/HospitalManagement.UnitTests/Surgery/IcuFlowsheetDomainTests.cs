using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Surgery;

public sealed class IcuFlowsheetDomainTests
{
    private readonly DbContextOptions<SurgeryDbContext> _dbOptions;
    private readonly FakeAuditPublisher _auditPublisher = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    public IcuFlowsheetDomainTests()
    {
        _dbOptions = new DbContextOptionsBuilder<SurgeryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    [Trait("Roadmap", "F08-G07")]
    public void FlowsheetEntryCalculatesMapAndFluidBalanceCorrectly()
    {
        var id = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var entry = IcuFlowsheetEntry.Create(
            id,
            admissionId,
            now,
            staffId,
            heartRateBpm: 84,
            systolicBpMmHg: 120,
            diastolicBpMmHg: 75,
            respiratoryRateBpm: 18,
            oxygenSaturationPct: 97.5m,
            bodyTemperatureCelsius: 37.1m,
            glasgowComaScale: 14,
            richmondAgitationSedationScale: -1,
            ventilationMode: IcuVentilationMode.InvasiveMechanical,
            fractionOfInspiredOxygenPct: 40,
            positiveEndExpiratoryPressure: 8,
            tidalVolumeMl: 480,
            peakInspiratoryPressure: 22,
            ivFluidIntakeMl: 150,
            enteralNutritionIntakeMl: 50,
            urineOutputMl: 120,
            drainOutputMl: 30,
            clinicalNotes: "Stabil seyrediyor",
            nowUtc: now);

        Assert.Equal(id, entry.Id);
        Assert.Equal(84, entry.HeartRateBpm);
        // MAP = (2*75 + 120) / 3 = 270 / 3 = 90
        Assert.Equal(90, entry.MeanArterialPressureMmHg);
        Assert.Equal(200, entry.TotalIntakeMl); // 150 + 50
        Assert.Equal(150, entry.TotalOutputMl); // 120 + 30
        Assert.Equal(50, entry.NetFluidBalanceMl); // 200 - 150
        Assert.Equal(14, entry.GlasgowComaScale);
        Assert.Equal(-1, entry.RichmondAgitationSedationScale);
    }

    [Fact]
    [Trait("Roadmap", "F08-G07")]
    public void FlowsheetEntryRejectsOutOfRangeParameters()
    {
        var admissionId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Invalid Heart Rate
        Assert.Throws<ArgumentOutOfRangeException>(() => IcuFlowsheetEntry.Create(
            Guid.NewGuid(), admissionId, now, staffId,
            heartRateBpm: 350, null, null, null, null, null, null, null,
            IcuVentilationMode.NoneSpontaneous, null, null, null, null, null, null, null, null, null, now));

        // Invalid GCS (>15)
        Assert.Throws<ArgumentOutOfRangeException>(() => IcuFlowsheetEntry.Create(
            Guid.NewGuid(), admissionId, now, staffId,
            null, null, null, null, null, null,
            glasgowComaScale: 16, null,
            IcuVentilationMode.NoneSpontaneous, null, null, null, null, null, null, null, null, null, now));

        // Invalid RASS (>4)
        Assert.Throws<ArgumentOutOfRangeException>(() => IcuFlowsheetEntry.Create(
            Guid.NewGuid(), admissionId, now, staffId,
            null, null, null, null, null, null, null,
            richmondAgitationSedationScale: 5,
            IcuVentilationMode.NoneSpontaneous, null, null, null, null, null, null, null, null, null, now));

        // Invalid SpO2 (>100)
        Assert.Throws<ArgumentOutOfRangeException>(() => IcuFlowsheetEntry.Create(
            Guid.NewGuid(), admissionId, now, staffId,
            null, null, null, null,
            oxygenSaturationPct: 105, null, null, null,
            IcuVentilationMode.NoneSpontaneous, null, null, null, null, null, null, null, null, null, now));
    }

    [Fact]
    [Trait("Roadmap", "F08-G07")]
    public async Task IcuFlowsheetServiceAggregatesFluidBalanceSummaryProperly()
    {
        var admissionId = Guid.NewGuid();
        var bed = IcuBed.Create(Guid.NewGuid(), "DEMO-ICU-01", "Yoğun Bakım Yatak 1");
        var admission = IcuAdmission.Create(
            admissionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            bed.Id,
            bed.BedCode,
            Guid.NewGuid(),
            null,
            "Septik Şok",
            IcuAcuityLevel.Level3MultiOrganSupport,
            15,
            IcuVentilationMode.InvasiveMechanical,
            null,
            DateTime.UtcNow.AddDays(-1));

        using (var db = new SurgeryDbContext(_dbOptions))
        {
            db.IcuBeds.Add(bed);
            db.IcuAdmissions.Add(admission);
            await db.SaveChangesAsync();
        }

        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Add 2 entries
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuFlowsheetService(db, _auditPublisher, _timeProvider);

            var entry1 = new CreateIcuFlowsheetEntryDto(
                IcuAdmissionId: admissionId,
                RecordedAtUtc: now.AddHours(-2),
                HeartRateBpm: 80,
                SystolicBpMmHg: 120,
                DiastolicBpMmHg: 80,
                RespiratoryRateBpm: 16,
                OxygenSaturationPct: 98m,
                BodyTemperatureCelsius: 37m,
                GlasgowComaScale: 15,
                RichmondAgitationSedationScale: 0,
                VentilationMode: IcuVentilationMode.InvasiveMechanical,
                FractionOfInspiredOxygenPct: 40,
                PositiveEndExpiratoryPressure: 5,
                TidalVolumeMl: 450,
                PeakInspiratoryPressure: 20,
                IvFluidIntakeMl: 100,
                EnteralNutritionIntakeMl: 50,
                UrineOutputMl: 70,
                DrainOutputMl: 10,
                ClinicalNotes: "Saat 1");

            var entry2 = new CreateIcuFlowsheetEntryDto(
                IcuAdmissionId: admissionId,
                RecordedAtUtc: now.AddHours(-1),
                HeartRateBpm: 82,
                SystolicBpMmHg: 118,
                DiastolicBpMmHg: 78,
                RespiratoryRateBpm: 16,
                OxygenSaturationPct: 99m,
                BodyTemperatureCelsius: 36.8m,
                GlasgowComaScale: 15,
                RichmondAgitationSedationScale: 0,
                VentilationMode: IcuVentilationMode.InvasiveMechanical,
                FractionOfInspiredOxygenPct: 40,
                PositiveEndExpiratoryPressure: 5,
                TidalVolumeMl: 450,
                PeakInspiratoryPressure: 20,
                IvFluidIntakeMl: 100,
                EnteralNutritionIntakeMl: 50,
                UrineOutputMl: 80,
                DrainOutputMl: 0,
                ClinicalNotes: "Saat 2");

            var res1 = await service.AddFlowsheetEntryAsync(entry1, staffId);
            var res2 = await service.AddFlowsheetEntryAsync(entry2, staffId);

            Assert.True(res1.IsSuccess);
            Assert.True(res2.IsSuccess);
        }

        // Query summary
        using (var db = new SurgeryDbContext(_dbOptions))
        {
            var service = new IcuFlowsheetService(db, _auditPublisher, _timeProvider);
            var summary = await service.GetFluidBalanceSummaryAsync(admissionId, now.AddHours(-12), now);

            Assert.Equal(2, summary.EntryCount);
            Assert.Equal(200, summary.TotalIvIntakeMl); // 100 + 100
            Assert.Equal(100, summary.TotalEnteralIntakeMl); // 50 + 50
            Assert.Equal(300, summary.TotalIntakeMl); // 200 + 100
            Assert.Equal(150, summary.TotalUrineOutputMl); // 70 + 80
            Assert.Equal(10, summary.TotalDrainOutputMl); // 10 + 0
            Assert.Equal(160, summary.TotalOutputMl); // 150 + 10
            Assert.Equal(140, summary.NetBalanceMl); // 300 - 160
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
