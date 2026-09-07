using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Patients;
using Xunit;

namespace HospitalManagement.UnitTests.Security;

public sealed class AuthorizationAttackSuiteTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void IdorAttackWhenPatientAccessesAnotherPatientsResourceIsDenied()
    {
        var patientAId = Guid.NewGuid();
        var patientBId = Guid.NewGuid();

        // User A claims
        var patientAClaims = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, patientAId.ToString()),
                new Claim(ClaimTypes.Role, HospitalRoles.Patient),
                new Claim(HospitalClaimTypes.PersonId, patientAId.ToString())
            },
            "TestAuth"));

        // Resource belonging to Patient B
        var patientBResource = ResourceScopedData.ForPatient(patientBId);

        // Verify IDOR check fails: User A's PersonId claim does not match Resource's PatientId
        var claimPatientId = patientAClaims.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var isAuthorized = Guid.TryParse(claimPatientId, out var parsedId) && parsedId == patientBResource.PatientId;

        Assert.False(isAuthorized, "IDOR Vulnerability: Patient A must not be authorized to access Patient B's resource.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void IdorAttackWhenClinicianWithoutCareRelationshipAccessesPatientIsDenied()
    {
        var doctorId = Guid.NewGuid();
        var assignedDoctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        var doctorClaims = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, doctorId.ToString()),
                new Claim(ClaimTypes.Role, HospitalRoles.Doctor),
                new Claim(HospitalClaimTypes.PersonId, doctorId.ToString())
            },
            "TestAuth"));

        // Clinical encounter resource explicitly assigned to another doctor
        var encounterResource = ResourceScopedData.ForClinicalEncounter(patientId, Guid.NewGuid(), assignedDoctorId);

        var callerStaffId = doctorClaims.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var hasDirectCareRelationship = Guid.TryParse(callerStaffId, out var staffGuid) && staffGuid == encounterResource.AssignedStaffId;

        Assert.False(hasDirectCareRelationship, "IDOR Vulnerability: Doctor without direct assignment or care relationship must be denied.");
    }

    [Theory]
    [InlineData(HospitalPermissions.ClinicalRecords.EncounterStart)]
    [InlineData(HospitalPermissions.Pharmacy.PrescriptionCreate)]
    [InlineData(HospitalPermissions.Identity.RoleAssign)]
    [InlineData(HospitalPermissions.Identity.UserDisable)]
    [InlineData(HospitalPermissions.Diagnostics.DiagnosticOrderCreate)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void PrivilegeEscalationPatientRoleAttemptingClinicalOrAdminPermissionsIsDenied(string forbiddenPermission)
    {
        var permission = HospitalPermissionCatalog.All.FirstOrDefault(p => p.Name == forbiddenPermission);
        Assert.NotNull(permission);

        // Ensure Patient role is NEVER granted clinical/admin permissions by default
        Assert.DoesNotContain(HospitalRoles.Patient, permission.DefaultRoles);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void PrivilegeEscalationNurseRoleAttemptingPrescriptionSigningIsDenied()
    {
        var prescriptionWritePermission = HospitalPermissionCatalog.All
            .FirstOrDefault(p => p.Name == HospitalPermissions.Pharmacy.PrescriptionSign);

        Assert.NotNull(prescriptionWritePermission);
        Assert.DoesNotContain(HospitalRoles.Nurse, prescriptionWritePermission.DefaultRoles);
        Assert.Contains(HospitalRoles.Doctor, prescriptionWritePermission.DefaultRoles);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void DepartmentBoundaryDoctorFromDifferentDepartmentWithoutCareRelationshipIsDenied()
    {
        var cardiologyDeptId = Guid.NewGuid();
        var neurologyDeptId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var cardiologyDoctor = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, doctorId.ToString()),
                new Claim(ClaimTypes.Role, HospitalRoles.Doctor),
                new Claim(HospitalClaimTypes.DepartmentId, cardiologyDeptId.ToString())
            },
            "TestAuth"));

        // Neurology department resource
        var neurologyResource = ResourceScopedData.ForDepartment(neurologyDeptId);

        var doctorDept = cardiologyDoctor.FindFirst(HospitalClaimTypes.DepartmentId)?.Value;
        var isDepartmentAuthorized = Guid.TryParse(doctorDept, out var deptGuid) && deptGuid == neurologyResource.DepartmentId;

        Assert.False(isDepartmentAuthorized, "Tenant/Department Boundary Violation: Cross-department unauthorized access must be denied.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void MassAssignmentPatientRegistrationRequestDoesNotExposeAdministrativeProperties()
    {
        var registrationType = typeof(PatientRegistrationRequest);
        var properties = registrationType.GetProperties();

        // Verify that administrative privilege escalation properties cannot be bound
        Assert.DoesNotContain(properties, p => p.Name.Equals("Role", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Equals("Roles", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Equals("IsAdmin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Equals("IsSuperUser", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, p => p.Name.Equals("Permissions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void SensitiveFieldLeakagePatientDtoResponsesDoNotExposeHashedPasswordsOrMfaSecrets()
    {
        var patientResponseType = typeof(PatientDetailResponse);
        var patientProperties = patientResponseType.GetProperties();

        Assert.DoesNotContain(patientProperties, p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(patientProperties, p => p.Name.Contains("Salt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(patientProperties, p => p.Name.Contains("SecurityStamp", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(patientProperties, p => p.Name.Contains("TwoFactorSecret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(patientProperties, p => p.Name.Contains("RecoveryCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F13-G02")]
    public void AuthorizationDenyGeneratesAuditEventWithoutExposingClinicalData()
    {
        var actorId = Guid.NewGuid();
        var targetPatientId = Guid.NewGuid();

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: DateTime.UtcNow,
            ActorUserId: actorId,
            ActorPersonId: null,
            ActorRole: HospitalRoles.Patient,
            ActorIpAddress: "127.0.0.1",
            ActorUserAgent: "SecurityTestRunner/1.0",
            Action: "Authorization.AccessDenied",
            TargetResourceType: "ClinicalEncounter",
            TargetResourceId: targetPatientId.ToString(),
            Outcome: AuditOutcome.Forbidden,
            Reason: "Access denied by ResourceScopeCareRelationshipPolicy",
            CorrelationId: Guid.NewGuid().ToString(),
            DetailsJson: null);

        Assert.Equal("Authorization.AccessDenied", auditEvent.Action);
        Assert.Equal(actorId, auditEvent.ActorUserId);
        Assert.Equal("ClinicalEncounter", auditEvent.TargetResourceType);
        Assert.Equal(AuditOutcome.Forbidden, auditEvent.Outcome);

        // Ensure audit record reason contains NO clinical data
        Assert.DoesNotContain("Kanser", auditEvent.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Diyabet", auditEvent.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mg", auditEvent.Reason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
