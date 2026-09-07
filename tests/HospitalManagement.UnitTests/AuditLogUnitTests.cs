using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.AuditPrivacy.Domain;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests;

public sealed class AuditLogUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G06")]
    public void AuditLogEntryCalculatesHashAndDetectsTampering()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var actorUserId = Guid.NewGuid();
        var actorPersonId = Guid.NewGuid();

        var entry = AuditLogEntry.Create(
            id,
            createdAt,
            actorUserId,
            actorPersonId,
            "DOC",
            "127.0.0.1",
            "Mozilla/5.0",
            AuditAction.EncounterView,
            "Encounter",
            "12345",
            AuditOutcome.Success,
            reason: null,
            correlationId: "corr-1");

        // Doğru ve değiştirilmemiş kayıt bütünlük testini geçer
        Assert.True(entry.VerifyHashIntegrity());

        // Başka bir veriye göre hesaplanan hash ile uyuşmaz
        var forgedHash = AuditLogEntry.ComputeRecordHash(
            id,
            createdAt,
            actorUserId,
            actorPersonId,
            AuditAction.ClinicalNoteSign, // Değiştirilmiş eylem!
            "Encounter",
            "12345",
            AuditOutcome.Success.ToString(),
            "corr-1");

        Assert.NotEqual(forgedHash, entry.RecordHash);
    }

    [Theory]
    [InlineData("ActorRole", "ADM")]
    [InlineData("ActorIpAddress", "203.0.113.10")]
    [InlineData("ActorUserAgent", "tampered-agent")]
    [InlineData("Reason", "tampered-reason")]
    [InlineData("DetailsJson", "{\"tampered\":true}")]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void AuditLogEntryHashCoversEveryMutableAuditField(string propertyName, string forgedValue)
    {
        var entry = AuditLogEntry.Create(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DOC",
            "127.0.0.1",
            "Mozilla/5.0",
            AuditAction.EncounterView,
            "Encounter",
            "12345",
            AuditOutcome.Success,
            reason: "authorized",
            correlationId: "corr-integrity",
            detailsJson: "{\"purpose\":\"care\"}");

        var property = typeof(AuditLogEntry).GetProperty(propertyName);
        Assert.NotNull(property);
        property.SetValue(entry, forgedValue);

        Assert.False(entry.VerifyHashIntegrity());
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G06")]
    public async Task AuditPrivacyDbContextRejectsModificationAndDeletionBecauseItIsAppendOnly()
    {
        var options = new DbContextOptionsBuilder<AuditPrivacyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new AuditPrivacyDbContext(options);

        var entry = AuditLogEntry.Create(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DOC",
            "127.0.0.1",
            "Mozilla/5.0",
            AuditAction.UserLogin,
            "User",
            "user-1",
            AuditOutcome.Success,
            reason: null,
            correlationId: "corr-1");

        dbContext.AuditLogs.Add(entry);
        await dbContext.SaveChangesAsync();

        // 1. Güncelleme denemesi (Reddedilmelidir)
        dbContext.Entry(entry).State = EntityState.Modified;
        var updateEx = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("append-only", updateEx.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Silme denemesi (Reddedilmelidir)
        dbContext.Entry(entry).State = EntityState.Deleted;
        var deleteEx = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
        Assert.Contains("append-only", deleteEx.Message, StringComparison.OrdinalIgnoreCase);
    }
}
