using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Authorization;

public sealed class CareRelationshipEvaluator(
    OrganizationDbContext organizationDbContext,
    CareRelationshipRegistry registry,
    TimeProvider timeProvider) : ICareRelationshipEvaluator
{
    private readonly OrganizationDbContext _organizationDbContext = organizationDbContext;
    private readonly CareRelationshipRegistry _registry = registry;
    private readonly TimeProvider _timeProvider = timeProvider;

    public Task<bool> HasActiveCareRelationshipAsync(
        Guid clinicianPersonId,
        Guid patientPersonId,
        CancellationToken cancellationToken = default)
    {
        var hasRelationship = _registry.HasCareRelationship(clinicianPersonId, patientPersonId);
        return Task.FromResult(hasRelationship);
    }

    public async Task<bool> IsAssignedToDepartmentAsync(
        Guid staffPersonId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (staffPersonId == Guid.Empty || departmentId == Guid.Empty)
        {
            return false;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var staffProfileIds = await _organizationDbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => profile.PersonId == staffPersonId && profile.IsActive)
            .Select(profile => profile.Id)
            .ToListAsync(cancellationToken);

        if (staffProfileIds.Count == 0)
        {
            return false;
        }

        var hasActiveAssignment = await _organizationDbContext.StaffDepartmentAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment => staffProfileIds.Contains(assignment.StaffProfileId)
                    && assignment.DepartmentId == departmentId
                    && assignment.StartsAtUtc <= nowUtc
                    && (assignment.EndsAtUtc == null || assignment.EndsAtUtc > nowUtc),
                cancellationToken);

        return hasActiveAssignment;
    }

    public async Task<bool> IsAssignedToFacilityAsync(
        Guid staffPersonId,
        Guid facilityId,
        CancellationToken cancellationToken = default)
    {
        if (staffPersonId == Guid.Empty || facilityId == Guid.Empty)
        {
            return false;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var staffProfileIds = await _organizationDbContext.StaffProfiles
            .AsNoTracking()
            .Where(profile => profile.PersonId == staffPersonId && profile.IsActive)
            .Select(profile => profile.Id)
            .ToListAsync(cancellationToken);

        if (staffProfileIds.Count == 0)
        {
            return false;
        }

        var assignedDepartmentIds = await _organizationDbContext.StaffDepartmentAssignments
            .AsNoTracking()
            .Where(assignment => staffProfileIds.Contains(assignment.StaffProfileId)
                && assignment.StartsAtUtc <= nowUtc
                && (assignment.EndsAtUtc == null || assignment.EndsAtUtc > nowUtc))
            .Select(assignment => assignment.DepartmentId)
            .ToListAsync(cancellationToken);

        if (assignedDepartmentIds.Count == 0)
        {
            return false;
        }

        var hasFacility = await _organizationDbContext.Departments
            .AsNoTracking()
            .AnyAsync(
                dept => assignedDepartmentIds.Contains(dept.Id) && dept.FacilityId == facilityId,
                cancellationToken);

        return hasFacility;
    }
}

