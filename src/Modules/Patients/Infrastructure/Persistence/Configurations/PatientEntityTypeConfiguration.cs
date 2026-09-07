using HospitalManagement.Modules.Patients.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Patients.Infrastructure.Persistence.Configurations;

public sealed class PatientEntityTypeConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("patient_records", PatientsDbContext.Schema);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        builder.Property(p => p.MedicalRecordNumber)
            .HasColumnName("medical_record_number")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(p => p.NationalIdSynthetic)
            .HasColumnName("national_id_synthetic")
            .HasMaxLength(32);

        builder.Property(p => p.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(32);

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.OwnsOne(p => p.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.City)
                .HasColumnName("address_city")
                .HasMaxLength(64);
            addressBuilder.Property(a => a.District)
                .HasColumnName("address_district")
                .HasMaxLength(64);
            addressBuilder.Property(a => a.Line1)
                .HasColumnName("address_line1")
                .HasMaxLength(256);
            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("address_postal_code")
                .HasMaxLength(16);
        });

        builder.OwnsOne(p => p.EmergencyContact, contactBuilder =>
        {
            contactBuilder.Property(c => c.FullName)
                .HasColumnName("emergency_contact_name")
                .HasMaxLength(128);
            contactBuilder.Property(c => c.Relationship)
                .HasColumnName("emergency_contact_relationship")
                .HasMaxLength(64);
            contactBuilder.Property(c => c.PhoneNumber)
                .HasColumnName("emergency_contact_phone")
                .HasMaxLength(32);
        });

        builder.OwnsOne(p => p.CommunicationPreferences, prefBuilder =>
        {
            prefBuilder.Property(c => c.AllowSms)
                .HasColumnName("pref_allow_sms")
                .IsRequired();
            prefBuilder.Property(c => c.AllowEmail)
                .HasColumnName("pref_allow_email")
                .IsRequired();
            prefBuilder.Property(c => c.PreferredLanguage)
                .HasColumnName("pref_language")
                .HasMaxLength(8)
                .IsRequired();
        });

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.Property(p => p.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(p => p.MedicalRecordNumber)
            .IsUnique()
            .HasDatabaseName("ux_patient_records_mrn");

        builder.HasIndex(p => p.PersonId)
            .IsUnique()
            .HasDatabaseName("ux_patient_records_person_id");

        builder.HasIndex(p => new { p.LastName, p.FirstName, p.DateOfBirth })
            .HasDatabaseName("ix_patient_records_name_dob");

        builder.HasIndex(p => p.NationalIdSynthetic)
            .HasDatabaseName("ix_patient_records_national_id");
    }
}
