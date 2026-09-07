using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class OutpatientDashboardServiceTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetSummaryAsyncCalculatesAggregatesCorrectly()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new OutpatientDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var dept2 = Guid.NewGuid();
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new DailyOutpatientMetric(date, dept1, "Kardiyoloji", doc1, "Dr. Ali", now);
        m1.ApplyTransition("None", "Scheduled", now);
        m1.ApplyTransition("Scheduled", "CheckedIn", now);

        var m2 = new DailyOutpatientMetric(date, dept2, "Göz", doc2, "Dr. Veli", now);
        m2.ApplyTransition("None", "Scheduled", now);
        m2.ApplyTransition("Scheduled", "InProgress", now);
        m2.ApplyTransition("InProgress", "Completed", now);

        dbContext.DailyOutpatientMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var summary = await service.GetSummaryAsync(date);

        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalAppointments);
        Assert.Equal(1, summary.CheckedInCount);
        Assert.Equal(1, summary.CompletedCount);
        Assert.Equal(1, summary.WaitingQueueCount);
    }

    [Fact]
    public async Task GetDepartmentMetricsAsyncGroupsByDepartment()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new OutpatientDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new DailyOutpatientMetric(date, dept1, "Dahiliye", Guid.NewGuid(), "Dr. A", now);
        m1.ApplyTransition("None", "Scheduled", now);

        var m2 = new DailyOutpatientMetric(date, dept1, "Dahiliye", Guid.NewGuid(), "Dr. B", now);
        m2.ApplyTransition("None", "Scheduled", now);
        m2.ApplyTransition("Scheduled", "Completed", now);

        dbContext.DailyOutpatientMetrics.AddRange(m1, m2);
        await dbContext.SaveChangesAsync();

        var depts = await service.GetDepartmentMetricsAsync(date);

        Assert.Single(depts);
        Assert.Equal("Dahiliye", depts[0].DepartmentName);
        Assert.Equal(2, depts[0].TotalAppointments);
        Assert.Equal(1, depts[0].ScheduledCount);
        Assert.Equal(1, depts[0].CompletedCount);
    }

    [Fact]
    public async Task GetDoctorMetricsAsyncGroupsByDoctor()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var service = new OutpatientDashboardService(dbContext);

        var date = new DateOnly(2026, 9, 4);
        var dept1 = Guid.NewGuid();
        var doc1 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var m1 = new DailyOutpatientMetric(date, dept1, "KBB", doc1, "Dr. Mehmet", now);
        m1.ApplyTransition("None", "Scheduled", now);
        m1.ApplyTransition("Scheduled", "CheckedIn", now);

        dbContext.DailyOutpatientMetrics.Add(m1);
        await dbContext.SaveChangesAsync();

        var docs = await service.GetDoctorMetricsAsync(date, dept1);

        Assert.Single(docs);
        Assert.Equal(doc1, docs[0].DoctorId);
        Assert.Equal("Dr. Mehmet", docs[0].DoctorName);
        Assert.Equal(1, docs[0].TotalAppointments);
        Assert.Equal(1, docs[0].WaitingCount);
    }
}
