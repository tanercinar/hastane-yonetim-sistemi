using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Host.Authorization;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class HomeHealthIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G04")]
    public async Task HomeHealthVisitLifecycleRequestAssignStartCompleteSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var otherPatientId = Guid.Parse("00000000-0000-0000-0000-000000000202");
        var staffId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var homeHealthEncounterId = await SeedEncounterAsync(
            application,
            patientId,
            EncounterType.HomeHealth,
            startImmediately: true);
        var otherPatientEncounterId = await SeedEncounterAsync(
            application,
            otherPatientId,
            EncounterType.HomeHealth,
            startImmediately: true);
        var outpatientEncounterId = await SeedEncounterAsync(
            application,
            patientId,
            EncounterType.Outpatient,
            startImmediately: true);
        var plannedHomeHealthEncounterId = await SeedEncounterAsync(
            application,
            patientId,
            EncounterType.HomeHealth,
            startImmediately: false);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Request Home Health Visit
        var requestVisit = new RequestHomeHealthVisitRequest
        {
            PatientId = patientId,
            ServiceType = "WoundDressing",
            Priority = "Urgent",
            City = "DEMO İstanbul",
            District = "DEMO Kadıköy",
            AddressDetail = "DEMO adres — gerçek değildir",
            ContactPhone = "DEMO-000-000-0000",
            InitialNotes = "Postop yara pansumanı ve dren kontrolü",
        };

        var reqResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/home-health/visits", requestVisit);
        Assert.Equal(HttpStatusCode.Created, reqResp.StatusCode);
        var visit = await reqResp.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(visit);
        Assert.StartsWith("DEMO-HOM-", visit.ProtocolNumber, StringComparison.Ordinal);
        Assert.Equal("Requested", visit.Status);
        Assert.Equal("WoundDressing", visit.ServiceType);

        // 2. Assign Team
        var assignReq = new AssignHomeHealthTeamRequest
        {
            AssignedStaffId = staffId,
            ScheduledDateUtc = DateTime.UtcNow.AddHours(2),
        };

        var invalidAssignResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/assign",
            new AssignHomeHealthTeamRequest
            {
                AssignedStaffId = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                ScheduledDateUtc = DateTime.UtcNow.AddHours(2),
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAssignResponse.StatusCode);

        var assignResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/home-health/visits/{visit.Id}/assign", assignReq);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);
        var assignedVisit = await assignResp.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(assignedVisit);
        Assert.Equal("Assigned", assignedVisit.Status);
        Assert.Equal(staffId, assignedVisit.AssignedStaffId);

        Assert.Equal(string.Empty, assignedVisit.AddressDetail);
        Assert.Equal(string.Empty, assignedVisit.ContactPhone);

        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var assignedForNurseResponse = await nurseClient.GetAsync($"/api/v1/specialty/home-health/visits/{visit.Id}");
        Assert.Equal(HttpStatusCode.OK, assignedForNurseResponse.StatusCode);
        var assignedForNurse = await assignedForNurseResponse.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(assignedForNurse);
        Assert.Equal("DEMO adres — gerçek değildir", assignedForNurse.AddressDetail);
        Assert.Equal("DEMO-000-000-0000", assignedForNurse.ContactPhone);

        // 3. Start Visit (only the assigned staff member may execute the visit)
        var startResp = await PostWithAntiforgeryAsync<object?>(nurseClient, $"/api/v1/specialty/home-health/visits/{visit.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startedVisit = await startResp.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(startedVisit);
        Assert.Equal("InProgress", startedVisit.Status);

        // 4. Complete Visit
        var completeReq = new CompleteHomeHealthVisitRequest
        {
            ClinicalNotes = "Dikiş hattı temiz, enfeksiyon bulgusu yok. Steril pansuman yenilendi.",
            VitalsSummaryNotes = "TA: 120/80 mmHg, Nabız: 76 bpm, Ateş: 36.6 °C",
        };

        var missingEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, missingEncounterResponse.StatusCode);

        completeReq.EncounterId = Guid.NewGuid();
        var unknownEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, unknownEncounterResponse.StatusCode);

        completeReq.EncounterId = otherPatientEncounterId;
        var otherPatientEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, otherPatientEncounterResponse.StatusCode);

        completeReq.EncounterId = outpatientEncounterId;
        var wrongTypeEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, wrongTypeEncounterResponse.StatusCode);

        completeReq.EncounterId = plannedHomeHealthEncounterId;
        var plannedEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{visit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, plannedEncounterResponse.StatusCode);

        completeReq.EncounterId = homeHealthEncounterId;
        var completeResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/specialty/home-health/visits/{visit.Id}/complete", completeReq);
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedVisit = await completeResp.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(completedVisit);
        Assert.Equal("Completed", completedVisit.Status);
        Assert.NotNull(completedVisit.VisitCompletedAtUtc);
        Assert.Equal(homeHealthEncounterId, completedVisit.EncounterId);

        var secondVisit = await RequestAssignAndStartVisitAsync(
            doctorClient,
            nurseClient,
            patientId,
            staffId);
        var reusedEncounterResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{secondVisit.Id}/complete",
            completeReq);
        Assert.Equal(HttpStatusCode.BadRequest, reusedEncounterResponse.StatusCode);

        // 5. Query Patient Visits
        var listResp = await doctorClient.GetAsync($"/api/v1/specialty/home-health/visits/patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var list = await listResp.Content.ReadFromJsonAsync<List<HomeHealthVisitResponse>>();
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, item => item.Id == visit.Id && item.Status == "Completed");

        await using var conflictScope = application.Services.CreateAsyncScope();
        var specialtyDb = conflictScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
        var competingVisit = HomeHealthVisit.Request(
            Guid.NewGuid(),
            patientId,
            HomeCareServiceType.GeneralNursing,
            HomeVisitPriority.Routine,
            "DEMO İstanbul",
            "DEMO Kadıköy",
            "DEMO yarış adresi — gerçek değildir",
            "DEMO-000-000-0001",
            ClinicalTestData.DemoDoctorPersonId,
            null,
            DateTime.UtcNow);
        competingVisit.AssignTeam(staffId, DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        competingVisit.StartVisit(DateTime.UtcNow);
        competingVisit.CompleteVisit("DEMO yarış notu", null, homeHealthEncounterId, DateTime.UtcNow);
        specialtyDb.HomeHealthVisits.Add(competingVisit);
        var conflict = await Assert.ThrowsAsync<DbUpdateException>(() => specialtyDb.SaveChangesAsync());
        var postgresConflict = Assert.IsType<PostgresException>(conflict.InnerException);
        Assert.Equal("UX_HomeHealthVisits_EncounterId", postgresConflict.ConstraintName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G04")]
    public async Task SystemAdminCannotRequestHomeHealthVisitReturnsForbidden()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var requestVisit = new RequestHomeHealthVisitRequest
        {
            PatientId = patientId,
            City = "İstanbul",
            District = "Kadıköy",
            AddressDetail = "Moda Cad.",
            ContactPhone = "05320000000",
        };

        var response = await PostWithAntiforgeryAsync(adminClient, "/api/v1/specialty/home-health/visits", requestVisit);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> application)
    {
        return application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = email,
                Password = password,
            }),
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<T>(
        HttpClient client,
        string url,
        T body)
    {
        var antiforgeryResp = await client.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(antiforgeryResp);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is not null ? JsonContent.Create(body) : null,
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
    }

    private static async Task<HomeHealthVisitResponse> RequestAssignAndStartVisitAsync(
        HttpClient doctorClient,
        HttpClient nurseClient,
        Guid patientId,
        Guid staffId)
    {
        var requestResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/home-health/visits",
            new RequestHomeHealthVisitRequest
            {
                PatientId = patientId,
                ServiceType = "GeneralNursing",
                Priority = "Routine",
                City = "DEMO İstanbul",
                District = "DEMO Kadıköy",
                AddressDetail = "DEMO ikinci adres — gerçek değildir",
                ContactPhone = "DEMO-000-000-0002",
            });
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var requestedVisit = await requestResponse.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(requestedVisit);

        var assignResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/home-health/visits/{requestedVisit.Id}/assign",
            new AssignHomeHealthTeamRequest
            {
                AssignedStaffId = staffId,
                ScheduledDateUtc = DateTime.UtcNow.AddHours(3),
            });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var startResponse = await PostWithAntiforgeryAsync<object?>(
            nurseClient,
            $"/api/v1/specialty/home-health/visits/{requestedVisit.Id}/start",
            null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var startedVisit = await startResponse.Content.ReadFromJsonAsync<HomeHealthVisitResponse>();
        Assert.NotNull(startedVisit);
        return startedVisit;
    }

    private static async Task<Guid> SeedEncounterAsync(
        ApiWebApplicationFactory application,
        Guid patientId,
        EncounterType encounterType,
        bool startImmediately)
    {
        var nowUtc = DateTime.UtcNow;
        var encounter = Encounter.Create(
            Guid.NewGuid(),
            appointmentId: null,
            patientId,
            ClinicalTestData.DemoCardiologyDepartmentId,
            ClinicalTestData.DemoDoctorPersonId,
            encounterType,
            plannedStartTimeUtc: nowUtc,
            chiefComplaint: "DEMO evde sağlık karşılaşması",
            nowUtc,
            startImmediately);

        await using var scope = application.Services.CreateAsyncScope();
        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
        clinicalDb.Encounters.Add(encounter);
        await clinicalDb.SaveChangesAsync();
        return encounter.Id;
    }

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> application)
    {
        using var scope = application.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var audDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await audDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var schDb = sp.GetRequiredService<SchedulingDbContext>();
        await schDb.Database.MigrateAsync();

        var notDb = sp.GetRequiredService<NotificationsDbContext>();
        await notDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var pharmDb = sp.GetRequiredService<PharmacyDbContext>();
        await pharmDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var specDb = sp.GetRequiredService<SpecialtyCareDbContext>();
        await specDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var patientSeeder = sp.GetRequiredService<IPatientDataSeeder>();
        await patientSeeder.SeedAsync();

        sp.GetRequiredService<CareRelationshipRegistry>().EstablishCareRelationship(
            Guid.Parse("00000000-0000-0000-0000-000000000102"),
            Guid.Parse("00000000-0000-0000-0000-000000000109"));
    }
}
