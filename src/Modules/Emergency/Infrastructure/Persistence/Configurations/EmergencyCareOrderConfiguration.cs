using HospitalManagement.Modules.Emergency.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Emergency.Infrastructure.Persistence.Configurations;

public sealed class EmergencyCareOrderConfiguration : IEntityTypeConfiguration<EmergencyCareOrder>
{
    public void Configure(EntityTypeBuilder<EmergencyCareOrder> builder)
    {
        builder.ToTable("EmergencyCareOrders", "emergency");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.AdmissionId)
            .IsRequired();

        builder.Property(o => o.OrderType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.OrderCatalogCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.OrderCatalogName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.OrderedByDoctorId)
            .IsRequired();

        builder.Property(o => o.OrderedAtUtc)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(o => o.ClinicalInstructions)
            .HasMaxLength(1000);

        builder.Property(o => o.ResultSummary)
            .HasMaxLength(2000);

        builder.Property(o => o.CancellationReason)
            .HasMaxLength(500);

        builder.Property(o => o.Version)
            .IsRowVersion();

        builder.HasIndex(o => new { o.AdmissionId, o.Status });
        builder.HasIndex(o => o.OrderedAtUtc);
    }
}
