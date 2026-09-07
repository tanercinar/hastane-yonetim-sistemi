using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests.Infrastructure;

internal static class ClinicalTestData
{
    internal static readonly Guid DemoDoctorPersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000102");

    internal static readonly Guid DemoNursePersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000103");

    internal static readonly Guid DemoChiefPersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000108");

    internal static readonly Guid DemoPatientPersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000109");

    internal static readonly Guid DemoCardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    internal static async Task<Guid> SeedStartedEncounterAsync(
        ApiWebApplicationFactory application,
        Guid? patientId = null,
        IReadOnlyCollection<Guid>? additionalPractitionerIds = null,
        EncounterType encounterType = EncounterType.Outpatient)
    {
        ArgumentNullException.ThrowIfNull(application);

        await SeedOrganizationAsync(application);

        var nowUtc = DateTime.UtcNow;
        var encounter = Encounter.Create(
            Guid.NewGuid(),
            appointmentId: null,
            patientId ?? DemoPatientPersonId,
            DemoCardiologyDepartmentId,
            DemoDoctorPersonId,
            encounterType,
            plannedStartTimeUtc: nowUtc,
            chiefComplaint: "DEMO klinik test karşılaşması",
            nowUtc,
            startImmediately: true);

        foreach (var practitionerId in additionalPractitionerIds ?? [])
        {
            encounter.AddParticipant(practitionerId, ParticipantRole.AssistingNurse, nowUtc);
        }

        await using var scope = application.Services.CreateAsyncScope();
        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
        clinicalDb.Encounters.Add(encounter);
        await clinicalDb.SaveChangesAsync();

        return encounter.Id;
    }

    internal static async Task<Guid> SeedAppointmentLinkedStartedEncounterAsync(
        ApiWebApplicationFactory application,
        Guid patientId)
    {
        ArgumentNullException.ThrowIfNull(application);

        await SeedOrganizationAsync(application);

        var nowUtc = DateTime.UtcNow;
        var appointmentId = Guid.NewGuid();
        var encounter = Encounter.Create(
            Guid.NewGuid(),
            appointmentId,
            patientId,
            DemoCardiologyDepartmentId,
            DemoDoctorPersonId,
            EncounterType.Outpatient,
            plannedStartTimeUtc: nowUtc,
            chiefComplaint: "DEMO randevuya bağlı klinik test karşılaşması",
            nowUtc,
            startImmediately: true);

        await using var scope = application.Services.CreateAsyncScope();
        var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        schedulingDb.Appointments.Add(Appointment.Create(
            appointmentId,
            Guid.NewGuid(),
            patientId,
            DemoDoctorPersonId,
            DemoCardiologyDepartmentId,
            nowUtc,
            "DEMO antenatal takip",
            nowUtc));

        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
        clinicalDb.Encounters.Add(encounter);

        await schedulingDb.SaveChangesAsync();
        await clinicalDb.SaveChangesAsync();
        return encounter.Id;
    }

    internal static async Task SeedOrganizationAsync(ApiWebApplicationFactory application)
    {
        ArgumentNullException.ThrowIfNull(application);

        await using var scope = application.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await seeder.SeedAsync();
    }
}
