using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HospitalManagement.IntegrationTests;

public sealed class AuditHashChainConcurrencyTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F13-G02")]
    public async Task ConcurrentPublishCreatesOneLinearTamperEvidentChain()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var services = new ServiceCollection();
        services.AddDbContext<AuditPrivacyDbContext>(options =>
            options.UseNpgsql(database.ConnectionString));
        await using var provider = services.BuildServiceProvider();

        await using (var migrationScope = provider.CreateAsyncScope())
        {
            var dbContext = migrationScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        var timestamp = new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);
        var publishTasks = Enumerable.Range(1, 24).Select(async index =>
        {
            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var service = new AuditLogService(dbContext, NullLogger<AuditLogService>.Instance);
            await service.PublishAsync(new AuditEvent(
                Guid.NewGuid(),
                timestamp,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DOC",
                "127.0.0.1",
                "audit-concurrency-test",
                "Authorization.AccessDenied",
                "ClinicalEncounter",
                $"resource-{index}",
                AuditOutcome.Forbidden,
                "scope-mismatch",
                $"corr-{index}",
                null));
        });

        await Task.WhenAll(publishTasks);

        await using var verificationScope = provider.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var entries = await verificationDb.AuditLogs
            .AsNoTracking()
            .OrderBy(entry => entry.ChainPosition)
            .ToListAsync();

        Assert.Equal(24, entries.Count);
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            Assert.Equal(index + 1, entry.ChainPosition);
            Assert.Equal(index == 0 ? null : entries[index - 1].RecordHash, entry.PreviousRecordHash);
            Assert.True(entry.VerifyHashIntegrity());
        }
    }
}
