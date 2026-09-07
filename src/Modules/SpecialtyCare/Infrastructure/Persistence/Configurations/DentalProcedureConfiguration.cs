using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class DentalProcedureConfiguration : IEntityTypeConfiguration<DentalProcedure>
{
    public void Configure(EntityTypeBuilder<DentalProcedure> builder)
    {
        builder.ToTable("DentalProcedures", "specialty");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.PatientId)
            .IsRequired();

        builder.Property(p => p.ProcedureProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(p => p.ProcedureProtocolNumber)
            .IsUnique();

        builder.Property(p => p.Surfaces)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.ProcedureCode)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(p => p.ProcedureName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(p => p.EstimatedCost)
            .HasPrecision(10, 2);

        builder.Property(p => p.PerformedByDoctorId)
            .IsRequired();

        builder.Property(p => p.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(p => p.CreatedAtUtc)
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(p => p.PatientId);
        builder.HasIndex(p => p.ToothNumber);
        builder.HasIndex(p => p.Status);
    }
}
