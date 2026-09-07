using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class PharmacyDashboardServiceTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsyncCalculatesPrescriptionsAndAlertsCorrectly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new PharmacyDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var m1 = new PharmacyDispensingMetric(date, 10, 4, 6, 2, 1, now);
        dbContext.PharmacyDispensingMetrics.Add(m1);
        await dbContext.SaveChangesAsync();

        var summary = await service.GetSummaryAsync(date);

        Assert.NotNull(summary);
        Assert.Equal(10, summary.TotalPrescriptions);
        Assert.Equal(4, summary.PendingDispenseCount);
        Assert.Equal(6, summary.DispensedCount);
        Assert.Equal(2, summary.LowStockItemCount);
        Assert.Equal(1, summary.NearExpiryLotCount);
    }

    [Fact]
    public async Task GetStockAlertsAsyncGeneratesAlertsWithoutFinancialData()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new PharmacyDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var m1 = new PharmacyDispensingMetric(date, 5, 2, 3, 2, 1, now);
        dbContext.PharmacyDispensingMetrics.Add(m1);
        await dbContext.SaveChangesAsync();

        var alerts = await service.GetStockAlertsAsync(date);

        Assert.Equal(3, alerts.Count);
        Assert.All(alerts, a => Assert.False(string.IsNullOrWhiteSpace(a.ItemName)));
        Assert.All(alerts, a => Assert.True(a.CurrentStock >= 0));
    }
}
