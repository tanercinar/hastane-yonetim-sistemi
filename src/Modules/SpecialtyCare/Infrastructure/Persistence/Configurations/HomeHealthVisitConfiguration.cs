using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence.Configurations;

public sealed class HomeHealthVisitConfiguration : IEntityTypeConfiguration<HomeHealthVisit>
{
    public void Configure(EntityTypeBuilder<HomeHealthVisit> builder)
    {
        builder.ToTable("HomeHealthVisits", "specialty");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        builder.Property(v => v.PatientId)
            .IsRequired();

        builder.Property(v => v.ProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(v => v.ProtocolNumber)
            .IsUnique();

        builder.HasIndex(v => v.EncounterId)
            .IsUnique()
            .HasDatabaseName("UX_HomeHealthVisits_EncounterId");

        builder.Property(v => v.ServiceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(v => v.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(v => v.RequestedDateUtc)
            .IsRequired();

        builder.Property(v => v.City)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(v => v.District)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(v => v.AddressDetail)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.ContactPhone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(v => v.RequestedByStaffId)
            .IsRequired();

        builder.Property(v => v.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(v => v.VitalsSummaryNotes)
            .HasMaxLength(1000);

        builder.Property(v => v.CreatedAtUtc)
            .IsRequired();

        builder.Property(v => v.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(v => v.PatientId);
        builder.HasIndex(v => v.AssignedStaffId);
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.ScheduledDateUtc);
    }
}
