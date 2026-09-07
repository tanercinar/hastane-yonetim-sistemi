using HospitalManagement.BuildingBlocks.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.Host.Authorization;

public sealed class ResourceScopeAuthorizationRequirement(
    ResourceScope scope,
    string permission) : IAuthorizationRequirement
{
    public ResourceScope Scope { get; } = scope;

    public string Permission { get; } = permission ?? string.Empty;

    public bool IsKnown { get; } = HospitalPermissionCatalog.IsKnown(permission ?? string.Empty);
}

