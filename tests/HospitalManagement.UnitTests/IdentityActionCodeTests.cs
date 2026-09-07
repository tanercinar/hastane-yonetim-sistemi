using HospitalManagement.Modules.IdentityAccess.Domain;

namespace HospitalManagement.UnitTests;

public sealed class IdentityActionCodeTests
{
    private static readonly DateTime IssuedAtUtc = new(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G02")]
    public void IssuedCodeIsUsableBeforeExpiryAndCanOnlyBeConsumedOnce()
    {
        var code = Issue();
        var consumedAtUtc = IssuedAtUtc.AddMinutes(5);

        Assert.True(code.IsUsableAt(consumedAtUtc));

        code.Consume(consumedAtUtc);

        Assert.Equal(consumedAtUtc, code.ConsumedAtUtc);
        Assert.False(code.IsUsableAt(consumedAtUtc.AddSeconds(1)));
        Assert.Throws<InvalidOperationException>(() => code.Consume(consumedAtUtc.AddSeconds(1)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G02")]
    public void ExpiredOrRevokedCodeCannotBeConsumed()
    {
        var expired = Issue();
        var revoked = Issue();

        revoked.Revoke(IssuedAtUtc.AddMinutes(1));

        Assert.False(expired.IsUsableAt(IssuedAtUtc.AddMinutes(15)));
        Assert.False(revoked.IsUsableAt(IssuedAtUtc.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => expired.Consume(IssuedAtUtc.AddMinutes(15)));
        Assert.Throws<InvalidOperationException>(() => revoked.Consume(IssuedAtUtc.AddMinutes(2)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G02")]
    public void ActionCodeRejectsNonUtcTimesAndPlaintextValues()
    {
        Assert.Throws<ArgumentException>(() => IdentityActionCode.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            IdentityActionPurpose.ResetPassword,
            "DEMO-PLAINTEXT-CODE",
            IssuedAtUtc,
            IssuedAtUtc.AddMinutes(15)));
        Assert.Throws<ArgumentException>(() => IdentityActionCode.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            IdentityActionPurpose.ResetPassword,
            new string('A', 64),
            DateTime.SpecifyKind(IssuedAtUtc, DateTimeKind.Local),
            IssuedAtUtc.AddMinutes(15)));
        var code = Issue();
        var nonUtcTime = DateTime.SpecifyKind(IssuedAtUtc.AddMinutes(1), DateTimeKind.Unspecified);
        Assert.False(code.IsUsableAt(nonUtcTime));
        Assert.Throws<InvalidOperationException>(() => code.Consume(nonUtcTime));
    }

    private static IdentityActionCode Issue() => IdentityActionCode.Issue(
        Guid.NewGuid(),
        Guid.NewGuid(),
        IdentityActionPurpose.ResetPassword,
        new string('A', 64),
        IssuedAtUtc,
        IssuedAtUtc.AddMinutes(15));
}
