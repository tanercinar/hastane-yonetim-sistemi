namespace HospitalManagement.BuildingBlocks.Authorization;

public sealed record PermissionDefinition(
    string Name,
    string Category,
    string Description,
    IReadOnlyList<string> DefaultRoles);

