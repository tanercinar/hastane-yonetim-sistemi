using HospitalManagement.Modules.Inpatient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Inpatient.Infrastructure.Persistence.Configurations;

public sealed class NursingCareTaskConfiguration : IEntityTypeConfiguration<NursingCareTask>
{
    public void Configure(EntityTypeBuilder<NursingCareTask> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("nursing_care_tasks", "inpatient");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.CarePlanId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Frequency)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.DueTimeUtc)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.CompletionNotes)
            .HasMaxLength(500);

        builder.Property(t => t.CancellationReason)
            .HasMaxLength(500);

        builder.Property(t => t.Version)
            .IsConcurrencyToken();

        builder.HasIndex(t => t.CarePlanId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.DueTimeUtc);
    }
}
