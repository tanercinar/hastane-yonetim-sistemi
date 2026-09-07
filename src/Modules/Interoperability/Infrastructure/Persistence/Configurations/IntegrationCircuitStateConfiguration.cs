using HospitalManagement.Modules.Interoperability.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class IntegrationCircuitStateConfiguration : IEntityTypeConfiguration<IntegrationCircuitState>
{
    public void Configure(EntityTypeBuilder<IntegrationCircuitState> builder)
    {
        builder.ToTable("integration_circuit_states", "interoperability");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SystemType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(s => s.SystemType)
            .IsUnique();

        builder.Property(s => s.State)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.ConsecutiveFailures)
            .IsRequired();

        builder.Property(s => s.FailureThreshold)
            .IsRequired();

        builder.Property(s => s.RecoveryTimeoutSeconds)
            .IsRequired();

        builder.Property(s => s.LastFailureTimeUtc);

        builder.Property(s => s.NextAttemptAllowedUtc);

        builder.Property(s => s.LastUpdatedUtc)
            .IsRequired();
    }
}
