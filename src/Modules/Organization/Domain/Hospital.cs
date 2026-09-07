using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Organization.Domain;

public sealed class Hospital : IHasConcurrencyVersion
{
    private Hospital()
    {
    }

    private Hospital(
        Guid id,
        string code,
        string displayName,
        DateTime createdAtUtc)
    {
        Id = OrganizationDomainRules.RequireId(id, nameof(id));
        Code = OrganizationDomainRules.NormalizeCode(code, nameof(code), 32);
        DisplayName = OrganizationDomainRules.NormalizeRequiredText(
            displayName,
            nameof(displayName),
            160);
        CreatedAtUtc = OrganizationDomainRules.RequireUtc(createdAtUtc, nameof(createdAtUtc));
        IsActive = true;
    }

    public Guid Id
    {
        get; private set;
    }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsActive
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public long Version
    {
        get; set;
    }

    public static Hospital Create(
        Guid id,
        string code,
        string displayName,
        DateTime createdAtUtc) =>
        new(id, code, displayName, createdAtUtc);
}
