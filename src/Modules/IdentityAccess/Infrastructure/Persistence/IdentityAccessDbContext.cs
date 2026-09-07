using HospitalManagement.BuildingBlocks.Persistence;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

public sealed class IdentityAccessDbContext(DbContextOptions<IdentityAccessDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    private const string PostgreSqlUtcTimestampType = "timestamp with time zone";

    public const string Schema = "identity_access";
    public const string MigrationHistoryTable = "__ef_migrations_history";

    public DbSet<IdentityActionCode> ActionCodes => Set<IdentityActionCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(user => user.PersonId).HasColumnName("person_id");
            entity.Property(user => user.AccountKind)
                .HasColumnName("account_kind")
                .HasConversion<string>()
                .HasMaxLength(16);
            entity.Property(user => user.IsEnabled).HasColumnName("is_enabled");
            entity.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("ux_users_normalized_email");
            entity.HasIndex(user => user.PersonId)
                .IsUnique()
                .HasDatabaseName("ux_users_person_id");
        });
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        IdentityRoleSeed.Configure(builder);

        builder.Entity<IdentityActionCode>(entity =>
        {
            entity.ToTable(
                "action_codes",
                table => table.HasCheckConstraint(
                    "ck_action_codes_expiry",
                    "expires_at_utc > created_at_utc"));
            entity.HasKey(code => code.Id).HasName("pk_action_codes");
            entity.Property(code => code.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(code => code.UserId).HasColumnName("user_id");
            entity.Property(code => code.Purpose)
                .HasColumnName("purpose")
                .HasConversion<string>()
                .HasMaxLength(40);
            entity.Property(code => code.CodeHash)
                .HasColumnName("code_hash")
                .HasMaxLength(64);
            entity.Property(code => code.IssuedByUserId).HasColumnName("issued_by_user_id");
            entity.Property(code => code.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(code => code.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(code => code.ConsumedAtUtc).HasColumnName("consumed_at_utc");
            entity.Property(code => code.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(code => code.Version).HasColumnName("version");
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(code => code.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_action_codes_users_user_id");
            entity.HasIndex(code => code.CodeHash)
                .IsUnique()
                .HasDatabaseName("ux_action_codes_code_hash");
            entity.HasIndex(code => new { code.UserId, code.Purpose })
                .IsUnique()
                .HasFilter("consumed_at_utc IS NULL AND revoked_at_utc IS NULL")
                .HasDatabaseName("ux_action_codes_active_user_purpose");
        });

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var nonNullableType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (nonNullableType == typeof(DateTime) || nonNullableType == typeof(DateTimeOffset))
                {
                    property.SetColumnType(PostgreSqlUtcTimestampType);
                }
            }
        }

        builder.Entity<IdentityActionCode>()
            .Property(code => code.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever();
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

    private void PrepareTrackedEntries()
    {
        ValidateUtcTimestamps();
        AdvanceActionCodeVersions();
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
        if (!isUtc)
        {
            throw new InvalidOperationException(
                $"Persistence property '{entry.Metadata.ClrType.Name}.{property.Metadata.Name}' must contain a UTC timestamp.");
        }
    }

    private void AdvanceActionCodeVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IHasConcurrencyVersion>())
        {
            var versionProperty = entry.Property(entity => entity.Version);
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Version = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version = checked(versionProperty.OriginalValue + 1);
                versionProperty.IsModified = true;
            }
        }
    }
}
