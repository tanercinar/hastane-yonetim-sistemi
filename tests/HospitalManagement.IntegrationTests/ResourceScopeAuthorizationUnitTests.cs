using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Host.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.IntegrationTests;

public sealed class ResourceScopeAuthorizationUnitTests
{
    private static readonly Guid PatientAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PatientBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid NurseId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DepartmentId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid OtherDepartmentId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task PatientCanAccessOwnResource()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.Own,
            HospitalPermissions.Patient.ViewOwn);
        var resource = ResourceScopedData.ForPatient(PatientAId);
        var principal = CreatePrincipal(PatientAId, HospitalPermissions.Patient.ViewOwn);
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task PatientCannotAccessAnotherPatientsResourceBecauseHorizontalIdorIsRejected()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.Own,
            HospitalPermissions.Patient.ViewOwn);
        var resource = ResourceScopedData.ForPatient(PatientBId); // Patient B's resource
        var principal = CreatePrincipal(PatientAId, HospitalPermissions.Patient.ViewOwn); // Patient A is logged in
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task ClinicianWithActiveCareRelationshipCanAccessPatientEncounter()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        evaluator.ActiveCareRelationships.Add((DoctorId, PatientAId));

        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.CareTeam,
            HospitalPermissions.ClinicalRecords.EncounterView);
        var resource = ResourceScopedData.ForClinicalEncounter(PatientAId);
        var principal = CreatePrincipal(DoctorId, HospitalPermissions.ClinicalRecords.EncounterView);
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task ClinicianWithoutCareRelationshipCannotAccessPatientEncounter()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        // No relationship established between DoctorId and PatientBId

        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.CareTeam,
            HospitalPermissions.ClinicalRecords.EncounterView);
        var resource = ResourceScopedData.ForClinicalEncounter(PatientBId);
        var principal = CreatePrincipal(DoctorId, HospitalPermissions.ClinicalRecords.EncounterView);
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task ClinicianLackingPermissionClaimIsRejectedEvenWithCareRelationship()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        evaluator.ActiveCareRelationships.Add((DoctorId, PatientAId));

        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.CareTeam,
            HospitalPermissions.ClinicalRecords.EncounterView);
        var resource = ResourceScopedData.ForClinicalEncounter(PatientAId);
        var principal = CreatePrincipal(DoctorId, HospitalPermissions.Identity.ProfileViewOwn); // Missing EncounterView permission
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task StaffAssignedToDepartmentCanAccessDepartmentResource()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        evaluator.DepartmentAssignments.Add((NurseId, DepartmentId));

        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.Department,
            HospitalPermissions.ClinicalRecords.ObservationRecordVital);
        var resource = ResourceScopedData.ForDepartment(DepartmentId);
        var principal = CreatePrincipal(NurseId, HospitalPermissions.ClinicalRecords.ObservationRecordVital);
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G04")]
    public async Task StaffNotAssignedToDepartmentIsRejectedAcrossDepartmentBoundary()
    {
        var evaluator = new FakeCareRelationshipEvaluator();
        evaluator.DepartmentAssignments.Add((NurseId, DepartmentId)); // Assigned to DepartmentId only

        var handler = new ResourceScopeAuthorizationHandler(evaluator);
        var requirement = new ResourceScopeAuthorizationRequirement(
            ResourceScope.Department,
            HospitalPermissions.ClinicalRecords.ObservationRecordVital);
        var resource = ResourceScopedData.ForDepartment(OtherDepartmentId); // Accessing OtherDepartmentId
        var principal = CreatePrincipal(NurseId, HospitalPermissions.ClinicalRecords.ObservationRecordVital);
        var context = new AuthorizationHandlerContext([requirement], principal, resource);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(Guid personId, params string[] permissions)
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(HospitalClaimTypes.PersonId, personId.ToString()));
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(HospitalClaimTypes.Permission, permission));
        }

        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeCareRelationshipEvaluator : ICareRelationshipEvaluator
    {
        public HashSet<(Guid ClinicianId, Guid PatientId)> ActiveCareRelationships { get; } = [];
        public HashSet<(Guid StaffId, Guid DepartmentId)> DepartmentAssignments { get; } = [];
        public HashSet<(Guid StaffId, Guid FacilityId)> FacilityAssignments { get; } = [];

        public Task<bool> HasActiveCareRelationshipAsync(
            Guid clinicianPersonId,
            Guid patientPersonId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveCareRelationships.Contains((clinicianPersonId, patientPersonId)));
        }

        public Task<bool> IsAssignedToDepartmentAsync(
            Guid staffPersonId,
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DepartmentAssignments.Contains((staffPersonId, departmentId)));
        }

        public Task<bool> IsAssignedToFacilityAsync(
            Guid staffPersonId,
            Guid facilityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FacilityAssignments.Contains((staffPersonId, facilityId)));
        }
    }
}

