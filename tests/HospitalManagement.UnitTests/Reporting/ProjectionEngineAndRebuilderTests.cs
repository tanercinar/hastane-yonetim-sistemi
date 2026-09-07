using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Reporting.Infrastructure;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class ProjectionEngineAndRebuilderTests
{
    private static ReportingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReportingDbContext(options);
    }

    [Fact]
    public async Task ProjectAppointmentEventIsIdempotentOnDuplicateEventId()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);

        var eventId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var evt = new AppointmentProjectedEvent(
            eventId,
            Guid.NewGuid(),
            date,
            deptId,
            "Kardiyoloji",
            doctorId,
            "Dr. Ahmet",
            PreviousStatus: "None",
            NewStatus: "Scheduled",
            now);

        // First projection: Should succeed
        var firstResult = await engine.ProjectAppointmentEventAsync(evt);
        Assert.True(firstResult);

        var metric = await dbContext.DailyOutpatientMetrics.FirstOrDefaultAsync();
        Assert.NotNull(metric);
        Assert.Equal(1, metric.TotalAppointments);
        Assert.Equal(1, metric.ScheduledCount);

        // Second projection with same EventId: Should be ignored (idempotent)
        var secondResult = await engine.ProjectAppointmentEventAsync(evt);
        Assert.False(secondResult);

        // State remains unchanged
        var metricAfterDuplicate = await dbContext.DailyOutpatientMetrics.FirstOrDefaultAsync();
        Assert.NotNull(metricAfterDuplicate);
        Assert.Equal(1, metricAfterDuplicate.TotalAppointments);
        Assert.Equal(1, metricAfterDuplicate.ScheduledCount);

        // Checkpoint updated
        var checkpoint = await dbContext.ProjectionCheckpoints.FirstOrDefaultAsync(c => c.ProjectionName == "DailyOutpatient");
        Assert.NotNull(checkpoint);
        Assert.Equal(1, checkpoint.LastProcessedPosition);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);
    }

    [Fact]
    public async Task ProjectDiagnosticEventIsIdempotentOnDuplicateEventId()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);

        var eventId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var evt = new DiagnosticProjectedEvent(
            eventId,
            Guid.NewGuid(),
            date,
            "Radiology_CT",
            PreviousStatus: "None",
            NewStatus: "Ordered",
            IsCritical: false,
            TurnaroundMinutes: null,
            now);

        var firstResult = await engine.ProjectDiagnosticEventAsync(evt);
        Assert.True(firstResult);

        var secondResult = await engine.ProjectDiagnosticEventAsync(evt);
        Assert.False(secondResult);

        var metric = await dbContext.DiagnosticWorkloadMetrics.FirstOrDefaultAsync();
        Assert.NotNull(metric);
        Assert.Equal(1, metric.TotalOrders);
        Assert.Equal(1, metric.PendingSpecimenCount);
    }

    [Fact]
    public async Task ProjectBedOccupancyEventIsIdempotentOnDuplicateEventId()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);

        var eventId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var evt = new BedOccupancyProjectedEvent(
            eventId,
            date,
            deptId,
            "Acil Servis",
            "Emergency",
            TotalBeds: 30,
            OccupiedBeds: 25,
            PendingTransfers: 4,
            now);

        var firstResult = await engine.ProjectBedOccupancyEventAsync(evt);
        Assert.True(firstResult);

        var secondResult = await engine.ProjectBedOccupancyEventAsync(evt);
        Assert.False(secondResult);

        var metric = await dbContext.BedOccupancyMetrics.FirstOrDefaultAsync();
        Assert.NotNull(metric);
        Assert.Equal(30, metric.TotalBeds);
        Assert.Equal(25, metric.OccupiedBeds);
    }

    [Fact]
    public async Task ProjectPharmacyEventIsIdempotentOnDuplicateEventId()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);

        var eventId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var evt = new PharmacyProjectedEvent(
            eventId,
            date,
            EventType: "PrescriptionCreated",
            PendingDelta: 5,
            DispensedDelta: 0,
            LowStockCount: 1,
            NearExpiryCount: 2,
            now);

        var firstResult = await engine.ProjectPharmacyEventAsync(evt);
        Assert.True(firstResult);

        var secondResult = await engine.ProjectPharmacyEventAsync(evt);
        Assert.False(secondResult);

        var metric = await dbContext.PharmacyDispensingMetrics.FirstOrDefaultAsync();
        Assert.NotNull(metric);
        Assert.Equal(5, metric.PendingDispenseCount);
        Assert.Equal(1, metric.LowStockItemCount);
    }

    [Fact]
    public async Task ProjectionRebuilderRebuildsAllProjectionsSuccessfully()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);
        var rebuilder = new ProjectionRebuilder(dbContext, engine, TimeProvider.System);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        // Populate some projection data
        await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
            Guid.NewGuid(), Guid.NewGuid(), date, Guid.NewGuid(), "Göz", null, null, "None", "Scheduled", now));
        await engine.ProjectDiagnosticEventAsync(new DiagnosticProjectedEvent(
            Guid.NewGuid(), Guid.NewGuid(), date, "Laboratory", "None", "Ordered", false, null, now));

        Assert.Equal(1, await dbContext.DailyOutpatientMetrics.CountAsync());
        Assert.Equal(1, await dbContext.DiagnosticWorkloadMetrics.CountAsync());
        Assert.Equal(2, await dbContext.ProjectionProcessedEvents.CountAsync());

        // Rebuild all projections
        var summary = await rebuilder.RebuildAllProjectionsAsync();

        Assert.True(summary.Success);
        Assert.Equal(4, summary.TotalProjectionsRebuilt);
        Assert.Equal(1, await dbContext.DailyOutpatientMetrics.CountAsync());
        Assert.Equal(1, await dbContext.DiagnosticWorkloadMetrics.CountAsync());
        Assert.Equal(2, await dbContext.ProjectionProcessedEvents.CountAsync());
        Assert.Equal(2, await dbContext.ProjectionSourceEvents.CountAsync());

        // Checkpoints are active and replayed projections retain their source positions.
        var checkpoints = await dbContext.ProjectionCheckpoints.ToListAsync();
        Assert.Equal(4, checkpoints.Count);
        Assert.All(checkpoints, c =>
        {
            Assert.Equal(ProjectionStatus.Active, c.Status);
            var expectedPosition = c.ProjectionName is "DailyOutpatient" or "DiagnosticWorkload" ? 1 : 0;
            Assert.Equal(expectedPosition, c.LastProcessedPosition);
        });
    }

    [Fact]
    public async Task ProjectionRebuilderReturnsFailureForUnknownProjection()
    {
        await using var dbContext = CreateInMemoryDbContext();
        var engine = new ReportingProjectionEngine(dbContext);
        var rebuilder = new ProjectionRebuilder(dbContext, engine, TimeProvider.System);

        var summary = await rebuilder.RebuildProjectionAsync("NonExistentProjection");

        Assert.False(summary.Success);
        Assert.Equal(0, summary.TotalProjectionsRebuilt);
        Assert.Contains("Geçersiz projeksiyon", summary.Message);
    }
}
