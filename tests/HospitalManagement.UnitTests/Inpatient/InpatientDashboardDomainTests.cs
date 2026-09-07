using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.Modules.Inpatient.Application;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class InpatientDashboardDomainTests
{
    [Fact]
    public void DashboardCalculatesOccupancyPercentagesCorrectly()
    {
        var ward1Id = Guid.NewGuid();
        var ward2Id = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        var wardSummaries = new List<WardOccupancySummaryDto>
        {
            new(
                ward1Id,
                "Kardiyoloji Servisi",
                "CARD-01",
                deptId,
                TotalBeds: 10,
                OccupiedBeds: 8,
                AvailableBeds: 1,
                CleaningBeds: 1,
                MaintenanceBeds: 0,
                OccupancyPercentage: 80.0,
                ActivePatientsCount: 8),
            new(
                ward2Id,
                "Ortopedi Servisi",
                "ORTH-01",
                deptId,
                TotalBeds: 10,
                OccupiedBeds: 2,
                AvailableBeds: 7,
                CleaningBeds: 0,
                MaintenanceBeds: 1,
                OccupancyPercentage: 20.0,
                ActivePatientsCount: 2),
        };

        var dashboard = new InpatientDashboardDto(
            TotalBeds: 20,
            OccupiedBeds: 10,
            AvailableBeds: 8,
            CleaningBeds: 1,
            MaintenanceBeds: 1,
            OverallOccupancyPercentage: 50.0,
            PendingAdmissionsCount: 3,
            PendingTransfersCount: 1,
            TodayDischargesCount: 4,
            Wards: wardSummaries);

        Assert.Equal(20, dashboard.TotalBeds);
        Assert.Equal(10, dashboard.OccupiedBeds);
        Assert.Equal(50.0, dashboard.OverallOccupancyPercentage);
        Assert.Equal(3, dashboard.PendingAdmissionsCount);
        Assert.Equal(1, dashboard.PendingTransfersCount);
        Assert.Equal(4, dashboard.TodayDischargesCount);
        Assert.Equal(2, dashboard.Wards.Count);
        Assert.Equal(80.0, dashboard.Wards[0].OccupancyPercentage);
        Assert.Equal(20.0, dashboard.Wards[1].OccupancyPercentage);
    }

    [Fact]
    public void ZeroBedDashboardReturnsZeroPercentageWithoutException()
    {
        var dashboard = new InpatientDashboardDto(
            TotalBeds: 0,
            OccupiedBeds: 0,
            AvailableBeds: 0,
            CleaningBeds: 0,
            MaintenanceBeds: 0,
            OverallOccupancyPercentage: 0.0,
            PendingAdmissionsCount: 0,
            PendingTransfersCount: 0,
            TodayDischargesCount: 0,
            Wards: []);

        Assert.Equal(0, dashboard.TotalBeds);
        Assert.Equal(0.0, dashboard.OverallOccupancyPercentage);
        Assert.Empty(dashboard.Wards);
    }
}
