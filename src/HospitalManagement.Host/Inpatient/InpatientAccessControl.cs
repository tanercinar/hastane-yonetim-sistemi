using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Inpatient;

/// <summary>
/// Enforces inpatient resource scope at the API boundary. Holding a role or permission is not
/// sufficient: the caller must also own the patient record, be part of the care relationship,
/// be the attending practitioner, or have an active assignment in the resource department.
/// </summary>
public sealed class InpatientAccessControl(
    InpatientDbContext inpatientDbContext,
    ICareRelationshipEvaluator careRelationshipEvaluator)
{
    private readonly InpatientDbContext _inpatientDbContext = inpatientDbContext;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;

    public async Task<bool> CanRequestAdmissionAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        Guid departmentId,
        Guid attendingDoctorId,
        CancellationToken cancellationToken)
    {
        if (!HasAnyPermission(actor, HospitalPermissions.Inpatient.AdmissionRequest)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        var actorMatchesRequest = actorPersonId == attendingDoctorId
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                patientId,
                cancellationToken);

        return actorMatchesRequest
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                departmentId,
                cancellationToken)
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                attendingDoctorId,
                departmentId,
                cancellationToken);
    }

    public async Task<bool> CanAccessWardAsync(
        ClaimsPrincipal actor,
        Guid wardId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var departmentId = await _inpatientDbContext.Wards
            .AsNoTracking()
            .Where(ward => ward.Id == wardId)
            .Select(ward => (Guid?)ward.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

        return departmentId.HasValue
            && await CanAccessDepartmentAsync(actor, departmentId.Value, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessBedAsync(
        ClaimsPrincipal actor,
        Guid bedId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var wardId = await _inpatientDbContext.Beds
            .AsNoTracking()
            .Where(bed => bed.Id == bedId)
            .Select(bed => (Guid?)bed.WardId)
            .SingleOrDefaultAsync(cancellationToken);

        return wardId.HasValue
            && await CanAccessWardAsync(actor, wardId.Value, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessAdmissionAsync(
        ClaimsPrincipal actor,
        Guid admissionId,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var resource = await _inpatientDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.Id == admissionId)
            .Select(admission => new InpatientResource(
                admission.PatientId,
                admission.DepartmentId,
                admission.AttendingDoctorId))
            .SingleOrDefaultAsync(cancellationToken);

        return resource is not null
            && await CanAccessResourceAsync(
                actor,
                resource,
                allowPatientOwnRecord,
                cancellationToken,
                permissions);
    }

    public async Task<bool> CanAccessPatientAdmissionAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        if (allowPatientOwnRecord && ClinicalRecordAccessControl.IsPatientOwnRecord(actor, patientId))
        {
            return true;
        }

        var resources = await _inpatientDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.PatientId == patientId
                && admission.Status != AdmissionStatus.Cancelled)
            .Select(admission => new InpatientResource(
                admission.PatientId,
                admission.DepartmentId,
                admission.AttendingDoctorId))
            .ToListAsync(cancellationToken);

        foreach (var resource in resources)
        {
            if (await CanAccessResourceAsync(actor, resource, false, cancellationToken, permissions))
            {
                return true;
            }
        }

        return false;
    }

    public Task<Guid?> FindAdmissionPatientIdAsync(
        Guid admissionId,
        CancellationToken cancellationToken = default) =>
        _inpatientDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.Id == admissionId)
            .Select(admission => (Guid?)admission.PatientId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> IsPersonAssignedToAdmissionDepartmentAsync(
        Guid personId,
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var departmentId = await _inpatientDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.Id == admissionId)
            .Select(admission => (Guid?)admission.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

        return departmentId.HasValue
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                personId,
                departmentId.Value,
                cancellationToken);
    }

    public async Task<bool> CanAccessTransferAsync(
        ClaimsPrincipal actor,
        Guid transferId,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var admissionId = await _inpatientDbContext.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.Id == transferId)
            .Select(transfer => (Guid?)transfer.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        if (admissionId.HasValue
            && await CanAccessAdmissionAsync(
                actor,
                admissionId.Value,
                allowPatientOwnRecord,
                cancellationToken,
                permissions))
        {
            return true;
        }

        return await CanAccessTransferTargetWardAsync(
            actor,
            transferId,
            cancellationToken,
            permissions);
    }

    public async Task<bool> CanAccessTransferTargetWardAsync(
        ClaimsPrincipal actor,
        Guid transferId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var targetWardId = await _inpatientDbContext.Transfers
            .AsNoTracking()
            .Where(transfer => transfer.Id == transferId)
            .Select(transfer => (Guid?)transfer.TargetWardId)
            .SingleOrDefaultAsync(cancellationToken);

        return targetWardId.HasValue
            && await CanAccessWardAsync(actor, targetWardId.Value, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessObservationAsync(
        ClaimsPrincipal actor,
        Guid observationId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var admissionId = await _inpatientDbContext.NursingObservations
            .AsNoTracking()
            .Where(observation => observation.Id == observationId)
            .Select(observation => (Guid?)observation.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        return admissionId.HasValue
            && await CanAccessAdmissionAsync(actor, admissionId.Value, false, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessCarePlanAsync(
        ClaimsPrincipal actor,
        Guid carePlanId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var admissionId = await _inpatientDbContext.NursingCarePlans
            .AsNoTracking()
            .Where(plan => plan.Id == carePlanId)
            .Select(plan => (Guid?)plan.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        return admissionId.HasValue
            && await CanAccessAdmissionAsync(actor, admissionId.Value, false, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessCareTaskAsync(
        ClaimsPrincipal actor,
        Guid taskId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var carePlanId = await _inpatientDbContext.NursingCareTasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.CarePlanId)
            .SingleOrDefaultAsync(cancellationToken);

        return carePlanId.HasValue
            && await CanAccessCarePlanAsync(actor, carePlanId.Value, cancellationToken, permissions);
    }

    public async Task<bool> CanAccessMedicationAsync(
        ClaimsPrincipal actor,
        Guid medicationAdministrationId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var admissionId = await _inpatientDbContext.MedicationAdministrations
            .AsNoTracking()
            .Where(administration => administration.Id == medicationAdministrationId)
            .Select(administration => (Guid?)administration.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        return admissionId.HasValue
            && await CanAccessAdmissionAsync(actor, admissionId.Value, false, cancellationToken, permissions);
    }

    public async Task<IReadOnlySet<Guid>> GetAccessibleDepartmentIdsAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        if (!HasAnyPermission(actor, permissions)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return new HashSet<Guid>();
        }

        var departmentIds = await _inpatientDbContext.Wards
            .AsNoTracking()
            .Select(ward => ward.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var accessible = new HashSet<Guid>();
        foreach (var departmentId in departmentIds)
        {
            if (await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                    actorPersonId,
                    departmentId,
                    cancellationToken))
            {
                accessible.Add(departmentId);
            }
        }

        return accessible;
    }

    public async Task<IReadOnlySet<Guid>> GetAccessibleWardIdsAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        var departmentIds = await GetAccessibleDepartmentIdsAsync(actor, cancellationToken, permissions);
        if (departmentIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var wardIds = await _inpatientDbContext.Wards
            .AsNoTracking()
            .Where(ward => departmentIds.Contains(ward.DepartmentId))
            .Select(ward => ward.Id)
            .ToListAsync(cancellationToken);

        return wardIds.ToHashSet();
    }

    private async Task<bool> CanAccessDepartmentAsync(
        ClaimsPrincipal actor,
        Guid departmentId,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        return HasAnyPermission(actor, permissions)
            && ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId)
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                departmentId,
                cancellationToken);
    }

    private async Task<bool> CanAccessResourceAsync(
        ClaimsPrincipal actor,
        InpatientResource resource,
        bool allowPatientOwnRecord,
        CancellationToken cancellationToken,
        params string[] permissions)
    {
        if (allowPatientOwnRecord
            && ClinicalRecordAccessControl.IsPatientOwnRecord(actor, resource.PatientId))
        {
            return true;
        }

        if (!HasAnyPermission(actor, permissions)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        return actorPersonId == resource.AttendingDoctorId
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                resource.PatientId,
                cancellationToken)
            || await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                resource.DepartmentId,
                cancellationToken);
    }

    private static bool HasAnyPermission(ClaimsPrincipal actor, params string[] permissions) =>
        actor.Identity?.IsAuthenticated == true
        && permissions.Any(permission => actor.HasClaim(HospitalClaimTypes.Permission, permission));

    private sealed record InpatientResource(
        Guid PatientId,
        Guid DepartmentId,
        Guid AttendingDoctorId);
}
