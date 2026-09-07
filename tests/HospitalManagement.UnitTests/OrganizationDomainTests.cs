using HospitalManagement.Modules.Organization.Domain;

namespace HospitalManagement.UnitTests;

public sealed class OrganizationDomainTests
{
    private static readonly DateTime DemoUtc = new(
        2026,
        8,
        27,
        9,
        0,
        0,
        DateTimeKind.Utc);

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G01")]
    public void OrganizationNodesNormalizeCodesWithoutCombiningDepartmentAndSpecialty()
    {
        var hospitalId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var hospital = Hospital.Create(
            hospitalId,
            " demo-hospital ",
            " DEMO Eğitim Hastanesi ",
            DemoUtc);
        var facility = Facility.Create(
            facilityId,
            hospitalId,
            " demo-central ",
            " DEMO Merkez Şube ",
            DemoUtc);
        var department = Department.Create(
            Guid.NewGuid(),
            hospitalId,
            facilityId,
            null,
            " demo-cardiology-department ",
            " DEMO Kardiyoloji Bölümü ",
            DepartmentCategory.Clinical,
            DemoUtc);
        var specialty = Specialty.Create(
            Guid.NewGuid(),
            hospitalId,
            " demo-cardiology-specialty ",
            " DEMO Kardiyoloji Uzmanlığı ",
            DemoUtc);

        Assert.Equal("DEMO-HOSPITAL", hospital.Code);
        Assert.Equal("DEMO Eğitim Hastanesi", hospital.DisplayName);
        Assert.Equal("DEMO-CENTRAL", facility.Code);
        Assert.Equal("DEMO-CARDIOLOGY-DEPARTMENT", department.Code);
        Assert.Equal(DepartmentCategory.Clinical, department.Category);
        Assert.Equal("DEMO-CARDIOLOGY-SPECIALTY", specialty.Code);
        Assert.NotEqual(department.Id, specialty.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G01")]
    public void InvalidDepartmentHierarchyAndAssignmentIntervalAreRejected()
    {
        var departmentId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => Department.Create(
            departmentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            departmentId,
            "DEMO-SELF-PARENT",
            "DEMO Hatalı Bölüm",
            DepartmentCategory.Clinical,
            DemoUtc));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                DemoUtc,
                DemoUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G01")]
    public void StaffProfileStoresOnlyClinicalScopeAttributes()
    {
        var personId = Guid.NewGuid();
        var specialtyId = Guid.NewGuid();
        var profile = StaffProfile.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            personId,
            " demo-staff-001 ",
            ClinicalProfession.Physician,
            specialtyId,
            DemoUtc);

        Assert.Equal(personId, profile.PersonId);
        Assert.Equal("DEMO-STAFF-001", profile.StaffNumber);
        Assert.Equal(ClinicalProfession.Physician, profile.Profession);
        Assert.Equal(specialtyId, profile.PrimarySpecialtyId);
        Assert.True(profile.IsActive);
    }
}
