using HospitalManagement.BuildingBlocks.Authorization;

namespace HospitalManagement.Host.Authorization;

public static class AuthorizationEndpointExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder.RequireAuthorization(permission);
    }

    public static TBuilder RequireAnyPermission<TBuilder>(
        this TBuilder builder,
        params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(permissions);

        var candidates = permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new ArgumentException("En az bir izin belirtilmelidir.", nameof(permissions));
        }

        return builder.RequireAuthorization(policy => policy.RequireAssertion(context =>
            context.User.Identity?.IsAuthenticated == true
            && candidates
                .Where(HospitalPermissionCatalog.IsKnown)
                .Any(permission => context.User.HasClaim(HospitalClaimTypes.Permission, permission))));
    }
}
