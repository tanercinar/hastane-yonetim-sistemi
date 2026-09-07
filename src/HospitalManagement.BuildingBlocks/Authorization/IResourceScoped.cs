namespace HospitalManagement.BuildingBlocks.Authorization;

public interface IResourceScoped
{
    Guid? PatientId => null;

    Guid? DepartmentId => null;

    Guid? FacilityId => null;

    Guid? AssignedStaffId => null;

    Guid? OwnerUserId => null;
}

