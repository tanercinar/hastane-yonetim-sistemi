using HospitalManagement.Contracts.Specialty;
using Xunit;

namespace HospitalManagement.UnitTests.SpecialtyCare;

public sealed class SpecialtyReportingDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G05")]
    public void SpecialtyOperationalSummaryResponseHoldsAllVerticalCountsCorrectly()
    {
        var now = DateTime.UtcNow;
        var summary = new SpecialtyOperationalSummaryResponse(
            ActivePregnanciesCount: 15,
            HighRiskPregnanciesCount: 3,
            TotalDeliveriesCount: 42,
            CesareanDeliveriesCount: 14,
            NormalDeliveriesCount: 28,
            TotalDentalProceduresCount: 120,
            CompletedDentalProceduresCount: 95,
            PlannedDentalProceduresCount: 25,
            TotalDentalExaminationsCount: 88,
            ActiveHomeVisitsCount: 18,
            PendingHomeVisitRequestsCount: 5,
            AssignedHomeVisitsCount: 7,
            CompletedHomeVisitsCount: 34,
            UrgentHomeVisitsCount: 2,
            GeneratedAtUtc: now);

        Assert.Equal(15, summary.ActivePregnanciesCount);
        Assert.Equal(3, summary.HighRiskPregnanciesCount);
        Assert.Equal(42, summary.TotalDeliveriesCount);
        Assert.Equal(14, summary.CesareanDeliveriesCount);
        Assert.Equal(28, summary.NormalDeliveriesCount);
        Assert.Equal(120, summary.TotalDentalProceduresCount);
        Assert.Equal(95, summary.CompletedDentalProceduresCount);
        Assert.Equal(25, summary.PlannedDentalProceduresCount);
        Assert.Equal(88, summary.TotalDentalExaminationsCount);
        Assert.Equal(18, summary.ActiveHomeVisitsCount);
        Assert.Equal(5, summary.PendingHomeVisitRequestsCount);
        Assert.Equal(7, summary.AssignedHomeVisitsCount);
        Assert.Equal(34, summary.CompletedHomeVisitsCount);
        Assert.Equal(2, summary.UrgentHomeVisitsCount);
        Assert.Equal(now, summary.GeneratedAtUtc);
    }
}
