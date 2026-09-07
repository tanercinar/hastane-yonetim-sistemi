using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure;

/// <summary>
/// Applies the clinical access decision in one place. A role name never grants access by itself:
/// callers must hold the requested permission and match the resource through ownership, encounter
/// participation, an active care relationship, or (for the chief medical officer) department scope.
/// </summary>
public sealed class ClinicalRecordAccessControl(
    ClinicalRecordsDbContext dbContext,
    ICareRelationshipEvaluator careRelationshipEvaluator)
{
    private readonly ClinicalRecordsDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator
        ?? throw new ArgumentNullException(nameof(careRelationshipEvaluator));

    public static bool HasPermission(ClaimsPrincipal actor, string permission)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return actor.Identity?.IsAuthenticated == true
            && actor.HasClaim(HospitalClaimTypes.Permission, permission);
    }

    public static bool TryGetActorPersonId(ClaimsPrincipal actor, out Guid personId)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out personId)
            && personId != Guid.Empty;
    }

    public static bool IsPatientOwnRecord(ClaimsPrincipal actor, Guid patientId)
    {
        return actor.IsInRole(HospitalRoles.Patient)
            && TryGetActorPersonId(actor, out var actorPersonId)
            && actorPersonId == patientId;
    }

    public async Task<bool> CanCreateEncounterAsync(
        ClaimsPrincipal actor,
        Guid primaryPractitionerId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        if (!HasPermission(actor, HospitalPermissions.ClinicalRecords.EncounterStart)
            || !TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        if (!await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                departmentId,
                cancellationToken))
        {
            return false;
        }

        return actorPersonId == primaryPractitionerId
            || actor.IsInRole(HospitalRoles.ChiefMedicalOfficer);
    }

    public async Task<bool> CanAccessEncounterAsync(
        ClaimsPrincipal actor,
        Encounter encounter,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(encounter);

        if (allowPatientOwnRecord && IsPatientOwnRecord(actor, encounter.PatientId))
        {
            return true;
        }

        if (!HasPermission(actor, permission)
            || !TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        if (encounter.PrimaryPractitionerId == actorPersonId
            || encounter.Participants.Any(participant =>
                participant.PractitionerId == actorPersonId && participant.LeftAtUtc is null))
        {
            return true;
        }

        if (await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                encounter.PatientId,
                cancellationToken))
        {
            return true;
        }

        return actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                encounter.DepartmentId,
                cancellationToken);
    }

    public async Task<bool> CanAccessPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        string permission,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken)
    {
        if (allowPatientOwnRecord && IsPatientOwnRecord(actor, patientId))
        {
            return true;
        }

        if (!HasPermission(actor, permission)
            || !TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        if (await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                patientId,
                cancellationToken))
        {
            return true;
        }

        var encounterRelationship = await _dbContext.Encounters
            .AsNoTracking()
            .AnyAsync(
                encounter => encounter.PatientId == patientId
                    && encounter.Status != EncounterStatus.Cancelled
                    && encounter.Status != EncounterStatus.EnteredInError
                    && (encounter.PrimaryPractitionerId == actorPersonId
                        || encounter.Participants.Any(participant =>
                            participant.PractitionerId == actorPersonId
                            && participant.LeftAtUtc == null)),
                cancellationToken);

        if (encounterRelationship)
        {
            return true;
        }

        if (!actor.IsInRole(HospitalRoles.ChiefMedicalOfficer))
        {
            return false;
        }

        var departmentIds = await _dbContext.Encounters
            .AsNoTracking()
            .Where(encounter => encounter.PatientId == patientId
                && encounter.Status != EncounterStatus.Cancelled
                && encounter.Status != EncounterStatus.EnteredInError)
            .Select(encounter => encounter.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var departmentId in departmentIds)
        {
            if (await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                    actorPersonId,
                    departmentId,
                    cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> CanAccessDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        string permission,
        CancellationToken cancellationToken)
    {
        return HasPermission(actor, permission)
            && TryGetActorPersonId(actor, out var actorPersonId)
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                departmentId,
                cancellationToken);
    }

    public Task<bool> IsPractitionerAssignedToDepartmentAsync(
        Guid practitionerId,
        Guid departmentId,
        CancellationToken cancellationToken) =>
        practitionerId != Guid.Empty && departmentId != Guid.Empty
            ? _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                practitionerId,
                departmentId,
                cancellationToken)
            : Task.FromResult(false);

    public async Task<Encounter?> FindEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken,
        bool tracked = false)
    {
        var query = _dbContext.Encounters.Include(encounter => encounter.Participants);
        return tracked
            ? await query.FirstOrDefaultAsync(encounter => encounter.Id == encounterId, cancellationToken)
            : await query.AsNoTracking().FirstOrDefaultAsync(encounter => encounter.Id == encounterId, cancellationToken);
    }

    public static bool IsEncounterOpenForClinicalEntry(Encounter encounter) =>
        encounter.Status == EncounterStatus.InProgress;
}
