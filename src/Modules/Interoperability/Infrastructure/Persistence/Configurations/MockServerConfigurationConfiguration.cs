using HospitalManagement.Modules.Interoperability.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class MockServerConfigurationConfiguration : IEntityTypeConfiguration<MockServerConfiguration>
{
    public void Configure(EntityTypeBuilder<MockServerConfiguration> builder)
    {
        builder.ToTable("mock_server_configs", "interoperability");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.SystemType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(c => c.SystemType)
            .IsUnique();

        builder.Property(c => c.FaultMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.IsEnabled)
            .IsRequired();

        builder.Property(c => c.LatencyMilliseconds)
            .IsRequired();

        builder.Property(c => c.FailureRatePercentage)
            .IsRequired();

        builder.Property(c => c.MaxRetryAttempts)
            .IsRequired();

        builder.Property(c => c.TimeoutSeconds)
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .IsRequired();
    }
}
