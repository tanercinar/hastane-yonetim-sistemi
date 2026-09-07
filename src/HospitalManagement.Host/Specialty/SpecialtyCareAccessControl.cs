using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Modules.Organization.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Specialty;

/// <summary>
/// Enforces the patient-resource boundary for the phase 9 clinical verticals.
/// A role/permission alone never grants access to an arbitrary patient identifier.
/// </summary>
public sealed class SpecialtyCareAccessControl(
    SpecialtyCareDbContext specialtyCareDbContext,
    PatientsDbContext patientsDbContext,
    OrganizationDbContext organizationDbContext,
    ICareRelationshipEvaluator careRelationshipEvaluator,
    TimeProvider timeProvider)
{
    private readonly SpecialtyCareDbContext _specialtyCareDbContext = specialtyCareDbContext;
    private readonly PatientsDbContext _patientsDbContext = patientsDbContext;
    private readonly OrganizationDbContext _organizationDbContext = organizationDbContext;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<bool> CanAccessPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        string permission,
        CancellationToken cancellationToken = default,
        params Guid?[] assignedStaffIds)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, permission)
            || patientId == Guid.Empty)
        {
            return false;
        }

        var patientPersonId = await _patientsDbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.Id == patientId && patient.IsActive)
            .Select(patient => (Guid?)patient.PersonId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!patientPersonId.HasValue)
        {
            return false;
        }

        return actorPersonId == patientPersonId.Value
            || assignedStaffIds.Any(id => id.HasValue && id.Value == actorPersonId)
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                patientPersonId.Value,
                cancellationToken);
    }

    public async Task<Guid?> GetOwnPatientIdAsync(
        ClaimsPrincipal actor,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, permission))
        {
            return null;
        }

        return await _patientsDbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.PersonId == actorPersonId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanAccessPregnancyEpisodeAsync(
        ClaimsPrincipal actor,
        Guid episodeId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var resource = await _specialtyCareDbContext.PregnancyEpisodes
            .AsNoTracking()
            .Where(episode => episode.Id == episodeId)
            .Select(episode => new
            {
                episode.PatientId,
                episode.AssignedDoctorId,
                episode.AssignedMidwifeId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return resource is not null
            && await CanAccessPatientAsync(
                actor,
                resource.PatientId,
                permission,
                cancellationToken,
                resource.AssignedDoctorId,
                resource.AssignedMidwifeId);
    }

    public async Task<bool> CanAccessDeliveryAsync(
        ClaimsPrincipal actor,
        Guid deliveryId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var resource = await _specialtyCareDbContext.DeliveryRecords
            .AsNoTracking()
            .Where(delivery => delivery.Id == deliveryId)
            .Select(delivery => new
            {
                delivery.MotherPatientId,
                delivery.AttendingDoctorId,
                delivery.AssistingMidwifeId,
                delivery.PediatricianDoctorId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return resource is not null
            && await CanAccessPatientAsync(
                actor,
                resource.MotherPatientId,
                permission,
                cancellationToken,
                resource.AttendingDoctorId,
                resource.AssistingMidwifeId,
                resource.PediatricianDoctorId);
    }

    public async Task<bool> CanAccessDentalProcedureAsync(
        ClaimsPrincipal actor,
        Guid procedureId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var resource = await _specialtyCareDbContext.DentalProcedures
            .AsNoTracking()
            .Where(procedure => procedure.Id == procedureId)
            .Select(procedure => new
            {
                procedure.PatientId,
                procedure.PerformedByDoctorId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return resource is not null
            && await CanAccessPatientAsync(
                actor,
                resource.PatientId,
                permission,
                cancellationToken,
                resource.PerformedByDoctorId);
    }

    public async Task<bool> CanAccessHomeHealthVisitAsync(
        ClaimsPrincipal actor,
        Guid visitId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var resource = await _specialtyCareDbContext.HomeHealthVisits
            .AsNoTracking()
            .Where(visit => visit.Id == visitId)
            .Select(visit => new
            {
                visit.PatientId,
                visit.RequestedByStaffId,
                visit.AssignedStaffId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return resource is not null
            && await CanAccessPatientAsync(
                actor,
                resource.PatientId,
                permission,
                cancellationToken,
                resource.RequestedByStaffId,
                resource.AssignedStaffId);
    }

    public async Task<bool> IsAssignedHomeHealthStaffAsync(
        ClaimsPrincipal actor,
        Guid visitId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId) || !HasPermission(actor, permission))
        {
            return false;
        }

        return await _specialtyCareDbContext.HomeHealthVisits
            .AsNoTracking()
            .AnyAsync(
                visit => visit.Id == visitId && visit.AssignedStaffId == actorPersonId,
                cancellationToken);
    }

    public async Task<bool> IsHomeHealthEncounterAvailableAsync(
        Guid visitId,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        if (encounterId == Guid.Empty)
        {
            return false;
        }

        return !await _specialtyCareDbContext.HomeHealthVisits
            .AsNoTracking()
            .AnyAsync(
                visit => visit.Id != visitId && visit.EncounterId == encounterId,
                cancellationToken);
    }

    public static bool CanViewHomeAddress(ClaimsPrincipal actor, HomeHealthVisitDto visit) =>
        TryGetActorPersonId(actor, out var actorPersonId)
        && visit.AssignedStaffId.HasValue
        && visit.AssignedStaffId.Value == actorPersonId;

    public async Task<bool> IsValidPregnancyTeamAsync(
        Guid? doctorPersonId,
        Guid? midwifePersonId,
        CancellationToken cancellationToken = default) =>
        await IsOptionalActiveStaffAsync(
            doctorPersonId,
            [ClinicalProfession.Physician],
            cancellationToken)
        && await IsOptionalActiveStaffAsync(
            midwifePersonId,
            [ClinicalProfession.Nurse],
            cancellationToken);

    public async Task<bool> IsValidDeliveryTeamAsync(
        Guid attendingDoctorPersonId,
        Guid? assistingMidwifePersonId,
        Guid? pediatricianDoctorPersonId,
        CancellationToken cancellationToken = default) =>
        await IsActiveStaffAsync(
            attendingDoctorPersonId,
            [ClinicalProfession.Physician],
            cancellationToken)
        && await IsOptionalActiveStaffAsync(
            assistingMidwifePersonId,
            [ClinicalProfession.Nurse],
            cancellationToken)
        && await IsOptionalActiveStaffAsync(
            pediatricianDoctorPersonId,
            [ClinicalProfession.Physician],
            cancellationToken);

    public Task<bool> IsValidDentistAsync(
        Guid dentistPersonId,
        CancellationToken cancellationToken = default) =>
        IsActiveStaffAsync(
            dentistPersonId,
            [ClinicalProfession.Physician],
            cancellationToken);

    public Task<bool> IsValidHomeHealthAssigneeAsync(
        Guid staffPersonId,
        CancellationToken cancellationToken = default) =>
        IsActiveStaffAsync(
            staffPersonId,
            [ClinicalProfession.Physician, ClinicalProfession.Nurse],
            cancellationToken);

    public async Task<bool> AreValidNewbornPatientsAsync(
        Guid motherPatientId,
        IEnumerable<Guid> newbornPatientIds,
        CancellationToken cancellationToken = default)
    {
        var providedIds = newbornPatientIds.ToList();

        if (providedIds.Count == 0)
        {
            return true;
        }

        if (providedIds.Any(id => id == Guid.Empty || id == motherPatientId)
            || providedIds.Distinct().Count() != providedIds.Count)
        {
            return false;
        }

        var activePatientCount = await _patientsDbContext.Patients
            .AsNoTracking()
            .CountAsync(
                patient => providedIds.Contains(patient.Id) && patient.IsActive,
                cancellationToken);

        if (activePatientCount != providedIds.Count)
        {
            return false;
        }

        return !await _specialtyCareDbContext.NewbornRecords
            .AsNoTracking()
            .AnyAsync(newborn => providedIds.Contains(newborn.NewbornPatientId), cancellationToken);
    }

    public async Task<bool> IsValidNewbornPatientForDeliveryAsync(
        Guid deliveryId,
        Guid newbornPatientId,
        CancellationToken cancellationToken = default)
    {
        var motherPatientId = await _specialtyCareDbContext.DeliveryRecords
            .AsNoTracking()
            .Where(delivery => delivery.Id == deliveryId)
            .Select(delivery => (Guid?)delivery.MotherPatientId)
            .SingleOrDefaultAsync(cancellationToken);

        return motherPatientId.HasValue
            && await AreValidNewbornPatientsAsync(
                motherPatientId.Value,
                [newbornPatientId],
                cancellationToken);
    }

    private Task<bool> IsOptionalActiveStaffAsync(
        Guid? personId,
        IReadOnlyCollection<ClinicalProfession> allowedProfessions,
        CancellationToken cancellationToken) =>
        !personId.HasValue
            ? Task.FromResult(true)
            : IsActiveStaffAsync(personId.Value, allowedProfessions, cancellationToken);

    private async Task<bool> IsActiveStaffAsync(
        Guid personId,
        IReadOnlyCollection<ClinicalProfession> allowedProfessions,
        CancellationToken cancellationToken)
    {
        if (personId == Guid.Empty)
        {
            return false;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return await (
            from profile in _organizationDbContext.StaffProfiles.AsNoTracking()
            join assignment in _organizationDbContext.StaffDepartmentAssignments.AsNoTracking()
                on profile.Id equals assignment.StaffProfileId
            join department in _organizationDbContext.Departments.AsNoTracking()
                on assignment.DepartmentId equals department.Id
            where profile.PersonId == personId
                && profile.IsActive
                && allowedProfessions.Contains(profile.Profession)
                && department.IsActive
                && assignment.StartsAtUtc <= nowUtc
                && (assignment.EndsAtUtc == null || assignment.EndsAtUtc > nowUtc)
            select assignment.Id)
            .AnyAsync(cancellationToken);
    }

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.Identity?.IsAuthenticated == true
        && actor.HasClaim(HospitalClaimTypes.Permission, permission);

    private static bool TryGetActorPersonId(ClaimsPrincipal actor, out Guid personId)
    {
        var value = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(value, out personId) && personId != Guid.Empty;
    }
}
