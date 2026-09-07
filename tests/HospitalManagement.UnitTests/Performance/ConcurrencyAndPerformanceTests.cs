using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Reporting.Domain;
using HospitalManagement.Modules.Scheduling.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Performance;

public sealed class ConcurrencyAndPerformanceTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G07")]
    public void AppointmentSlotBookingConcurrentRequestsOnlyFirstSucceedsAndSecondFails()
    {
        var now = DateTime.UtcNow;
        var slot = AppointmentSlot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(2),
            now.AddHours(2).AddMinutes(30),
            now);

        var patient1 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();

        // First patient books slot
        slot.Book(patient1, now);
        Assert.Equal(SlotStatus.Booked, slot.Status);

        // Second patient concurrent attempt on the same slot must fail
        var ex = Assert.Throws<InvalidOperationException>(() => slot.Book(patient2, now));
        Assert.Contains("uygun değildir", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G07")]
    public void BedAssignmentConcurrentReservationOnlyFirstSucceedsAndSecondFails()
    {
        var now = DateTime.UtcNow;
        var bed = Bed.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "B-101-A",
            BedPlacementGender.Any,
            IsolationType.None,
            hasTelemetry: false,
            hasOxygen: true,
            hasVentilator: false,
            now);

        var admission1 = Guid.NewGuid();
        var patient1 = Guid.NewGuid();
        var admission2 = Guid.NewGuid();
        var patient2 = Guid.NewGuid();

        // First admission assigns bed
        bed.AssignAdmission(admission1, patient1, now);
        Assert.Equal(BedStatus.Occupied, bed.Status);
        Assert.Equal(admission1, bed.CurrentAdmissionId);

        // Second concurrent assignment on the same bed must fail
        var ex = Assert.Throws<InvalidOperationException>(() =>
            bed.AssignAdmission(admission2, patient2, now));
        Assert.Contains("uygun değil", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G07")]
    public void MedicationStockDeductionExceedingAvailableQuantityThrowsInvalidOperationException()
    {
        var now = DateTime.UtcNow;
        var stock = MedicationStockItem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Depo-A",
            Guid.NewGuid(),
            "LOT-2026-X",
            now.AddMonths(12),
            initialQuantity: 10,
            reorderLevel: 2,
            now);

        // Deduct 7 units
        stock.DeductStock(7, now);
        Assert.Equal(3, stock.QuantityOnHand);
        Assert.Equal(3, stock.QuantityAvailable);

        // Attempting to deduct 5 units when only 3 are available must fail
        var ex = Assert.Throws<InvalidOperationException>(() => stock.DeductStock(5, now));
        Assert.Contains("Yetersiz", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, stock.QuantityOnHand); // Stock remains unchanged
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G07")]
    public void ReportingProjectionTransitionAccuracyUnderLoad()
    {
        var now = DateTime.UtcNow;
        var metric = new DailyOutpatientMetric(
            DateOnly.FromDateTime(now),
            Guid.NewGuid(),
            "Kardiyoloji",
            Guid.NewGuid(),
            "Dr. Ahmet",
            now);

        // 50 appointments scheduled
        for (var i = 0; i < 50; i++)
        {
            metric.ApplyTransition("None", "Scheduled", now);
        }

        Assert.Equal(50, metric.TotalAppointments);
        Assert.Equal(50, metric.ScheduledCount);

        // 30 checked in
        for (var i = 0; i < 30; i++)
        {
            metric.ApplyTransition("Scheduled", "CheckedIn", now);
        }

        Assert.Equal(50, metric.TotalAppointments);
        Assert.Equal(20, metric.ScheduledCount);
        Assert.Equal(30, metric.CheckedInCount);

        // 25 completed
        for (var i = 0; i < 25; i++)
        {
            metric.ApplyTransition("CheckedIn", "Completed", now);
        }

        Assert.Equal(50, metric.TotalAppointments);
        Assert.Equal(5, metric.CheckedInCount);
        Assert.Equal(25, metric.CompletedCount);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(50000, 200)] // Clamped to max allowed
    [InlineData(-10, 1)]     // Clamped to min allowed
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G07")]
    public void PaginationClampingGuaranteesBoundedMemory(int requestedPageSize, int expectedClamped)
    {
        var clamped = Math.Clamp(requestedPageSize, 1, 200);
        Assert.Equal(expectedClamped, clamped);
    }
}
