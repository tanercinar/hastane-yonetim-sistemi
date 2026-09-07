namespace HospitalManagement.BuildingBlocks.Authorization;

public interface ICareRelationshipEvaluator
{
    Task<bool> HasActiveCareRelationshipAsync(
        Guid clinicianPersonId,
        Guid patientPersonId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToDepartmentAsync(
        Guid staffPersonId,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsAssignedToFacilityAsync(
        Guid staffPersonId,
        Guid facilityId,
        CancellationToken cancellationToken = default);
}

