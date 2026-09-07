using HospitalManagement.Modules.Diagnostics.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence.Configurations;

public sealed class LabResultItemConfiguration : IEntityTypeConfiguration<LabResultItem>
{
    public void Configure(EntityTypeBuilder<LabResultItem> builder)
    {
        builder.ToTable("lab_result_items", "diagnostics");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.LabResultId)
            .HasColumnName("lab_result_id")
            .IsRequired();

        builder.Property(i => i.ParameterCode)
            .HasColumnName("parameter_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.ParameterName)
            .HasColumnName("parameter_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.NumericValue)
            .HasColumnName("numeric_value")
            .HasPrecision(18, 4);

        builder.Property(i => i.StringValue)
            .HasColumnName("string_value")
            .HasMaxLength(500);

        builder.Property(i => i.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50);

        builder.Property(i => i.ReferenceRangeLow)
            .HasColumnName("reference_range_low")
            .HasPrecision(18, 4);

        builder.Property(i => i.ReferenceRangeHigh)
            .HasColumnName("reference_range_high")
            .HasPrecision(18, 4);

        builder.Property(i => i.ReferenceRangeText)
            .HasColumnName("reference_range_text")
            .HasMaxLength(200);

        builder.Property(i => i.Flag)
            .HasColumnName("flag")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(i => i.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.HasIndex(i => i.LabResultId);
        builder.HasIndex(i => i.ParameterCode);
    }
}
