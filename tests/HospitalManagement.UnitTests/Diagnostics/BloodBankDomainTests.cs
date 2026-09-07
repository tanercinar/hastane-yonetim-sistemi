using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class BloodBankDomainTests
{
    [Theory]
    [InlineData(BloodGroup.ONegative, BloodGroup.APositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.BPositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ONegative, BloodGroup.ONegative, true)]
    [InlineData(BloodGroup.APositive, BloodGroup.APositive, true)]
    [InlineData(BloodGroup.APositive, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.APositive, BloodGroup.BPositive, false)]
    [InlineData(BloodGroup.APositive, BloodGroup.OPositive, false)]
    [InlineData(BloodGroup.BPositive, BloodGroup.APositive, false)]
    [InlineData(BloodGroup.BPositive, BloodGroup.ABPositive, true)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.APositive, false)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.BPositive, false)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.ABPositive, true)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G09")]
    public void RedCellCompatibilityMatrixMatchesImmunohematologyRules(
        BloodGroup donor,
        BloodGroup recipient,
        bool expectedCompatible)
    {
        var result = BloodCompatibilityMatrix.IsCompatible(donor, recipient, BloodProductType.RedBloodCells);
        Assert.Equal(expectedCompatible, result);
    }

    [Theory]
    [InlineData(BloodGroup.ABPositive, BloodGroup.APositive, true)]
    [InlineData(BloodGroup.ABPositive, BloodGroup.OPositive, true)]
    [InlineData(BloodGroup.OPositive, BloodGroup.APositive, false)]
    [InlineData(BloodGroup.OPositive, BloodGroup.OPositive, true)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G09")]
    public void PlasmaCompatibilityMatrixMatchesUniversalRules(
        BloodGroup donor,
        BloodGroup recipient,
        bool expectedCompatible)
    {
        var result = BloodCompatibilityMatrix.IsCompatible(donor, recipient, BloodProductType.FreshFrozenPlasma);
        Assert.Equal(expectedCompatible, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G09")]
    public void BloodUnitLifecycleTransitionsProperly()
    {
        var nowUtc = DateTime.UtcNow;
        var unit = BloodUnit.Create(
            Guid.NewGuid(),
            "DEMO-BLD-2026-0001",
            BloodProductType.RedBloodCells,
            BloodGroup.ONegative,
            450,
            nowUtc.AddDays(-2),
            nowUtc.AddDays(40),
            "Dolap-A / Raf-1",
            nowUtc);

        Assert.Equal(BloodUnitStatus.Available, unit.Status);

        var patientId = Guid.NewGuid();
        unit.Reserve(patientId, nowUtc.AddHours(48), nowUtc);
        Assert.Equal(BloodUnitStatus.Reserved, unit.Status);
        Assert.Equal(patientId, unit.ReservedForPatientId);

        unit.Issue(nowUtc.AddHours(1));
        Assert.Equal(BloodUnitStatus.Issued, unit.Status);

        unit.RecordTransfusion(nowUtc.AddHours(2));
        Assert.Equal(BloodUnitStatus.Transfused, unit.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G09")]
    public void CrossmatchRequestRecordsDeterministicIncompatibility()
    {
        var nowUtc = DateTime.UtcNow;
        var req = CrossmatchRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BloodGroup.BPositive,
            BloodProductType.RedBloodCells,
            2,
            nowUtc.AddHours(12),
            nowUtc);

        var techUserId = Guid.NewGuid();
        req.RecordTestResult(
            techUserId,
            BloodCompatibilityStatus.Incompatible,
            null,
            "A Rh(+) eritrosit süspansiyonu B Rh(+) alıcı için deterministik olarak reddedildi.",
            nowUtc);

        Assert.Equal(CrossmatchStatus.Completed, req.Status);
        Assert.Equal(BloodCompatibilityStatus.Incompatible, req.CompatibilityResult);
        Assert.Null(req.AllocatedBloodUnitId);
    }
}
