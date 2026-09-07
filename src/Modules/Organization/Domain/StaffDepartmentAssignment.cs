using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.Organization.Domain;

public sealed class StaffDepartmentAssignment : IHasConcurrencyVersion
{
    private StaffDepartmentAssignment()
    {
    }

    private StaffDepartmentAssignment(
        Guid id,
        Guid hospitalId,
        Guid staffProfileId,
        Guid departmentId,
        bool isPrimary,
        DateTime startsAtUtc,
        DateTime? endsAtUtc)
    {
        Id = OrganizationDomainRules.RequireId(id, nameof(id));
        HospitalId = OrganizationDomainRules.RequireId(hospitalId, nameof(hospitalId));
        StaffProfileId = OrganizationDomainRules.RequireId(staffProfileId, nameof(staffProfileId));
        DepartmentId = OrganizationDomainRules.RequireId(departmentId, nameof(departmentId));
        StartsAtUtc = OrganizationDomainRules.RequireUtc(startsAtUtc, nameof(startsAtUtc));
        if (endsAtUtc.HasValue)
        {
            EndsAtUtc = OrganizationDomainRules.RequireUtc(endsAtUtc.Value, nameof(endsAtUtc));
            if (EndsAtUtc <= StartsAtUtc)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endsAtUtc),
                    "Assignment end must be later than its start.");
            }
        }

        IsPrimary = isPrimary;
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid HospitalId
    {
        get; private set;
    }

    public Guid StaffProfileId
    {
        get; private set;
    }

    public Guid DepartmentId
    {
        get; private set;
    }

    public bool IsPrimary
    {
        get; private set;
    }

    public DateTime StartsAtUtc
    {
        get; private set;
    }

    public DateTime? EndsAtUtc
    {
        get; private set;
    }

    public long Version
    {
        get; set;
    }

    public static StaffDepartmentAssignment Create(
        Guid id,
        Guid hospitalId,
        Guid staffProfileId,
        Guid departmentId,
        bool isPrimary,
        DateTime startsAtUtc,
        DateTime? endsAtUtc = null) =>
        new(
            id,
            hospitalId,
            staffProfileId,
            departmentId,
            isPrimary,
            startsAtUtc,
            endsAtUtc);

    public void End(DateTime endsAtUtc)
    {
        var validatedEnd = OrganizationDomainRules.RequireUtc(endsAtUtc, nameof(endsAtUtc));
        if (EndsAtUtc.HasValue)
        {
            throw new InvalidOperationException("Assignment is already ended.");
        }
        if (validatedEnd <= StartsAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endsAtUtc),
                "Assignment end must be later than its start.");
        }

        EndsAtUtc = validatedEnd;
    }
}
