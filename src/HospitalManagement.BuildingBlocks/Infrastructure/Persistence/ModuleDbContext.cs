using HospitalManagement.BuildingBlocks.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HospitalManagement.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Provides persistence rules shared by module-owned DbContexts without exposing module entities.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    private const string PostgreSqlUtcTimestampType = "timestamp with time zone";

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        ConfigureModuleModel(modelBuilder);
        ApplyPersistenceConventions(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareTrackedEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareTrackedEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected abstract void ConfigureModuleModel(ModelBuilder modelBuilder);

    private static void ApplyPersistenceConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (IsUtcTimestampType(property.ClrType))
                {
                    property.SetColumnType(PostgreSqlUtcTimestampType);
                }
            }

            if (!typeof(IHasConcurrencyVersion).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .Property<long>(nameof(IHasConcurrencyVersion.Version))
                .IsConcurrencyToken()
                .ValueGeneratedNever();
        }
    }

    private static bool IsUtcTimestampType(Type propertyType)
    {
        var nonNullableType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return nonNullableType == typeof(DateTime) || nonNullableType == typeof(DateTimeOffset);
    }

    private void PrepareTrackedEntries()
    {
        ValidateUtcTimestamps();
        AdvanceConcurrencyVersions();
    }

    private void ValidateUtcTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            foreach (var property in entry.Properties)
            {
                if (entry.State == EntityState.Modified && !property.IsModified)
                {
                    continue;
                }

                ValidateUtcTimestamp(entry, property);
            }
        }
    }

    private static void ValidateUtcTimestamp(EntityEntry entry, PropertyEntry property)
    {
        var isUtc = property.CurrentValue switch
        {
            null => true,
            DateTime dateTime => dateTime.Kind == DateTimeKind.Utc,
            DateTimeOffset dateTimeOffset => dateTimeOffset.Offset == TimeSpan.Zero,
            _ => true,
        };

        if (isUtc)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Persistence property '{entry.Metadata.ClrType.Name}.{property.Metadata.Name}' must contain a UTC timestamp.");
    }

    private void AdvanceConcurrencyVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IHasConcurrencyVersion>())
        {
            var versionProperty = entry.Property(entity => entity.Version);

            if (entry.State == EntityState.Added)
            {
                entry.Entity.Version = 1;
                continue;
            }

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            if (versionProperty.OriginalValue < 1)
            {
                throw new InvalidOperationException(
                    $"Concurrency version for '{entry.Metadata.ClrType.Name}' must be a positive value.");
            }

            entry.Entity.Version = checked(versionProperty.OriginalValue + 1);
            versionProperty.IsModified = true;
        }
    }
}
