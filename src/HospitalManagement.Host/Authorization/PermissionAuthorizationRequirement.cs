using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.Host.Authorization;

public sealed class PermissionAuthorizationRequirement(string permission, bool isKnown) : IAuthorizationRequirement
{
    public string Permission { get; } = permission ?? string.Empty;

    public bool IsKnown { get; } = isKnown;
}
