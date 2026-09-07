namespace HospitalManagement.BuildingBlocks.Authorization;

public sealed record ResourceScopedData(
    Guid? PatientId = null,
    Guid? DepartmentId = null,
    Guid? FacilityId = null,
    Guid? AssignedStaffId = null,
    Guid? OwnerUserId = null) : IResourceScoped
{
    public static ResourceScopedData ForPatient(Guid patientId) =>
        new(PatientId: patientId);

    public static ResourceScopedData ForDepartment(Guid departmentId) =>
        new(DepartmentId: departmentId);

    public static ResourceScopedData ForFacility(Guid facilityId) =>
        new(FacilityId: facilityId);

    public static ResourceScopedData ForAssignedStaff(Guid assignedStaffId) =>
        new(AssignedStaffId: assignedStaffId);

    public static ResourceScopedData ForOwner(Guid ownerUserId) =>
        new(OwnerUserId: ownerUserId);

    public static ResourceScopedData ForClinicalEncounter(
        Guid patientId,
        Guid? departmentId = null,
        Guid? assignedStaffId = null) =>
        new(
            PatientId: patientId,
            DepartmentId: departmentId,
            AssignedStaffId: assignedStaffId);
}

