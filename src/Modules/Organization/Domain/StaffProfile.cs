using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Organization.Domain;

public sealed class StaffProfile : IHasConcurrencyVersion
{
    private StaffProfile()
    {
    }

    private StaffProfile(
        Guid id,
        Guid hospitalId,
        Guid personId,
        string staffNumber,
        ClinicalProfession profession,
        Guid? primarySpecialtyId,
        DateTime createdAtUtc)
    {
        Id = OrganizationDomainRules.RequireId(id, nameof(id));
        HospitalId = OrganizationDomainRules.RequireId(hospitalId, nameof(hospitalId));
        PersonId = OrganizationDomainRules.RequireId(personId, nameof(personId));
        if (!Enum.IsDefined(profession))
        {
            throw new ArgumentOutOfRangeException(nameof(profession));
        }
        if (primarySpecialtyId == Guid.Empty)
        {
            throw new ArgumentException("Primary specialty identifier cannot be empty.", nameof(primarySpecialtyId));
        }

        StaffNumber = OrganizationDomainRules.NormalizeCode(
            staffNumber,
            nameof(staffNumber),
            40);
        Profession = profession;
        PrimarySpecialtyId = primarySpecialtyId;
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

    /// <summary>
    /// Opaque reference to the person owner; no cross-module database foreign key is created.
    /// </summary>
    public Guid PersonId
    {
        get; private set;
    }

    public string StaffNumber { get; private set; } = string.Empty;

    public ClinicalProfession Profession
    {
        get; private set;
    }

    public Guid? PrimarySpecialtyId
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

    public static StaffProfile Create(
        Guid id,
        Guid hospitalId,
        Guid personId,
        string staffNumber,
        ClinicalProfession profession,
        Guid? primarySpecialtyId,
        DateTime createdAtUtc) =>
        new(
            id,
            hospitalId,
            personId,
            staffNumber,
            profession,
            primarySpecialtyId,
            createdAtUtc);
}
