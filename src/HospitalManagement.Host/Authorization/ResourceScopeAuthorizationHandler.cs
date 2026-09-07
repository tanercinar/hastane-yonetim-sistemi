using HospitalManagement.BuildingBlocks.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.Host.Authorization;

public sealed class ResourceScopeAuthorizationHandler(
    ICareRelationshipEvaluator careRelationshipEvaluator)
    : AuthorizationHandler<ResourceScopeAuthorizationRequirement, IResourceScoped>
{
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceScopeAuthorizationRequirement requirement,
        IResourceScoped resource)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(resource);

        if (!requirement.IsKnown)
        {
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (!context.User.HasClaim(HospitalClaimTypes.Permission, requirement.Permission))
        {
            return;
        }

        var personIdClaim = context.User.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (!Guid.TryParse(personIdClaim, out var userPersonId) || userPersonId == Guid.Empty)
        {
            return;
        }

        var scopeGranted = requirement.Scope switch
        {
            ResourceScope.Own =>
                (resource.PatientId.HasValue && resource.PatientId.Value == userPersonId) ||
                (resource.OwnerUserId.HasValue && resource.OwnerUserId.Value == userPersonId),

            ResourceScope.CareTeam =>
                (resource.PatientId.HasValue && resource.PatientId.Value == userPersonId) ||
                (resource.PatientId.HasValue && await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(userPersonId, resource.PatientId.Value)),

            ResourceScope.Department =>
                resource.DepartmentId.HasValue &&
                await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(userPersonId, resource.DepartmentId.Value),

            ResourceScope.Assigned =>
                resource.AssignedStaffId.HasValue && resource.AssignedStaffId.Value == userPersonId,

            ResourceScope.Facility =>
                resource.FacilityId.HasValue &&
                await _careRelationshipEvaluator.IsAssignedToFacilityAsync(userPersonId, resource.FacilityId.Value),

            ResourceScope.System or ResourceScope.Deidentified or ResourceScope.Organization =>
                true,

            _ => false,
        };

        if (scopeGranted)
        {
            context.Succeed(requirement);
        }
    }
}

