using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagement.Host.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddHospitalAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, HospitalUserClaimsPrincipalFactory>();
        services.AddSingleton<IAuthorizationPolicyProvider, HospitalAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddSingleton<CareRelationshipRegistry>();
        services.AddScoped<ICareRelationshipEvaluator, CareRelationshipEvaluator>();
        services.AddScoped<IAuthorizationHandler, ResourceScopeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RecentAuthenticationAuthorizationHandler>();

        return services;
    }
}

