using HospitalManagement.Modules.Interoperability.Domain.ENabiz;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Interoperability.Infrastructure.Persistence.Configurations;

public sealed class ENabizTransmissionRecordConfiguration : IEntityTypeConfiguration<ENabizTransmissionRecord>
{
    public void Configure(EntityTypeBuilder<ENabizTransmissionRecord> builder)
    {
        builder.ToTable("enabiz_transmissions", "interoperability");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SysTakipNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(t => t.SysTakipNo)
            .IsUnique();

        builder.Property(t => t.PackageType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.PatientId)
            .IsRequired();

        builder.HasIndex(t => t.PatientId);

        builder.Property(t => t.PatientNationalId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.HasPatientConsent)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(t => t.Status);

        builder.Property(t => t.PayloadSummary)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(t => t.ResponseCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ResponseMessage)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.RetryCount)
            .IsRequired();

        builder.Property(t => t.QueuedAtUtc)
            .IsRequired();

        builder.Property(t => t.SentAtUtc);

        builder.Property(t => t.LastAttemptAtUtc);
    }
}
