using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Application;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Pharmacy;

public sealed class PrescriptionAccessContext(
    ClinicalRecordAccessControl clinicalAccessControl,
    ICareRelationshipEvaluator careRelationshipEvaluator,
    OrganizationDbContext organizationDbContext) : IPrescriptionAccessContext
{
    private readonly ClinicalRecordAccessControl _clinicalAccessControl = clinicalAccessControl;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;
    private readonly OrganizationDbContext _organizationDbContext = organizationDbContext;

    public async Task<PrescriptionEncounterContext?> FindEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        var encounter = await _clinicalAccessControl.FindEncounterAsync(
            encounterId,
            cancellationToken);

        return encounter is null
            ? null
            : new PrescriptionEncounterContext(
                encounter.Id,
                encounter.PatientId,
                encounter.DepartmentId,
                ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter));
    }

    public async Task<bool> CanAccessEncounterAsync(
        ClaimsPrincipal actor,
        PrescriptionEncounterContext encounter,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default)
    {
        var clinicalEncounter = await _clinicalAccessControl.FindEncounterAsync(
            encounter.EncounterId,
            cancellationToken);

        return clinicalEncounter is not null
            && await _clinicalAccessControl.CanAccessEncounterAsync(
                actor,
                clinicalEncounter,
                permission,
                allowPatientOwnRecord,
                cancellationToken);
    }

    public Task<bool> CanAccessPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken = default) =>
        _clinicalAccessControl.CanAccessPatientAsync(
            actor,
            patientId,
            permission,
            allowPatientOwnRecord,
            cancellationToken);

    public Task<bool> IsAssignedToDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        TryGetActorPersonId(actor, out var personId)
            ? _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                personId,
                departmentId,
                cancellationToken)
            : Task.FromResult(false);

    public async Task<bool> IsAssignedToFacilityForDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorPersonId(actor, out var personId))
        {
            return false;
        }

        var facilityId = await _organizationDbContext.Departments
            .AsNoTracking()
            .Where(department => department.Id == departmentId)
            .Select(department => (Guid?)department.FacilityId)
            .SingleOrDefaultAsync(cancellationToken);

        return facilityId.HasValue
            && await _careRelationshipEvaluator.IsAssignedToFacilityAsync(
                personId,
                facilityId.Value,
                cancellationToken);
    }

    private static bool TryGetActorPersonId(ClaimsPrincipal actor, out Guid personId) =>
        Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out personId)
        && personId != Guid.Empty;
}
