namespace HospitalManagement.BuildingBlocks.Authorization;

public static class RolePermissionDefaults
{
    private static readonly Dictionary<string, IReadOnlySet<string>> RolePermissions =
        BuildRolePermissions();

    public static IReadOnlyDictionary<string, IReadOnlySet<string>> GetAllRolePermissions() =>
        RolePermissions;

    public static IReadOnlySet<string> GetPermissionsForRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return FrozenEmptySet;
        }

        return RolePermissions.TryGetValue(role.Trim(), out var permissions)
            ? permissions
            : FrozenEmptySet;
    }

    public static IReadOnlySet<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var combined = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                continue;
            }

            if (RolePermissions.TryGetValue(role.Trim(), out var permissions))
            {
                foreach (var permission in permissions)
                {
                    combined.Add(permission);
                }
            }
        }

        return combined;
    }

    private static readonly IReadOnlySet<string> FrozenEmptySet =
        new HashSet<string>(StringComparer.Ordinal);

    private static Dictionary<string, IReadOnlySet<string>> BuildRolePermissions()
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var role in HospitalRoles.All)
        {
            map[role.Code] = new HashSet<string>(StringComparer.Ordinal);
        }

        foreach (var permission in HospitalPermissionCatalog.All)
        {
            foreach (var role in permission.DefaultRoles)
            {
                if (map.TryGetValue(role, out var rolePermissions))
                {
                    rolePermissions.Add(permission.Name);
                }
            }
        }

        return map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlySet<string>)kvp.Value,
            StringComparer.Ordinal);
    }
}

