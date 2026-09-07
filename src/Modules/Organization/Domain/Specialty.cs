using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Organization.Domain;

public sealed class Specialty : IHasConcurrencyVersion
{
    private Specialty()
    {
    }

    private Specialty(
        Guid id,
        Guid hospitalId,
        string code,
        string displayName,
        DateTime createdAtUtc)
    {
        Id = OrganizationDomainRules.RequireId(id, nameof(id));
        HospitalId = OrganizationDomainRules.RequireId(hospitalId, nameof(hospitalId));
        Code = OrganizationDomainRules.NormalizeCode(code, nameof(code), 40);
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

    public Guid HospitalId
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

    public static Specialty Create(
        Guid id,
        Guid hospitalId,
        string code,
        string displayName,
        DateTime createdAtUtc) =>
        new(id, hospitalId, code, displayName, createdAtUtc);
}
