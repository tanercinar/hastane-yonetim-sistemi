using HospitalManagement.Modules.Organization.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence;

internal sealed class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> entity)
    {
        entity.ToTable("hospitals");
        entity.HasKey(hospital => hospital.Id).HasName("pk_hospitals");
        entity.Property(hospital => hospital.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(hospital => hospital.Code).HasColumnName("code").HasMaxLength(32);
        entity.Property(hospital => hospital.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(160);
        entity.Property(hospital => hospital.IsActive).HasColumnName("is_active");
        entity.Property(hospital => hospital.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(hospital => hospital.Version).HasColumnName("version");
        entity.HasIndex(hospital => hospital.Code)
            .IsUnique()
            .HasDatabaseName("ux_hospitals_code");
    }
}

internal sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> entity)
    {
        entity.ToTable("facilities");
        entity.HasKey(facility => facility.Id).HasName("pk_facilities");
        entity.HasAlternateKey(facility => new { facility.HospitalId, facility.Id })
            .HasName("ak_facilities_hospital_id_id");
        entity.Property(facility => facility.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(facility => facility.HospitalId).HasColumnName("hospital_id");
        entity.Property(facility => facility.Code).HasColumnName("code").HasMaxLength(32);
        entity.Property(facility => facility.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(160);
        entity.Property(facility => facility.IsActive).HasColumnName("is_active");
        entity.Property(facility => facility.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(facility => facility.Version).HasColumnName("version");
        entity.HasOne<Hospital>()
            .WithMany()
            .HasForeignKey(facility => facility.HospitalId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_facilities_hospitals_hospital_id");
        entity.HasIndex(facility => new { facility.HospitalId, facility.Code })
            .IsUnique()
            .HasDatabaseName("ux_facilities_hospital_id_code");
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> entity)
    {
        entity.ToTable("departments");
        entity.HasKey(department => department.Id).HasName("pk_departments");
        entity.HasAlternateKey(department => new { department.HospitalId, department.Id })
            .HasName("ak_departments_hospital_id_id");
        entity.HasAlternateKey(department => new
        {
            department.HospitalId,
            department.FacilityId,
            department.Id,
        }).HasName("ak_departments_hospital_id_facility_id_id");
        entity.Property(department => department.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(department => department.HospitalId).HasColumnName("hospital_id");
        entity.Property(department => department.FacilityId).HasColumnName("facility_id");
        entity.Property(department => department.ParentDepartmentId)
            .HasColumnName("parent_department_id");
        entity.Property(department => department.Code).HasColumnName("code").HasMaxLength(40);
        entity.Property(department => department.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(160);
        entity.Property(department => department.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(24);
        entity.Property(department => department.IsActive).HasColumnName("is_active");
        entity.Property(department => department.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(department => department.Version).HasColumnName("version");
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(department => new { department.HospitalId, department.FacilityId })
            .HasPrincipalKey(facility => new { facility.HospitalId, facility.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_departments_facilities_hospital_id_facility_id");
        entity.HasOne<Department>()
            .WithMany()
            .HasForeignKey(department => new
            {
                department.HospitalId,
                department.FacilityId,
                department.ParentDepartmentId,
            })
            .HasPrincipalKey(parent => new { parent.HospitalId, parent.FacilityId, parent.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_departments_parent_department");
        entity.HasIndex(department => new
        {
            department.HospitalId,
            department.FacilityId,
            department.Code,
        })
            .IsUnique()
            .HasDatabaseName("ux_departments_hospital_facility_code");
    }
}

internal sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> entity)
    {
        entity.ToTable("specialties");
        entity.HasKey(specialty => specialty.Id).HasName("pk_specialties");
        entity.HasAlternateKey(specialty => new { specialty.HospitalId, specialty.Id })
            .HasName("ak_specialties_hospital_id_id");
        entity.Property(specialty => specialty.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(specialty => specialty.HospitalId).HasColumnName("hospital_id");
        entity.Property(specialty => specialty.Code).HasColumnName("code").HasMaxLength(40);
        entity.Property(specialty => specialty.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(160);
        entity.Property(specialty => specialty.IsActive).HasColumnName("is_active");
        entity.Property(specialty => specialty.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(specialty => specialty.Version).HasColumnName("version");
        entity.HasOne<Hospital>()
            .WithMany()
            .HasForeignKey(specialty => specialty.HospitalId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_specialties_hospitals_hospital_id");
        entity.HasIndex(specialty => new { specialty.HospitalId, specialty.Code })
            .IsUnique()
            .HasDatabaseName("ux_specialties_hospital_id_code");
    }
}

internal sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> entity)
    {
        entity.ToTable("staff_profiles");
        entity.HasKey(profile => profile.Id).HasName("pk_staff_profiles");
        entity.HasAlternateKey(profile => new { profile.HospitalId, profile.Id })
            .HasName("ak_staff_profiles_hospital_id_id");
        entity.Property(profile => profile.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(profile => profile.HospitalId).HasColumnName("hospital_id");
        entity.Property(profile => profile.PersonId).HasColumnName("person_id");
        entity.Property(profile => profile.StaffNumber)
            .HasColumnName("staff_number")
            .HasMaxLength(40);
        entity.Property(profile => profile.Profession)
            .HasColumnName("profession")
            .HasConversion<string>()
            .HasMaxLength(48);
        entity.Property(profile => profile.PrimarySpecialtyId)
            .HasColumnName("primary_specialty_id");
        entity.Property(profile => profile.IsActive).HasColumnName("is_active");
        entity.Property(profile => profile.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(profile => profile.Version).HasColumnName("version");
        entity.HasOne<Hospital>()
            .WithMany()
            .HasForeignKey(profile => profile.HospitalId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_staff_profiles_hospitals_hospital_id");
        entity.HasOne<Specialty>()
            .WithMany()
            .HasForeignKey(profile => new { profile.HospitalId, profile.PrimarySpecialtyId })
            .HasPrincipalKey(specialty => new { specialty.HospitalId, specialty.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_staff_profiles_specialties_primary_specialty");
        entity.HasIndex(profile => new { profile.HospitalId, profile.StaffNumber })
            .IsUnique()
            .HasDatabaseName("ux_staff_profiles_hospital_id_staff_number");
        entity.HasIndex(profile => new { profile.HospitalId, profile.PersonId })
            .IsUnique()
            .HasDatabaseName("ux_staff_profiles_hospital_id_person_id");
    }
}

internal sealed class StaffDepartmentAssignmentConfiguration
    : IEntityTypeConfiguration<StaffDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<StaffDepartmentAssignment> entity)
    {
        entity.ToTable(
            "staff_department_assignments",
            table => table.HasCheckConstraint(
                "ck_staff_department_assignments_valid_interval",
                "ends_at_utc IS NULL OR ends_at_utc > starts_at_utc"));
        entity.HasKey(assignment => assignment.Id)
            .HasName("pk_staff_department_assignments");
        entity.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(assignment => assignment.HospitalId).HasColumnName("hospital_id");
        entity.Property(assignment => assignment.StaffProfileId)
            .HasColumnName("staff_profile_id");
        entity.Property(assignment => assignment.DepartmentId).HasColumnName("department_id");
        entity.Property(assignment => assignment.IsPrimary).HasColumnName("is_primary");
        entity.Property(assignment => assignment.StartsAtUtc).HasColumnName("starts_at_utc");
        entity.Property(assignment => assignment.EndsAtUtc).HasColumnName("ends_at_utc");
        entity.Property(assignment => assignment.Version).HasColumnName("version");
        entity.HasOne<StaffProfile>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.HospitalId, assignment.StaffProfileId })
            .HasPrincipalKey(profile => new { profile.HospitalId, profile.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_staff_assignments_staff_profiles_hospital_scope");
        entity.HasOne<Department>()
            .WithMany()
            .HasForeignKey(assignment => new { assignment.HospitalId, assignment.DepartmentId })
            .HasPrincipalKey(department => new { department.HospitalId, department.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_staff_assignments_departments_hospital_scope");
        entity.HasIndex(assignment => new
        {
            assignment.StaffProfileId,
            assignment.DepartmentId,
        })
            .IsUnique()
            .HasFilter("ends_at_utc IS NULL")
            .HasDatabaseName("ux_staff_assignments_active_staff_department");
        entity.HasIndex(assignment => assignment.StaffProfileId)
            .IsUnique()
            .HasFilter("is_primary AND ends_at_utc IS NULL")
            .HasDatabaseName("ux_staff_assignments_active_primary");
    }
}
