using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class ProjectionLagServiceTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task GetProjectionLagAsyncReturnsAccurateLagAndHealthyStatus()
    {
        await using var dbContext = CreateInMemoryDbContext();

        var recentCheckpoint = new ProjectionCheckpoint("DailyOutpatient");
        recentCheckpoint.MarkActive(10, DateTime.UtcNow.AddSeconds(-5));
        dbContext.ProjectionCheckpoints.Add(recentCheckpoint);

        var staleCheckpoint = new ProjectionCheckpoint("DiagnosticWorkload");
        staleCheckpoint.MarkActive(5, DateTime.UtcNow.AddSeconds(-400));
        dbContext.ProjectionCheckpoints.Add(staleCheckpoint);

        var errorCheckpoint = new ProjectionCheckpoint("BedOccupancy");
        errorCheckpoint.MarkError("Bağlantı hatası");
        dbContext.ProjectionCheckpoints.Add(errorCheckpoint);

        await dbContext.SaveChangesAsync();

        var readModelService = new ReportingReadModelService(dbContext);

        var lags = await readModelService.GetProjectionLagAsync();

        Assert.Equal(3, lags.Count);

        var outpatientLag = Assert.Single(lags, l => l.ProjectionName == "DailyOutpatient");
        Assert.True(outpatientLag.LagSeconds < 30);
        Assert.True(outpatientLag.IsHealthy);

        var diagLag = Assert.Single(lags, l => l.ProjectionName == "DiagnosticWorkload");
        Assert.True(diagLag.LagSeconds >= 350);
        Assert.False(diagLag.IsHealthy);

        var errorLag = Assert.Single(lags, l => l.ProjectionName == "BedOccupancy");
        Assert.False(errorLag.IsHealthy);
    }
}
