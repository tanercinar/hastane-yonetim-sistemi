using HospitalManagement.BuildingBlocks.Authorization;

namespace HospitalManagement.UnitTests;

public sealed class HospitalPermissionCatalogTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void CatalogContainsAllCanonicalPermissionsWithValidMetadata()
    {
        Assert.Equal(71, HospitalPermissionCatalog.All.Count);

        foreach (var permission in HospitalPermissionCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(permission.Name));
            Assert.False(string.IsNullOrWhiteSpace(permission.Category));
            Assert.False(string.IsNullOrWhiteSpace(permission.Description));
            Assert.True(HospitalPermissionCatalog.IsKnown(permission.Name));
            Assert.All(permission.DefaultRoles, role => Assert.True(HospitalRoles.IsKnown(role)));
        }

        Assert.False(HospitalPermissionCatalog.IsKnown("nonexistent.permission"));
        Assert.False(HospitalPermissionCatalog.IsKnown(string.Empty));
        Assert.False(HospitalPermissionCatalog.IsKnown("   "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void AllTwelveRolesAreDefinedAndRecognized()
    {
        Assert.Equal(12, HospitalRoles.All.Count);

        string[] expectedRoles =
        [
            HospitalRoles.Patient,
            HospitalRoles.Doctor,
            HospitalRoles.Nurse,
            HospitalRoles.ChiefMedicalOfficer,
            HospitalRoles.RegistrationStaff,
            HospitalRoles.LaboratoryStaff,
            HospitalRoles.RadiologyStaff,
            HospitalRoles.Pharmacist,
            HospitalRoles.SystemAdministrator,
            HospitalRoles.HospitalManager,
            HospitalRoles.BillingStaff,
            HospitalRoles.HumanResourcesStaff,
        ];

        foreach (var role in expectedRoles)
        {
            Assert.True(HospitalRoles.IsKnown(role));
        }

        Assert.False(HospitalRoles.IsKnown("SUPERUSER"));
        Assert.False(HospitalRoles.IsKnown(string.Empty));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void FutureRolesHaveNoDefaultPermissions()
    {
        var finPermissions = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.BillingStaff);
        var hrPermissions = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.HumanResourcesStaff);

        Assert.Empty(finPermissions);
        Assert.Empty(hrPermissions);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void SystemAdministratorHasNoClinicalPermissions()
    {
        var admPermissions = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.SystemAdministrator);

        Assert.DoesNotContain(HospitalPermissions.ClinicalRecords.ClinicalNoteSign, admPermissions);
        Assert.DoesNotContain(HospitalPermissions.ClinicalRecords.EncounterStart, admPermissions);
        Assert.DoesNotContain(HospitalPermissions.Pharmacy.PrescriptionCreate, admPermissions);
        Assert.DoesNotContain(HospitalPermissions.Diagnostics.DiagnosticOrderCreate, admPermissions);
        Assert.DoesNotContain(HospitalPermissions.Inpatient.AdmissionRequest, admPermissions);

        Assert.Contains(HospitalPermissions.Identity.RoleAssign, admPermissions);
        Assert.Contains(HospitalPermissions.Identity.PermissionAssign, admPermissions);
        Assert.Contains(HospitalPermissions.Identity.UserInviteStaff, admPermissions);
        Assert.Contains(HospitalPermissions.Organization.Manage, admPermissions);
        Assert.Contains(HospitalPermissions.ReportingAndAudit.AuditTechnicalView, admPermissions);
        Assert.Contains(HospitalPermissions.Interoperability.MockManage, admPermissions);
        Assert.DoesNotContain(HospitalPermissions.Interoperability.FhirExport, admPermissions);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void RegistrationStaffHasNoClinicalNotesOrPharmacyOrDiagnosticPermissions()
    {
        var regPermissions = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.RegistrationStaff);

        Assert.DoesNotContain(HospitalPermissions.ClinicalRecords.ClinicalNoteSign, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.ClinicalRecords.EncounterStart, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.Pharmacy.PrescriptionCreate, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.Pharmacy.PrescriptionDispense, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.Diagnostics.LaboratoryResultFinalize, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.Diagnostics.RadiologyReportFinalize, regPermissions);
        Assert.DoesNotContain(HospitalPermissions.Diagnostics.BloodTransfusionRecord, regPermissions);

        Assert.Contains(HospitalPermissions.Patient.DemographicsCreate, regPermissions);
        Assert.Contains(HospitalPermissions.Appointment.CheckIn, regPermissions);
        Assert.Contains(HospitalPermissions.Appointment.Manage, regPermissions);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void PatientHasOnlyOwnPortalPermissions()
    {
        var patientPermissions = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.Patient);

        Assert.Equal(10, patientPermissions.Count);
        Assert.Contains(HospitalPermissions.Identity.ProfileViewOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Identity.ProfileEditOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Patient.ViewOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Appointment.ViewOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Appointment.BookOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Appointment.ManageOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.Pharmacy.PrescriptionView, patientPermissions);
        Assert.Contains(HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.SpecialtyCare.SpecialtyCareViewOwn, patientPermissions);
        Assert.Contains(HospitalPermissions.ReportingAndAudit.AuditOwnAccessView, patientPermissions);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public void MultipleRolesComputeUnionOfPermissionsCorrectly()
    {
        var combined = RolePermissionDefaults.GetPermissionsForRoles([HospitalRoles.Doctor, HospitalRoles.ChiefMedicalOfficer]);
        var docOnly = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.Doctor);
        var chmOnly = RolePermissionDefaults.GetPermissionsForRole(HospitalRoles.ChiefMedicalOfficer);

        Assert.True(combined.IsSupersetOf(docOnly));
        Assert.True(combined.IsSupersetOf(chmOnly));
        Assert.Contains(HospitalPermissions.ClinicalRecords.ClinicalNoteReopen, combined);
        Assert.Contains(HospitalPermissions.ReportingAndAudit.AuditClinicalAccessView, combined);
    }
}
