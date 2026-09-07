using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence.Configurations;

public sealed class SurgeryBookingConfiguration : IEntityTypeConfiguration<SurgeryBooking>
{
    public void Configure(EntityTypeBuilder<SurgeryBooking> builder)
    {
        builder.ToTable("SurgeryBookings", "surgery");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingProtocolNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(b => b.BookingProtocolNumber)
            .IsUnique();

        builder.Property(b => b.PatientId)
            .IsRequired();

        builder.Property(b => b.DepartmentId)
            .IsRequired();

        builder.Property(b => b.DepartmentName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(b => b.ProcedureName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(b => b.ProcedureCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(b => b.Urgency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(b => b.OperatingRoomId)
            .IsRequired();

        builder.Property(b => b.LeadSurgeonDoctorId)
            .IsRequired();

        builder.Property(b => b.AnesthesiologistDoctorId)
            .IsRequired();

        builder.Property(b => b.ScheduledStartTimeUtc)
            .IsRequired();

        builder.Property(b => b.ScheduledEndTimeUtc)
            .IsRequired();

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(b => b.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.CreatedAtUtc)
            .IsRequired();

        builder.Property(b => b.UpdatedAtUtc)
            .IsRequired();

        builder.Property(b => b.Version)
            .IsRowVersion();

        builder.OwnsOne(b => b.PreOpChecklist, check =>
        {
            check.Property(c => c.ConsentSigned).HasColumnName("ChecklistConsentSigned");
            check.Property(c => c.AnesthesiaClearance).HasColumnName("ChecklistAnesthesiaClearance");
            check.Property(c => c.NpoConfirmed).HasColumnName("ChecklistNpoConfirmed");
            check.Property(c => c.BloodProductsReserved).HasColumnName("ChecklistBloodProductsReserved");
            check.Property(c => c.SiteMarked).HasColumnName("ChecklistSiteMarked");
            check.Property(c => c.AllergyChecked).HasColumnName("ChecklistAllergyChecked");
            check.Property(c => c.CompletedByStaffId).HasColumnName("ChecklistCompletedByStaffId");
            check.Property(c => c.CompletedAtUtc).HasColumnName("ChecklistCompletedAtUtc");
            check.Property(c => c.Notes).HasMaxLength(2000).HasColumnName("ChecklistNotes");
        });

        builder.HasIndex(b => new { b.OperatingRoomId, b.ScheduledStartTimeUtc, b.ScheduledEndTimeUtc });
        builder.HasIndex(b => new { b.LeadSurgeonDoctorId, b.ScheduledStartTimeUtc, b.ScheduledEndTimeUtc });
        builder.HasIndex(b => new { b.AnesthesiologistDoctorId, b.ScheduledStartTimeUtc, b.ScheduledEndTimeUtc });
        builder.HasIndex(b => b.PatientId);
        builder.HasIndex(b => b.Status);
    }
}
