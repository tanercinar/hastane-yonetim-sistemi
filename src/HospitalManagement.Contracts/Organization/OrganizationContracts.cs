using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Contracts.Organization;

public sealed class AssignStaffDepartmentRequest
{
    [Required]
    public Guid HospitalId
    {
        get; set;
    }

    [Required]
    public Guid StaffProfileId
    {
        get; set;
    }

    [Required]
    public Guid DepartmentId
    {
        get; set;
    }

    public bool IsPrimary { get; set; } = true;
}

public sealed record StaffDepartmentAssignmentResponse(
    Guid Id,
    Guid HospitalId,
    Guid StaffProfileId,
    Guid DepartmentId,
    bool IsPrimary,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc);
