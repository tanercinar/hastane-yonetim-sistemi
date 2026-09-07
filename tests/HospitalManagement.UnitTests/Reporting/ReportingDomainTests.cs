using HospitalManagement.Modules.Reporting.Domain;

namespace HospitalManagement.UnitTests.Reporting;

public sealed class ReportingDomainTests
{
    [Fact]
    public void DailyOutpatientMetricApplyTransitionUpdatesCountsCorrectly()
    {
        var departmentId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var metric = new DailyOutpatientMetric(date, departmentId, "Kardiyoloji", doctorId, "Dr. Ahmet", now);

        Assert.Equal(0, metric.TotalAppointments);
        Assert.Equal(0, metric.ScheduledCount);

        // Schedule new appointment (previous status is null/none)
        metric.ApplyTransition("None", "Scheduled", now);
        Assert.Equal(1, metric.TotalAppointments);
        Assert.Equal(1, metric.ScheduledCount);
        Assert.Equal(0, metric.CheckedInCount);

        // Check-in
        metric.ApplyTransition("Scheduled", "CheckedIn", now);
        Assert.Equal(1, metric.TotalAppointments);
        Assert.Equal(0, metric.ScheduledCount);
        Assert.Equal(1, metric.CheckedInCount);

        // In progress
        metric.ApplyTransition("CheckedIn", "InProgress", now);
        Assert.Equal(0, metric.CheckedInCount);
        Assert.Equal(1, metric.InProgressCount);

        // Complete
        metric.ApplyTransition("InProgress", "Completed", now);
        Assert.Equal(0, metric.InProgressCount);
        Assert.Equal(1, metric.CompletedCount);
    }

    [Fact]
    public void DiagnosticWorkloadMetricAppliesOrderTransitionsAndTurnaround()
    {
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var metric = new DiagnosticWorkloadMetric(date, "Laboratory", now);

        metric.ApplyOrderTransition("None", "Ordered", isCritical: false, turnaroundMinutes: null, now);
        Assert.Equal(1, metric.TotalOrders);
        Assert.Equal(1, metric.PendingSpecimenCount);
        Assert.Equal(0, metric.ProcessingCount);
        Assert.Equal(0, metric.FinalizedCount);

        metric.ApplyOrderTransition("Ordered", "Collected", isCritical: false, turnaroundMinutes: null, now);
        Assert.Equal(0, metric.PendingSpecimenCount);
        Assert.Equal(1, metric.ProcessingCount);

        metric.ApplyOrderTransition("Collected", "Finalized", isCritical: true, turnaroundMinutes: 30.0, now);
        Assert.Equal(0, metric.ProcessingCount);
        Assert.Equal(1, metric.FinalizedCount);
        Assert.Equal(1, metric.CriticalCount);
        Assert.Equal(30.0, metric.AvgTurnaroundMinutes);
    }

    [Fact]
    public void BedOccupancyMetricCalculatesOccupancyRateCorrectly()
    {
        var date = new DateOnly(2026, 9, 4);
        var deptId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var metric = new BedOccupancyMetric(date, deptId, "Dahiliye", "General", totalBeds: 20, occupiedBeds: 15, pendingTransfers: 2, now);

        Assert.Equal(20, metric.TotalBeds);
        Assert.Equal(15, metric.OccupiedBeds);
        Assert.Equal(5, metric.AvailableBeds);
        Assert.Equal(2, metric.PendingTransferCount);
        Assert.Equal(75.0, metric.OccupancyRatePercentage);

        metric.UpdateOccupancy(totalBeds: 20, occupiedBeds: 18, pendingTransfers: 1, now);
        Assert.Equal(18, metric.OccupiedBeds);
        Assert.Equal(2, metric.AvailableBeds);
        Assert.Equal(90.0, metric.OccupancyRatePercentage);
    }

    [Fact]
    public void PharmacyDispensingMetricUpdatesCountsCorrectly()
    {
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        var metric = new PharmacyDispensingMetric(date, totalPrescriptions: 10, pendingDispenseCount: 10, dispensedCount: 0, lowStockItemCount: 3, nearExpiryLotCount: 1, now);

        metric.UpdateCounts(pendingDelta: -1, dispensedDelta: 1, lowStockCount: 2, nearExpiryCount: 0, now);

        Assert.Equal(10, metric.TotalPrescriptions);
        Assert.Equal(9, metric.PendingDispenseCount);
        Assert.Equal(1, metric.DispensedCount);
        Assert.Equal(2, metric.LowStockItemCount);
        Assert.Equal(0, metric.NearExpiryLotCount);
    }

    [Fact]
    public void ProjectionCheckpointTransitionsStatusCorrectly()
    {
        var checkpoint = new ProjectionCheckpoint("DailyOutpatient");

        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);
        Assert.Equal(0, checkpoint.LastProcessedPosition);

        checkpoint.MarkRebuilding();
        Assert.Equal(ProjectionStatus.Rebuilding, checkpoint.Status);
        Assert.Equal(0, checkpoint.LastProcessedPosition);

        var now = DateTime.UtcNow;
        checkpoint.MarkActive(42, now);
        Assert.Equal(ProjectionStatus.Active, checkpoint.Status);
        Assert.Equal(42, checkpoint.LastProcessedPosition);

        checkpoint.MarkError("Bağlantı kesildi");
        Assert.Equal(ProjectionStatus.Error, checkpoint.Status);
        Assert.Equal("Bağlantı kesildi", checkpoint.LastError);
    }
}
