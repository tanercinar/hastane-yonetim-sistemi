using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Database;

/// <summary>
/// Owns only the platform bootstrap migration. Business entities remain in module-owned DbContexts.
/// </summary>
public sealed class DatabaseBootstrapDbContext(DbContextOptions<DatabaseBootstrapDbContext> options)
    : DbContext(options)
{
    public const string MigrationHistoryTable = "__ef_migrations_history";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(DatabaseSchemas.Platform);
    }
}
