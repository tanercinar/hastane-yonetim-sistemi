using HospitalManagement.BuildingBlocks.Authorization;

namespace HospitalManagement.UnitTests;

public sealed class ResourceScopeContractTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public void ResourceScopedDataFactoryMethodsInitializeCorrectFields()
    {
        var patientId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var patientData = ResourceScopedData.ForPatient(patientId);
        Assert.Equal(patientId, patientData.PatientId);
        Assert.Null(patientData.DepartmentId);
        Assert.Null(patientData.FacilityId);

        var deptData = ResourceScopedData.ForDepartment(departmentId);
        Assert.Equal(departmentId, deptData.DepartmentId);
        Assert.Null(deptData.PatientId);

        var facilityData = ResourceScopedData.ForFacility(facilityId);
        Assert.Equal(facilityId, facilityData.FacilityId);

        var staffData = ResourceScopedData.ForAssignedStaff(staffId);
        Assert.Equal(staffId, staffData.AssignedStaffId);

        var ownerData = ResourceScopedData.ForOwner(ownerId);
        Assert.Equal(ownerId, ownerData.OwnerUserId);

        var encounterData = ResourceScopedData.ForClinicalEncounter(patientId, departmentId, staffId);
        Assert.Equal(patientId, encounterData.PatientId);
        Assert.Equal(departmentId, encounterData.DepartmentId);
        Assert.Equal(staffId, encounterData.AssignedStaffId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public void ResourceScopeEnumHasExpectedMembers()
    {
        var scopes = Enum.GetValues<ResourceScope>();
        Assert.Contains(ResourceScope.Own, scopes);
        Assert.Contains(ResourceScope.Assigned, scopes);
        Assert.Contains(ResourceScope.CareTeam, scopes);
        Assert.Contains(ResourceScope.Department, scopes);
        Assert.Contains(ResourceScope.Facility, scopes);
        Assert.Contains(ResourceScope.Organization, scopes);
        Assert.Contains(ResourceScope.Deidentified, scopes);
        Assert.Contains(ResourceScope.System, scopes);
    }
}

