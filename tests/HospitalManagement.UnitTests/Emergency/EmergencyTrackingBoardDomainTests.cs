using HospitalManagement.Modules.Emergency.Domain;
using HospitalManagement.Modules.Emergency.Infrastructure;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Emergency;

public sealed class EmergencyTrackingBoardDomainTests
{
    private static EmergencyDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<EmergencyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EmergencyDbContext(options);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G02")]
    public async Task GetBoardSummaryAsyncCalculatesCorrectKpis()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var startTime = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(startTime);
        var staffId = Guid.NewGuid();

        // 1. Admission 1: WaitingTriage (admitted at 09:30, 30 mins ago)
        var adm1 = EmergencyAdmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyArrivalType.WalkIn,
            "Göğüs ağrısı",
            null,
            staffId,
            startTime.AddMinutes(-30));

        // 2. Admission 2: TriagedWaitingDoctor (admitted 60 mins ago, triaged 20 mins ago as Red1)
        var adm2 = EmergencyAdmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyArrivalType.Ambulance,
            "Kardiyak arrest şüphesi",
            null,
            staffId,
            startTime.AddMinutes(-60));
        adm2.RecordTriage(TriageLevel.Red1Resuscitation, "Vital stabil değil", staffId, 80, 50, 140, 36.5m, 28, 88, 10, "Unresponsive", null, startTime.AddMinutes(-20));

        // 3. Admission 3: InEvaluation (admitted 90 mins ago, triaged 80 mins ago as Yellow, assigned doctor 40 mins ago)
        var adm3 = EmergencyAdmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyArrivalType.WalkIn,
            "Karın ağrısı",
            null,
            staffId,
            startTime.AddMinutes(-90));
        adm3.RecordTriage(TriageLevel.YellowUrgent, "Akut apandisit", staffId, 120, 80, 85, 37.8m, 18, 98, 7, "Alert", null, startTime.AddMinutes(-80));
        adm3.AssignDoctor(Guid.NewGuid(), startTime.AddMinutes(-40));
        adm3.AssignBedOrZone("Sarı Alan - Yatak 3", startTime.AddMinutes(-40));

        // 4. Admission 4: Discharged today (admitted 120 mins ago, discharged 10 mins ago)
        var adm4 = EmergencyAdmission.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EmergencyArrivalType.WalkIn,
            "Hafif burkulma",
            null,
            staffId,
            startTime.AddMinutes(-120));
        adm4.RecordTriage(TriageLevel.GreenStandard, "Ayak bileği travması", staffId, 120, 80, 72, 36.5m, 14, 99, 2, "Alert", null, startTime.AddMinutes(-110));
        adm4.AssignDoctor(Guid.NewGuid(), startTime.AddMinutes(-100));
        adm4.UpdateStatus(EmergencyAdmissionStatus.Discharged, "Şifa ile taburcu", startTime.AddMinutes(-10));

        dbContext.Admissions.AddRange(adm1, adm2, adm3, adm4);
        await dbContext.SaveChangesAsync();

        var service = new EmergencyTrackingBoardService(dbContext, timeProvider);
        var summary = await service.GetBoardSummaryAsync();

        // Total active admissions = adm1 + adm2 + adm3 = 3
        Assert.Equal(3, summary.TotalActiveAdmissions);
        Assert.Equal(1, summary.WaitingTriageCount);
        Assert.Equal(1, summary.TriagedWaitingDoctorCount);
        Assert.Equal(1, summary.InEvaluationCount);
        Assert.Equal(0, summary.InObservationCount);
        Assert.Equal(1, summary.TodayDischargedCount);
        Assert.Equal(1, summary.Red1Count);
        Assert.Equal(0, summary.Red2Count);
        Assert.Equal(1, summary.YellowCount);
        Assert.Equal(0, summary.GreenCount);
        Assert.Equal(30.0, summary.AverageWaitMinutesTriage);
        Assert.Equal(20.0, summary.AverageWaitMinutesDoctor);
        Assert.Equal(60.0, summary.AverageLengthOfStayMinutes); // (30 + 60 + 90) / 3 = 60 mins
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G02")]
    public async Task GetBoardWorklistAsyncOrdersByTriagePriorityAndFilters()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var startTime = new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(startTime);
        var staffId = Guid.NewGuid();

        var greenAdm = EmergencyAdmission.Create(Guid.NewGuid(), Guid.NewGuid(), EmergencyArrivalType.WalkIn, "Yeşil hasta", null, staffId, startTime.AddMinutes(-100));
        greenAdm.RecordTriage(TriageLevel.GreenStandard, "Hafif", staffId, 120, 80, 70, 36.5m, 14, 99, 1, "Alert", null, startTime.AddMinutes(-90));
        greenAdm.AssignBedOrZone("Yeşil Alan - Yatak 1", startTime.AddMinutes(-80));

        var redAdm = EmergencyAdmission.Create(Guid.NewGuid(), Guid.NewGuid(), EmergencyArrivalType.Ambulance, "Kırmızı hasta", null, staffId, startTime.AddMinutes(-10));
        redAdm.RecordTriage(TriageLevel.Red1Resuscitation, "Kritik", staffId, 70, 40, 150, 36.0m, 30, 85, 10, "Unresponsive", null, startTime.AddMinutes(-5));
        redAdm.AssignBedOrZone("Resüsitasyon 1", startTime.AddMinutes(-5));

        var yellowAdm = EmergencyAdmission.Create(Guid.NewGuid(), Guid.NewGuid(), EmergencyArrivalType.WalkIn, "Sarı hasta", null, staffId, startTime.AddMinutes(-50));
        yellowAdm.RecordTriage(TriageLevel.YellowUrgent, "Acil", staffId, 130, 85, 90, 37.0m, 18, 97, 6, "Alert", null, startTime.AddMinutes(-40));
        yellowAdm.AssignBedOrZone("Sarı Alan - Yatak 2", startTime.AddMinutes(-30));

        dbContext.Admissions.AddRange(greenAdm, redAdm, yellowAdm);
        await dbContext.SaveChangesAsync();

        var service = new EmergencyTrackingBoardService(dbContext, timeProvider);

        // 1. Unfiltered worklist: Red1 should come first despite shorter waiting time
        var worklist = await service.GetBoardWorklistAsync();
        Assert.Equal(3, worklist.Count);
        Assert.Equal(TriageLevel.Red1Resuscitation, worklist[0].TriageLevel);
        Assert.Equal(TriageLevel.YellowUrgent, worklist[1].TriageLevel);
        Assert.Equal(TriageLevel.GreenStandard, worklist[2].TriageLevel);

        // 2. Filter by Zone
        var filteredZone = await service.GetBoardWorklistAsync(zone: "Resüsitasyon");
        Assert.Single(filteredZone);
        Assert.Equal("Resüsitasyon 1", filteredZone[0].AssignedBedOrZone);

        // 3. Filter by Triage
        var filteredTriage = await service.GetBoardWorklistAsync(triageLevel: TriageLevel.YellowUrgent);
        Assert.Single(filteredTriage);
        Assert.Equal(TriageLevel.YellowUrgent, filteredTriage[0].TriageLevel);
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
