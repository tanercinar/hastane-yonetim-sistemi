using HospitalManagement.BuildingBlocks.Infrastructure.Persistence;
using HospitalManagement.BuildingBlocks.Persistence;
using HospitalManagement.Host.Database;
using HospitalManagement.IntegrationTests.Infrastructure;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.IntegrationTests;

public sealed class DatabaseFoundationTests
{
    private const string InitialMigration = "202608260001_InitialDatabaseFoundation";

    [Fact]
    [Trait("Roadmap", "F01-G06")]
    public async Task InitialMigrationAppliesToAnEmptyIsolatedPostgreSqlDatabase()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        await using var context = CreateBootstrapContext(database.ConnectionString);

        Assert.StartsWith("hms_it_", database.DatabaseName, StringComparison.Ordinal);
        Assert.Contains(InitialMigration, await context.Database.GetPendingMigrationsAsync());
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal([InitialMigration], await context.Database.GetAppliedMigrationsAsync());

        var actualSchemas = await ReadSchemasAsync(database.ConnectionString);
        Assert.Equal(
            DatabaseSchemas.All.Order(StringComparer.Ordinal),
            actualSchemas.Intersect(DatabaseSchemas.All, StringComparer.Ordinal).Order(StringComparer.Ordinal));
    }

    [Fact]
    [Trait("Roadmap", "F01-G06")]
    public async Task ModulePersistenceRejectsNonUtcValuesAndDetectsConcurrentUpdates()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var options = new DbContextOptionsBuilder<PersistenceProbeDbContext>()
            .UseNpgsql(database.ConnectionString)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false)
            .Options;

        await using (var setupContext = new PersistenceProbeDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            var entityType = setupContext.Model.FindEntityType(typeof(PersistenceProbe));
            Assert.NotNull(entityType);
            Assert.Equal(
                "timestamp with time zone",
                entityType.FindProperty(nameof(PersistenceProbe.RecordedAtUtc))?.GetColumnType());
            Assert.True(entityType.FindProperty(nameof(PersistenceProbe.Version))?.IsConcurrencyToken);

            setupContext.Add(CreateProbe(DateTime.SpecifyKind(new(2026, 8, 26, 9, 0, 0), DateTimeKind.Local)));
            var localTimeException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => setupContext.SaveChangesAsync());
            Assert.Contains("must contain a UTC timestamp", localTimeException.Message, StringComparison.Ordinal);
            setupContext.ChangeTracker.Clear();

            var offsetProbe = CreateProbe(new(2026, 8, 26, 9, 0, 0, DateTimeKind.Utc));
            offsetProbe.ObservedAtUtc = new(2026, 8, 26, 12, 0, 0, TimeSpan.FromHours(3));
            setupContext.Add(offsetProbe);
            var offsetException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => setupContext.SaveChangesAsync());
            Assert.Contains("must contain a UTC timestamp", offsetException.Message, StringComparison.Ordinal);
            setupContext.ChangeTracker.Clear();
        }

        var probe = CreateProbe(new(2026, 8, 26, 9, 0, 0, DateTimeKind.Utc));
        await using (var insertContext = new PersistenceProbeDbContext(options))
        {
            insertContext.Add(probe);
            await insertContext.SaveChangesAsync();
            Assert.Equal(1, probe.Version);
        }

        await using var firstWriter = new PersistenceProbeDbContext(options);
        await using var staleWriter = new PersistenceProbeDbContext(options);
        var firstCopy = await firstWriter.Set<PersistenceProbe>().SingleAsync(entity => entity.Id == probe.Id);
        var staleCopy = await staleWriter.Set<PersistenceProbe>().SingleAsync(entity => entity.Id == probe.Id);

        firstCopy.Label = "DEMO-FIRST-WRITER";
        await firstWriter.SaveChangesAsync();
        Assert.Equal(2, firstCopy.Version);

        staleCopy.Label = "DEMO-STALE-WRITER";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleWriter.SaveChangesAsync());

        await using var verificationContext = new PersistenceProbeDbContext(options);
        var storedProbe = await verificationContext.Set<PersistenceProbe>()
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == probe.Id);
        Assert.Equal("DEMO-FIRST-WRITER", storedProbe.Label);
        Assert.Equal(2, storedProbe.Version);
    }

    private static DatabaseBootstrapDbContext CreateBootstrapContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DatabaseBootstrapDbContext>()
            .UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable(
                DatabaseBootstrapDbContext.MigrationHistoryTable,
                DatabaseSchemas.Platform))
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false)
            .Options;
        return new DatabaseBootstrapDbContext(options);
    }

    private static async Task<HashSet<string>> ReadSchemasAsync(string connectionString)
    {
        const string Sql = "SELECT schema_name FROM information_schema.schemata;";
        var schemas = new HashSet<string>(StringComparer.Ordinal);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(Sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            schemas.Add(reader.GetString(0));
        }

        return schemas;
    }

    private static PersistenceProbe CreateProbe(DateTime recordedAtUtc)
    {
        return new PersistenceProbe
        {
            Id = Guid.NewGuid(),
            Label = "DEMO-PERSISTENCE-PROBE",
            RecordedAtUtc = recordedAtUtc,
            ObservedAtUtc = new(2026, 8, 26, 9, 0, 0, TimeSpan.Zero),
        };
    }

    private sealed class PersistenceProbeDbContext(DbContextOptions<PersistenceProbeDbContext> options)
        : ModuleDbContext(options)
    {
        protected override void ConfigureModuleModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("foundation_test");
            modelBuilder.Entity<PersistenceProbe>(entity =>
            {
                entity.ToTable("persistence_probes");
                entity.HasKey(probe => probe.Id);
                entity.Property(probe => probe.Label).HasMaxLength(80);
            });
        }
    }

    private sealed class PersistenceProbe : IHasConcurrencyVersion
    {
        public Guid Id
        {
            get;
            set;
        }

        public string Label
        {
            get;
            set;
        } = string.Empty;

        public DateTime RecordedAtUtc
        {
            get;
            set;
        }

        public DateTimeOffset ObservedAtUtc
        {
            get;
            set;
        }

        public long Version
        {
            get;
            set;
        }
    }
}
