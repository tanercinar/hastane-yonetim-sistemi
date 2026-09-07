using HospitalManagement.Modules.Organization.Domain;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Organization.Infrastructure.Persistence;

internal static class OrganizationDemoData
{
    internal static readonly Guid HospitalId = new("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid FacilityId = new("20000000-0000-0000-0000-000000000001");

    private static readonly DateTime CreatedAtUtc = new(
        2026,
        8,
        27,
        0,
        0,
        0,
        DateTimeKind.Utc);

    internal static void Configure(ModelBuilder modelBuilder)
    {
        var hospital = Hospital.Create(
            HospitalId,
            "DEMO-HOSPITAL",
            "DEMO Eğitim Hastanesi",
            CreatedAtUtc);
        hospital.Version = 1;

        var facility = Facility.Create(
            FacilityId,
            HospitalId,
            "DEMO-CENTRAL",
            "DEMO Merkez Şube",
            CreatedAtUtc);
        facility.Version = 1;

        modelBuilder.Entity<Hospital>().HasData(hospital);
        modelBuilder.Entity<Facility>().HasData(facility);
        modelBuilder.Entity<Department>().HasData(CreateDepartments());
        modelBuilder.Entity<Specialty>().HasData(CreateSpecialties());
    }

    private static Department[] CreateDepartments()
    {
        var clinicalServicesId = new Guid("30000000-0000-0000-0000-000000000001");
        var diagnosticServicesId = new Guid("30000000-0000-0000-0000-000000000004");

        return
        [
            CreateDepartment(
                clinicalServicesId,
                null,
                "DEMO-CLINICAL-SERVICES",
                "DEMO Klinik Hizmetler",
                DepartmentCategory.Clinical),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000002"),
                clinicalServicesId,
                "DEMO-INTERNAL-MEDICINE",
                "DEMO İç Hastalıkları Bölümü",
                DepartmentCategory.Clinical),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000003"),
                clinicalServicesId,
                "DEMO-CARDIOLOGY",
                "DEMO Kardiyoloji Bölümü",
                DepartmentCategory.Clinical),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000008"),
                clinicalServicesId,
                "DEMO-INTENSIVE-CARE",
                "DEMO Yoğun Bakım Bölümü",
                DepartmentCategory.Clinical),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000009"),
                clinicalServicesId,
                "DEMO-GENERAL-SURGERY",
                "DEMO Genel Cerrahi Bölümü",
                DepartmentCategory.Clinical),
            CreateDepartment(
                diagnosticServicesId,
                null,
                "DEMO-DIAGNOSTIC-SERVICES",
                "DEMO Tanı Hizmetleri",
                DepartmentCategory.Diagnostic),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000005"),
                diagnosticServicesId,
                "DEMO-LABORATORY",
                "DEMO Laboratuvar Bölümü",
                DepartmentCategory.Diagnostic),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000006"),
                diagnosticServicesId,
                "DEMO-RADIOLOGY",
                "DEMO Radyoloji Bölümü",
                DepartmentCategory.Diagnostic),
            CreateDepartment(
                new("30000000-0000-0000-0000-000000000007"),
                null,
                "DEMO-PHARMACY",
                "DEMO Eczane Bölümü",
                DepartmentCategory.Operational),
        ];
    }

    private static Specialty[] CreateSpecialties()
    {
        return
        [
            CreateSpecialty(
                new("40000000-0000-0000-0000-000000000001"),
                "DEMO-INTERNAL-MEDICINE",
                "DEMO İç Hastalıkları"),
            CreateSpecialty(
                new("40000000-0000-0000-0000-000000000002"),
                "DEMO-CARDIOLOGY",
                "DEMO Kardiyoloji"),
            CreateSpecialty(
                new("40000000-0000-0000-0000-000000000003"),
                "DEMO-MEDICAL-BIOCHEMISTRY",
                "DEMO Tıbbi Biyokimya"),
            CreateSpecialty(
                new("40000000-0000-0000-0000-000000000004"),
                "DEMO-RADIOLOGY",
                "DEMO Radyoloji"),
            CreateSpecialty(
                new("40000000-0000-0000-0000-000000000005"),
                "DEMO-GENERAL-SURGERY",
                "DEMO Genel Cerrahi"),
        ];
    }

    private static Department CreateDepartment(
        Guid id,
        Guid? parentDepartmentId,
        string code,
        string displayName,
        DepartmentCategory category)
    {
        var department = Department.Create(
            id,
            HospitalId,
            FacilityId,
            parentDepartmentId,
            code,
            displayName,
            category,
            CreatedAtUtc);
        department.Version = 1;
        return department;
    }

    private static Specialty CreateSpecialty(Guid id, string code, string displayName)
    {
        var specialty = Specialty.Create(id, HospitalId, code, displayName, CreatedAtUtc);
        specialty.Version = 1;
        return specialty;
    }
}
