using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Host.Diagnostics;

public sealed class DiagnosticsAccessContext(
    ClinicalRecordAccessControl clinicalAccessControl,
    ICareRelationshipEvaluator careRelationshipEvaluator,
    OrganizationDbContext organizationDbContext) : IDiagnosticsAccessContext
{
    private const string LaboratoryDepartmentCode = "DEMO-LABORATORY";
    private const string RadiologyDepartmentCode = "DEMO-RADIOLOGY";

    private readonly ClinicalRecordAccessControl _clinicalAccessControl = clinicalAccessControl;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator = careRelationshipEvaluator;
    private readonly OrganizationDbContext _organizationDbContext = organizationDbContext;

    public async Task<DiagnosticEncounterContext?> FindEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        var encounter = await _clinicalAccessControl.FindEncounterAsync(encounterId, cancellationToken);
        return encounter is null
            ? null
            : new DiagnosticEncounterContext(
                encounter.Id,
                encounter.PatientId,
                encounter.DepartmentId,
                encounter.PrimaryPractitionerId,
                ClinicalRecordAccessControl.IsEncounterOpenForClinicalEntry(encounter));
    }

    public async Task<bool> CanAccessEncounterAsync(
        ClaimsPrincipal actor,
        DiagnosticEncounterContext encounter,
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

    public async Task<bool> CanAccessResourceAsync(
        ClaimsPrincipal actor,
        DiagnosticResourceContext resource,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken = default)
    {
        if (!ClinicalRecordAccessControl.HasPermission(actor, permission))
        {
            return false;
        }

        if (allowPatientOwnFinalResult && ClinicalRecordAccessControl.IsPatientOwnRecord(actor, resource.PatientId))
        {
            return true;
        }

        var encounter = await FindEncounterAsync(resource.EncounterId, cancellationToken);
        if (encounter is not null
            && await CanAccessEncounterAsync(actor, encounter, permission, false, cancellationToken))
        {
            return true;
        }

        return await CanAccessDiagnosticAreaAsync(actor, resource.OrderType, permission, cancellationToken);
    }

    public async Task<bool> CanAccessDiagnosticAreaAsync(
        ClaimsPrincipal actor,
        DiagnosticOrderType orderType,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (!ClinicalRecordAccessControl.HasPermission(actor, permission)
            || !ClinicalRecordAccessControl.TryGetActorPersonId(actor, out var actorPersonId))
        {
            return false;
        }

        var targetDepartmentCode = orderType == DiagnosticOrderType.Radiology
            ? RadiologyDepartmentCode
            : orderType is DiagnosticOrderType.Laboratory
                or DiagnosticOrderType.Pathology
                or DiagnosticOrderType.BloodBank
                    ? LaboratoryDepartmentCode
                    : null;

        if (targetDepartmentCode is null)
        {
            return false;
        }

        var targetDepartmentId = await _organizationDbContext.Departments
            .AsNoTracking()
            .Where(department => department.Code == targetDepartmentCode && department.IsActive)
            .Select(department => (Guid?)department.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return targetDepartmentId.HasValue
            && await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                targetDepartmentId.Value,
                cancellationToken);
    }
}
