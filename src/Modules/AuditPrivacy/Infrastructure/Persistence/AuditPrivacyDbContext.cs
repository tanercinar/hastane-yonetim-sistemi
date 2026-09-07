using HospitalManagement.Modules.AuditPrivacy.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;

public class AuditPrivacyDbContext(DbContextOptions<AuditPrivacyDbContext> options)
    : DbContext(options)
{
    public const string SchemaName = "audit_privacy";

    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_logs", SchemaName);

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            entity.Property(e => e.ActorUserId)
                .HasColumnName("actor_user_id");

            entity.Property(e => e.ActorPersonId)
                .HasColumnName("actor_person_id");

            entity.Property(e => e.ActorRole)
                .HasColumnName("actor_role")
                .HasMaxLength(64);

            entity.Property(e => e.ActorIpAddress)
                .HasColumnName("actor_ip_address")
                .HasMaxLength(64);

            entity.Property(e => e.ActorUserAgent)
                .HasColumnName("actor_user_agent")
                .HasMaxLength(512);

            entity.Property(e => e.Action)
                .HasColumnName("action")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.TargetResourceType)
                .HasColumnName("target_resource_type")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.TargetResourceId)
                .HasColumnName("target_resource_id")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.Outcome)
                .HasColumnName("outcome")
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(512);

            entity.Property(e => e.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.DetailsJson)
                .HasColumnName("details_json")
                .HasColumnType("text");

            entity.Property(e => e.RecordHash)
                .HasColumnName("record_hash")
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.PreviousRecordHash)
                .HasColumnName("previous_record_hash")
                .HasMaxLength(128);

            entity.Property(e => e.ChainPosition)
                .HasColumnName("chain_position")
                .IsRequired();

            entity.Property(e => e.HashVersion)
                .HasColumnName("hash_version")
                .IsRequired();

            entity.HasIndex(e => e.CreatedAtUtc);
            entity.HasIndex(e => e.ActorUserId);
            entity.HasIndex(e => e.ActorPersonId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => new { e.TargetResourceType, e.TargetResourceId });
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.ChainPosition)
                .IsUnique();
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceAppendOnly()
    {
        var forbiddenEntries = ChangeTracker.Entries<AuditLogEntry>()
            .Where(e => e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (forbiddenEntries.Count > 0)
        {
            throw new InvalidOperationException(
                "Denetim kayıtları salt-eklenir (append-only) niteliktedir; güncelleme ve silme işlemleri yasaktır.");
        }
    }
}
