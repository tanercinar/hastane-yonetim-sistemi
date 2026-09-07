using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class DiagnosticDashboardServiceTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsyncCalculatesAggregatesAndAvgTatCorrectly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new DiagnosticDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var m1 = new DiagnosticWorkloadMetric(date, "Biyokimya", now);
        m1.ApplyOrderTransition("None", "Ordered", false, null, now);
        m1.ApplyOrderTransition("Ordered", "Finalized", true, 45.0, now);

        var m2 = new DiagnosticWorkloadMetric(date, "Mikrobiyoloji", now);
        m2.ApplyOrderTransition("None", "Ordered", false, null, now);
        m2.ApplyOrderTransition("Ordered", "Collected", false, null, now);

        var m3 = new DiagnosticWorkloadMetric(date, "Radyoloji", now);
        m3.ApplyOrderTransition("None", "Ordered", false, null, now);
        m3.ApplyOrderTransition("Ordered", "Finalized", false, 35.0, now);

        dbContext.DiagnosticWorkloadMetrics.AddRange(m1, m2, m3);
        await dbContext.SaveChangesAsync();

        var summary = await service.GetSummaryAsync(date);

        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalOrders);
        Assert.Equal(1, summary.ProcessingCount); // Collected -> ProcessingCount
        Assert.Equal(2, summary.FinalizedCount);
        Assert.Equal(1, summary.CriticalCount);
        Assert.Equal(40.0, summary.AvgTurnaroundMinutes); // (45 + 35) / 2 = 40.0
    }

    [Fact]
    public async Task GetModalityMetricsAsyncGroupsByModality()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new DiagnosticDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var m1 = new DiagnosticWorkloadMetric(date, "Röntgen", now);
        m1.ApplyOrderTransition("None", "Ordered", false, null, now);
        m1.ApplyOrderTransition("Ordered", "Finalized", false, 20.0, now);

        var m2 = new DiagnosticWorkloadMetric(date, "MR", now);
        m2.ApplyOrderTransition("None", "Ordered", true, null, now);

        dbContext.DiagnosticWorkloadMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var list = await service.GetModalityMetricsAsync(date);

        Assert.Equal(2, list.Count);
        var mr = list.Single(m => m.ModalityOrSection == "MR");
        Assert.Equal(1, mr.TotalOrders);
        Assert.Equal(1, mr.PendingSpecimenCount);
        Assert.Equal(1, mr.CriticalCount);

        var xray = list.Single(m => m.ModalityOrSection == "Röntgen");
        Assert.Equal(1, xray.TotalOrders);
        Assert.Equal(1, xray.FinalizedCount);
        Assert.Equal(20.0, xray.AvgTurnaroundMinutes);
    }

    [Fact]
    public async Task GetCriticalAlertMetricsAsyncReturnsOnlyMetricsWithCriticalCount()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new DiagnosticDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var m1 = new DiagnosticWorkloadMetric(date, "Biyokimya", now);
        m1.ApplyOrderTransition("None", "Ordered", true, null, now);
        m1.ApplyOrderTransition("None", "Ordered", true, null, now);

        var m2 = new DiagnosticWorkloadMetric(date, "Patoloji", now);
        m2.ApplyOrderTransition("None", "Ordered", false, null, now);

        dbContext.DiagnosticWorkloadMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var alerts = await service.GetCriticalAlertMetricsAsync(date);

        Assert.Single(alerts);
        Assert.Equal("Biyokimya", alerts[0].ModalityOrSection);
        Assert.Equal(2, alerts[0].CriticalCount);
    }
}
