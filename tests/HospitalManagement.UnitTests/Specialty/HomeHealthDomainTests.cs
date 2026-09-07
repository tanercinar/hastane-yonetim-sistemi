using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using Xunit;

namespace HospitalManagement.UnitTests.SpecialtyCare;

public sealed class HomeHealthDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G04")]
    public void HomeHealthVisitRequestCreatesValidVisitWithProtocol()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var visit = HomeHealthVisit.Request(
            id,
            patientId,
            HomeCareServiceType.WoundDressing,
            HomeVisitPriority.Urgent,
            "İstanbul",
            "Kadıköy",
            "Moda Cad. No: 12",
            "0532 555 0122",
            staffId,
            "Diyabetik ayak pansumanı",
            now);

        Assert.Equal(id, visit.Id);
        Assert.Equal(patientId, visit.PatientId);
        Assert.StartsWith("DEMO-HOM-", visit.ProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(HomeCareServiceType.WoundDressing, visit.ServiceType);
        Assert.Equal(HomeVisitPriority.Urgent, visit.Priority);
        Assert.Equal(HomeVisitStatus.Requested, visit.Status);
        Assert.Equal("İstanbul", visit.City);
        Assert.Equal("Kadıköy", visit.District);
        Assert.Equal("Moda Cad. No: 12", visit.AddressDetail);
        Assert.Equal("0532 555 0122", visit.ContactPhone);
        Assert.Equal(staffId, visit.RequestedByStaffId);
    }

    [Theory]
    [InlineData("", "Kadıköy", "Moda Cad.", "05320000000")]
    [InlineData("İstanbul", "", "Moda Cad.", "05320000000")]
    [InlineData("İstanbul", "Kadıköy", "", "05320000000")]
    [InlineData("İstanbul", "Kadıköy", "Moda Cad.", "")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G04")]
    public void HomeHealthVisitValidationFailsForMissingAddressOrPhone(
        string city,
        string district,
        string addressDetail,
        string contactPhone)
    {
        Assert.Throws<ArgumentException>(() =>
            HomeHealthVisit.Request(
                Guid.NewGuid(),
                Guid.NewGuid(),
                HomeCareServiceType.GeneralNursing,
                HomeVisitPriority.Routine,
                city,
                district,
                addressDetail,
                contactPhone,
                Guid.NewGuid(),
                null,
                DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G04")]
    public void HomeHealthVisitLifecycleAssignStartCompleteSucceeds()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var assignedStaffId = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var visit = HomeHealthVisit.Request(
            id,
            patientId,
            HomeCareServiceType.BloodCollection,
            HomeVisitPriority.Routine,
            "Ankara",
            "Çankaya",
            "Tunalı Hilmi Cad. No: 5",
            "0533 111 2233",
            staffId,
            "Rutin biyokimya kan alma",
            now);

        // 1. Assign
        visit.AssignTeam(assignedStaffId, now.AddHours(2), now);
        Assert.Equal(HomeVisitStatus.Assigned, visit.Status);
        Assert.Equal(assignedStaffId, visit.AssignedStaffId);
        Assert.NotNull(visit.ScheduledDateUtc);

        // 2. Start
        visit.StartVisit(now.AddHours(2));
        Assert.Equal(HomeVisitStatus.InProgress, visit.Status);
        Assert.NotNull(visit.VisitStartedAtUtc);

        // 3. Complete
        Assert.Throws<ArgumentException>(() =>
            visit.CompleteVisit("Klinik not", null, Guid.Empty, now.AddHours(3)));
        Assert.Equal(HomeVisitStatus.InProgress, visit.Status);

        visit.CompleteVisit("Kan numuneleri alındı ve soğuk zincirle laboratuvara iletildi.", "TA: 125/80, Nabız: 70", encounterId, now.AddHours(3));
        Assert.Equal(HomeVisitStatus.Completed, visit.Status);
        Assert.NotNull(visit.VisitCompletedAtUtc);
        Assert.Equal(encounterId, visit.EncounterId);
        Assert.Equal("TA: 125/80, Nabız: 70", visit.VitalsSummaryNotes);
        Assert.Contains("Kan numuneleri alındı", visit.ClinicalNotes, StringComparison.Ordinal);

        // Cannot complete again
        Assert.Throws<InvalidOperationException>(() => visit.CompleteVisit("Tekrar", null, Guid.NewGuid(), DateTime.UtcNow));
        // Cannot cancel completed
        Assert.Throws<InvalidOperationException>(() => visit.Cancel("İptal", DateTime.UtcNow));
    }
}
