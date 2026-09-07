using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Authorization;

/// <summary>
/// Applies the phase 8 resource boundary after the endpoint-level permission check.
/// A permission alone never grants access to a clinical record: the caller must also
/// be assigned to the resource, belong to its department/facility, or have an active
/// care relationship with the patient.
/// </summary>
public sealed class Phase8ClinicalAccessControl(
    EmergencyDbContext emergencyDbContext,
    SurgeryDbContext surgeryDbContext,
    OrganizationDbContext organizationDbContext,
    ICareRelationshipEvaluator careRelationshipEvaluator,
    TimeProvider timeProvider)
{
    private static readonly Guid DemoFacilityId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static readonly Guid IntensiveCareDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000008");

    private readonly EmergencyDbContext _emergencyDbContext = emergencyDbContext;
    private readonly SurgeryDbContext _surgeryDbContext = surgeryDbContext;
    private readonly OrganizationDbContext _organizationDbContext = organizationDbContext;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;
    private readonly TimeProvider _timeProvider = timeProvider;

    public Task<bool> CanUseEmergencyWorklistAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default) =>
        CanUseFacilityClinicalWorklistAsync(
            actor,
            HospitalPermissions.Inpatient.EmergencyTriageRecord,
            cancellationToken);

    public async Task<bool> CanAccessEmergencyAdmissionAsync(
        ClaimsPrincipal actor,
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _emergencyDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.Id == admissionId)
            .Select(admission => new
            {
                admission.PatientId,
                admission.AssignedDoctorId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return data is not null
            && await CanAccessEmergencyResourceAsync(
                actor,
                new PatientResource(data.PatientId, data.AssignedDoctorId, null),
                cancellationToken);
    }

    public async Task<bool> CanAccessEmergencyPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var data = await _emergencyDbContext.Admissions
            .AsNoTracking()
            .Where(admission => admission.PatientId == patientId)
            .OrderByDescending(admission => admission.AdmittedAtUtc)
            .Select(admission => new
            {
                admission.PatientId,
                admission.AssignedDoctorId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return data is not null
            && await CanAccessEmergencyResourceAsync(
                actor,
                new PatientResource(data.PatientId, data.AssignedDoctorId, null),
                cancellationToken);
    }

    public async Task<bool> CanAccessEmergencyOrderAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var admissionId = await _emergencyDbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId)
            .Select(order => (Guid?)order.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        return admissionId.HasValue
            && await CanAccessEmergencyAdmissionAsync(actor, admissionId.Value, cancellationToken);
    }

    public async Task<bool> CanAccessEmergencyConsultationAsync(
        ClaimsPrincipal actor,
        Guid consultationId,
        CancellationToken cancellationToken = default)
    {
        var admissionId = await _emergencyDbContext.Consultations
            .AsNoTracking()
            .Where(consultation => consultation.Id == consultationId)
            .Select(consultation => (Guid?)consultation.AdmissionId)
            .SingleOrDefaultAsync(cancellationToken);

        return admissionId.HasValue
            && await CanAccessEmergencyAdmissionAsync(actor, admissionId.Value, cancellationToken);
    }

    public async Task<bool> CanCreateSurgeryBookingAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        Guid departmentId,
        Guid leadSurgeonDoctorId,
        Guid anesthesiologistDoctorId,
        Guid? operatingNurseStaffId,
        CancellationToken cancellationToken = default)
    {
        var resource = new PatientResource(
            patientId,
            leadSurgeonDoctorId,
            departmentId,
            anesthesiologistDoctorId,
            operatingNurseStaffId);

        if (!await CanAccessSurgeryResourceAsync(actor, resource, cancellationToken))
        {
            return false;
        }

        if (!await IsActiveStaffAssignedToDepartmentAsync(
                leadSurgeonDoctorId,
                departmentId,
                ClinicalProfession.Physician,
                cancellationToken)
            || !await IsActiveStaffAssignedToDepartmentAsync(
                anesthesiologistDoctorId,
                departmentId,
                ClinicalProfession.Physician,
                cancellationToken))
        {
            return false;
        }

        return !operatingNurseStaffId.HasValue
            || await IsActiveStaffAssignedToDepartmentAsync(
                operatingNurseStaffId.Value,
                departmentId,
                ClinicalProfession.Nurse,
                cancellationToken);
    }

    public Task<bool> CanUseSurgeryWorklistAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default) =>
        CanUseFacilityClinicalWorklistAsync(
            actor,
            HospitalPermissions.Inpatient.SurgerySchedule,
            cancellationToken);

    public async Task<bool> CanAccessSurgeryBookingAsync(
        ClaimsPrincipal actor,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var data = await _surgeryDbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.Id == bookingId)
            .Select(booking => new
            {
                booking.PatientId,
                booking.LeadSurgeonDoctorId,
                booking.DepartmentId,
                booking.AnesthesiologistDoctorId,
                booking.OperatingNurseStaffId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return data is not null
            && await CanAccessSurgeryResourceAsync(
                actor,
                new PatientResource(
                    data.PatientId,
                    data.LeadSurgeonDoctorId,
                    data.DepartmentId,
                    data.AnesthesiologistDoctorId,
                    data.OperatingNurseStaffId),
                cancellationToken);
    }

    public Task<bool> CanAccessSurgeryBookingAsync(
        ClaimsPrincipal actor,
        SurgeryBookingDto booking,
        CancellationToken cancellationToken = default) =>
        CanAccessSurgeryResourceAsync(
            actor,
            new PatientResource(
                booking.PatientId,
                booking.LeadSurgeonDoctorId,
                booking.DepartmentId,
                booking.AnesthesiologistDoctorId,
                booking.OperatingNurseStaffId),
            cancellationToken);

    public async Task<bool> CanAccessPerioperativeRecordAsync(
        ClaimsPrincipal actor,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        var bookingId = await _surgeryDbContext.PerioperativeRecords
            .AsNoTracking()
            .Where(record => record.Id == recordId)
            .Select(record => (Guid?)record.SurgeryBookingId)
            .SingleOrDefaultAsync(cancellationToken);

        return bookingId.HasValue
            && await CanAccessSurgeryBookingAsync(actor, bookingId.Value, cancellationToken);
    }

    public async Task<bool> CanUseIcuWorklistAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, HospitalPermissions.Inpatient.CriticalCareRecord))
        {
            return false;
        }

        return await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
            actorPersonId,
            IntensiveCareDepartmentId,
            cancellationToken);
    }

    public Task<bool> CanCreateIcuAdmissionAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        Guid attendingDoctorId,
        Guid? primaryNurseId,
        CancellationToken cancellationToken = default) =>
        CanAccessIcuResourceAsync(
            actor,
            new PatientResource(patientId, attendingDoctorId, IntensiveCareDepartmentId, primaryNurseId),
            cancellationToken);

    public async Task<bool> CanAccessIcuAdmissionAsync(
        ClaimsPrincipal actor,
        Guid admissionId,
        CancellationToken cancellationToken = default)
    {
        var data = await _surgeryDbContext.IcuAdmissions
            .AsNoTracking()
            .Where(admission => admission.Id == admissionId)
            .Select(admission => new
            {
                admission.PatientId,
                admission.AttendingDoctorId,
                admission.PrimaryNurseId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return data is not null
            && await CanAccessIcuResourceAsync(
                actor,
                new PatientResource(
                    data.PatientId,
                    data.AttendingDoctorId,
                    IntensiveCareDepartmentId,
                    data.PrimaryNurseId),
                cancellationToken);
    }

    public Task<bool> CanAccessIcuAdmissionAsync(
        ClaimsPrincipal actor,
        IcuAdmissionDto admission,
        CancellationToken cancellationToken = default) =>
        CanAccessIcuResourceAsync(
            actor,
            new PatientResource(
                admission.PatientId,
                admission.AttendingDoctorId,
                IntensiveCareDepartmentId,
                admission.PrimaryNurseId),
            cancellationToken);

    public Task<bool> CanInitiateClinicalHandoffAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        CanAccessHandoffResourceAsync(
            actor,
            new PatientResource(patientId, null, null),
            cancellationToken);

    public async Task<bool> CanAccessClinicalHandoffAsync(
        ClaimsPrincipal actor,
        Guid handoffId,
        CancellationToken cancellationToken = default)
    {
        var data = await _surgeryDbContext.ClinicalHandoffs
            .AsNoTracking()
            .Where(handoff => handoff.Id == handoffId)
            .Select(handoff => new
            {
                handoff.PatientId,
                handoff.HandingOverStaffId,
                handoff.ReceivingStaffId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return data is not null
            && await CanAccessHandoffResourceAsync(
                actor,
                new PatientResource(
                    data.PatientId,
                    data.HandingOverStaffId,
                    null,
                    data.ReceivingStaffId),
                cancellationToken);
    }

    public Task<bool> CanAccessClinicalHandoffAsync(
        ClaimsPrincipal actor,
        ClinicalHandoffDto handoff,
        CancellationToken cancellationToken = default) =>
        CanAccessHandoffResourceAsync(
            actor,
            new PatientResource(
                handoff.PatientId,
                handoff.HandingOverStaffId,
                null,
                handoff.ReceivingStaffId),
            cancellationToken);

    private async Task<bool> CanAccessEmergencyResourceAsync(
        ClaimsPrincipal actor,
        PatientResource resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, HospitalPermissions.Inpatient.EmergencyTriageRecord))
        {
            return false;
        }

        return resource.AssignedStaffIds.Contains(actorPersonId)
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                resource.PatientId,
                cancellationToken)
            || await IsActiveStaffAtDemoFacilityAsync(actorPersonId, cancellationToken);
    }

    private async Task<bool> CanAccessSurgeryResourceAsync(
        ClaimsPrincipal actor,
        PatientResource resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, HospitalPermissions.Inpatient.SurgerySchedule))
        {
            return false;
        }

        return resource.AssignedStaffIds.Contains(actorPersonId)
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                resource.PatientId,
                cancellationToken)
            || (resource.DepartmentId.HasValue
                && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                    actorPersonId,
                    resource.DepartmentId.Value,
                    cancellationToken));
    }

    private async Task<bool> CanAccessIcuResourceAsync(
        ClaimsPrincipal actor,
        PatientResource resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, HospitalPermissions.Inpatient.CriticalCareRecord))
        {
            return false;
        }

        return resource.AssignedStaffIds.Contains(actorPersonId)
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                resource.PatientId,
                cancellationToken)
            || await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                IntensiveCareDepartmentId,
                cancellationToken);
    }

    private async Task<bool> CanAccessHandoffResourceAsync(
        ClaimsPrincipal actor,
        PatientResource resource,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorPersonId(actor, out var actorPersonId)
            || !HasPermission(actor, HospitalPermissions.Inpatient.CriticalCareRecord))
        {
            return false;
        }

        return resource.AssignedStaffIds.Contains(actorPersonId)
            || await _careRelationshipEvaluator.HasActiveCareRelationshipAsync(
                actorPersonId,
                resource.PatientId,
                cancellationToken)
            || await IsActiveStaffAtDemoFacilityAsync(actorPersonId, cancellationToken);
    }

    private async Task<bool> CanUseFacilityClinicalWorklistAsync(
        ClaimsPrincipal actor,
        string permission,
        CancellationToken cancellationToken)
    {
        return HasPermission(actor, permission)
            && TryGetActorPersonId(actor, out var actorPersonId)
            && await IsActiveStaffAtDemoFacilityAsync(actorPersonId, cancellationToken);
    }

    private async Task<bool> IsActiveStaffAtDemoFacilityAsync(
        Guid actorPersonId,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return await (
            from profile in _organizationDbContext.StaffProfiles.AsNoTracking()
            join assignment in _organizationDbContext.StaffDepartmentAssignments.AsNoTracking()
                on profile.Id equals assignment.StaffProfileId
            join department in _organizationDbContext.Departments.AsNoTracking()
                on assignment.DepartmentId equals department.Id
            where profile.PersonId == actorPersonId
                && profile.IsActive
                && department.IsActive
                && department.FacilityId == DemoFacilityId
                && assignment.StartsAtUtc <= nowUtc
                && (assignment.EndsAtUtc == null || assignment.EndsAtUtc > nowUtc)
            select assignment.Id)
            .AnyAsync(cancellationToken);
    }

    private async Task<bool> IsActiveStaffAssignedToDepartmentAsync(
        Guid personId,
        Guid departmentId,
        ClinicalProfession profession,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return await (
            from profile in _organizationDbContext.StaffProfiles.AsNoTracking()
            join assignment in _organizationDbContext.StaffDepartmentAssignments.AsNoTracking()
                on profile.Id equals assignment.StaffProfileId
            join department in _organizationDbContext.Departments.AsNoTracking()
                on assignment.DepartmentId equals department.Id
            where profile.PersonId == personId
                && profile.Profession == profession
                && profile.IsActive
                && department.Id == departmentId
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

    private sealed record PatientResource(
        Guid PatientId,
        Guid? AssignedStaffId,
        Guid? DepartmentId,
        params Guid?[] AdditionalAssignedStaffIds)
    {
        public HashSet<Guid> AssignedStaffIds
        {
            get;
        } = new[]
        {
            AssignedStaffId,
        }
            .Concat(AdditionalAssignedStaffIds)
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .ToHashSet();
    }
}
