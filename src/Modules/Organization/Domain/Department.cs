using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Organization.Domain;

public sealed class Department : IHasConcurrencyVersion
{
    private Department()
    {
    }

    private Department(
        Guid id,
        Guid hospitalId,
        Guid facilityId,
        Guid? parentDepartmentId,
        string code,
        string displayName,
        DepartmentCategory category,
        DateTime createdAtUtc)
    {
        Id = OrganizationDomainRules.RequireId(id, nameof(id));
        HospitalId = OrganizationDomainRules.RequireId(hospitalId, nameof(hospitalId));
        FacilityId = OrganizationDomainRules.RequireId(facilityId, nameof(facilityId));
        if (parentDepartmentId == Guid.Empty)
        {
            throw new ArgumentException("Parent department identifier cannot be empty.", nameof(parentDepartmentId));
        }
        if (parentDepartmentId == id)
        {
            throw new ArgumentException("A department cannot be its own parent.", nameof(parentDepartmentId));
        }
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        ParentDepartmentId = parentDepartmentId;
        Code = OrganizationDomainRules.NormalizeCode(code, nameof(code), 40);
        DisplayName = OrganizationDomainRules.NormalizeRequiredText(
            displayName,
            nameof(displayName),
            160);
        Category = category;
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

    public Guid FacilityId
    {
        get; private set;
    }

    public Guid? ParentDepartmentId
    {
        get; private set;
    }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public DepartmentCategory Category
    {
        get; private set;
    }

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

    public static Department Create(
        Guid id,
        Guid hospitalId,
        Guid facilityId,
        Guid? parentDepartmentId,
        string code,
        string displayName,
        DepartmentCategory category,
        DateTime createdAtUtc) =>
        new(
            id,
            hospitalId,
            facilityId,
            parentDepartmentId,
            code,
            displayName,
            category,
            createdAtUtc);
}
