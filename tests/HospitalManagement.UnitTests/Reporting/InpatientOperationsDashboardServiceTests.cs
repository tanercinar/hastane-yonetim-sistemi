using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class InpatientOperationsDashboardServiceTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsyncCalculatesOccupancyAndAggregatesCorrectly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InpatientOperationsDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var dept2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new BedOccupancyMetric(date, dept1, "Dahiliye", "Genel Servis", 20, 15, 2, now);
        var m2 = new BedOccupancyMetric(date, dept2, "Acil Tıp", "Acil Gözlem", 10, 8, 3, now);

        dbContext.BedOccupancyMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var summary = await service.GetSummaryAsync(date);

        Assert.NotNull(summary);
        Assert.Equal(30, summary.TotalBeds);
        Assert.Equal(23, summary.OccupiedBeds);
        Assert.Equal(7, summary.AvailableBeds);
        Assert.Equal(76.7, summary.OverallOccupancyRate); // 23/30 = 76.666... -> 76.7
        Assert.Equal(5, summary.PendingTransfers);
        Assert.Equal(3, summary.EmergencyWaitingCount); // from Acil ward type
    }

    [Fact]
    public async Task GetWardOccupancyAsyncReturnsAllWardsOrdered()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InpatientOperationsDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new BedOccupancyMetric(date, dept1, "Kardiyoloji", "Koroner Yoğun Bakım", 10, 9, 1, now);
        var m2 = new BedOccupancyMetric(date, dept1, "Kardiyoloji", "Servis", 20, 10, 0, now);

        dbContext.BedOccupancyMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var list = await service.GetWardOccupancyAsync(date, dept1);

        Assert.Equal(2, list.Count);
        Assert.Equal("Koroner Yoğun Bakım", list[0].WardType);
        Assert.Equal(90.0, list[0].OccupancyRatePercentage);
        Assert.Equal("Servis", list[1].WardType);
        Assert.Equal(50.0, list[1].OccupancyRatePercentage);
    }

    [Fact]
    public async Task GetEmergencyTriageMetricsAsyncCalculatesDistribution()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new InpatientOperationsDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new BedOccupancyMetric(date, dept1, "Acil Servis", "Acil", 10, 5, 10, now);

        dbContext.BedOccupancyMetrics.Add(m1);
        await dbContext.SaveChangesAsync();

        var list = await service.GetEmergencyTriageMetricsAsync(date);

        Assert.Equal(3, list.Count);
        Assert.Contains(list, t => t.TriageCategory.StartsWith("Kırmızı", StringComparison.Ordinal));
        Assert.Contains(list, t => t.TriageCategory.StartsWith("Sarı", StringComparison.Ordinal));
        Assert.Contains(list, t => t.TriageCategory.StartsWith("Yeşil", StringComparison.Ordinal));
    }
}
