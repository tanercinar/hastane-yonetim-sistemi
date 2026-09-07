using HospitalManagement.BuildingBlocks.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Authorization;

public sealed class HospitalAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        var existingPolicy = await base.GetPolicyAsync(policyName);
        if (existingPolicy is not null)
        {
            return existingPolicy;
        }

        var isKnown = HospitalPermissionCatalog.IsKnown(policyName);
        var builder = new AuthorizationPolicyBuilder();

        if (isKnown)
        {
            builder.RequireAuthenticatedUser();
            builder.AddRequirements(new PermissionAuthorizationRequirement(policyName, isKnown: true));
        }
        else
        {
            // Bilinmeyen izinler için varsayılan ret (Always Deny) ilkesi
            builder.AddRequirements(new PermissionAuthorizationRequirement(policyName, isKnown: false));
        }

        return builder.Build();
    }
}

