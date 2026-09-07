using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class ClinicalRecordAccessControlTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-KAPI")]
    public async Task PermissionAloneDoesNotGrantUnrelatedClinicalRecordAccess()
    {
        await using var dbContext = CreateDbContext();
        var encounter = CreateEncounter();
        dbContext.Encounters.Add(encounter);
        await dbContext.SaveChangesAsync();

        var evaluator = new FakeCareRelationshipEvaluator();
        var accessControl = new ClinicalRecordAccessControl(dbContext, evaluator);
        var unrelatedAdministrator = CreateActor(
            Guid.NewGuid(),
            HospitalRoles.SystemAdministrator,
            HospitalPermissions.ClinicalRecords.EncounterView);

        var allowed = await accessControl.CanAccessEncounterAsync(
            unrelatedAdministrator,
            encounter,
            HospitalPermissions.ClinicalRecords.EncounterView,
            allowPatientOwnRecord: false,
            CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-KAPI")]
    public async Task PermissionAndEncounterParticipationGrantClinicalRecordAccess()
    {
        await using var dbContext = CreateDbContext();
        var encounter = CreateEncounter();
        dbContext.Encounters.Add(encounter);
        await dbContext.SaveChangesAsync();

        var accessControl = new ClinicalRecordAccessControl(
            dbContext,
            new FakeCareRelationshipEvaluator());
        var primaryDoctor = CreateActor(
            encounter.PrimaryPractitionerId,
            HospitalRoles.Doctor,
            HospitalPermissions.ClinicalRecords.EncounterView);

        var allowed = await accessControl.CanAccessEncounterAsync(
            primaryDoctor,
            encounter,
            HospitalPermissions.ClinicalRecords.EncounterView,
            allowPatientOwnRecord: false,
            CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-KAPI")]
    public async Task PatientCanOnlyUseOwnRecordExceptionForMatchingPatient()
    {
        await using var dbContext = CreateDbContext();
        var encounter = CreateEncounter();
        var accessControl = new ClinicalRecordAccessControl(
            dbContext,
            new FakeCareRelationshipEvaluator());
        var patient = CreateActor(
            encounter.PatientId,
            HospitalRoles.Patient,
            HospitalPermissions.Patient.ViewOwn);

        var ownAllowed = await accessControl.CanAccessEncounterAsync(
            patient,
            encounter,
            HospitalPermissions.ClinicalRecords.EncounterView,
            allowPatientOwnRecord: true,
            CancellationToken.None);

        var otherEncounter = CreateEncounter(patientId: Guid.NewGuid());
        var otherAllowed = await accessControl.CanAccessEncounterAsync(
            patient,
            otherEncounter,
            HospitalPermissions.ClinicalRecords.EncounterView,
            allowPatientOwnRecord: true,
            CancellationToken.None);

        Assert.True(ownAllowed);
        Assert.False(otherAllowed);
    }

    private static ClinicalRecordsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicalRecordsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ClinicalRecordsDbContext(options);
    }

    private static Encounter CreateEncounter(Guid? patientId = null) =>
        Encounter.Create(
            Guid.NewGuid(),
            appointmentId: null,
            patientId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            EncounterType.Outpatient,
            plannedStartTimeUtc: DateTime.UtcNow,
            chiefComplaint: "DEMO test",
            DateTime.UtcNow,
            startImmediately: true);

    private static ClaimsPrincipal CreateActor(
        Guid personId,
        string role,
        string permission)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(HospitalClaimTypes.PersonId, personId.ToString("D")),
                new Claim(ClaimTypes.Role, role),
                new Claim(HospitalClaimTypes.Permission, permission),
            ],
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeCareRelationshipEvaluator : ICareRelationshipEvaluator
    {
        public Task<bool> HasActiveCareRelationshipAsync(
            Guid clinicianPersonId,
            Guid patientPersonId,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> IsAssignedToDepartmentAsync(
            Guid staffPersonId,
            Guid departmentId,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> IsAssignedToFacilityAsync(
            Guid staffPersonId,
            Guid facilityId,
            CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
