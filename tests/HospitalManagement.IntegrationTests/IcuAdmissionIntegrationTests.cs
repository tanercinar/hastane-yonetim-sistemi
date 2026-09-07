using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class IcuAdmissionIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G06")]
    public async Task IcuAdmissionFullFlowAdmitBedCollisionCarePlanTransferAndForbiddenCheckSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patient1Id = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var patient2Id = Guid.Parse("00000000-0000-0000-0000-000000000122");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var stay1Id = Guid.NewGuid();
        var stay2Id = Guid.NewGuid();

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Get ICU Beds
        var beds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        Assert.NotNull(beds);
        Assert.True(beds.Count >= 6);
        var bed1 = beds.First(b => b.BedCode == "DEMO-ICU-01");
        Assert.False(bed1.IsOccupied);

        // 2. Admit Patient 1 to Bed 1
        var admit1Req = new CreateIcuAdmissionRequest(
            inpatientStayId: stay1Id,
            patientId: patient1Id,
            encounterId: null,
            icuBedId: bed1.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "Akut solunum yetmezliği ve ARDS",
            acuityLevel: "Level3MultiOrganSupport",
            monitoringFrequencyMinutes: 15,
            ventilationMode: "InvasiveMechanical",
            carePlanNotes: "Sedasyon ve nöromüsküler blokaj protokolü");

        var admit1Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/icu/admissions", admit1Req);
        Assert.Equal(HttpStatusCode.Created, admit1Resp.StatusCode);
        var adm1 = await admit1Resp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(adm1);
        Assert.Equal("Active", adm1.Status);
        Assert.Equal("DEMO-ICU-01", adm1.IcuBedCode);

        // 3. Bed collision: Attempt to admit Patient 2 to Bed 1 -> 409 Conflict
        var admit2Req = new CreateIcuAdmissionRequest(
            inpatientStayId: stay2Id,
            patientId: patient2Id,
            encounterId: null,
            icuBedId: bed1.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "Miyokard enfarktüsü sonrası arrest",
            acuityLevel: "Level3MultiOrganSupport",
            monitoringFrequencyMinutes: 15,
            ventilationMode: "InvasiveMechanical",
            carePlanNotes: null);

        var admit2Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/icu/admissions", admit2Req);
        Assert.Equal(HttpStatusCode.Conflict, admit2Resp.StatusCode);

        // 4. Update Care Plan
        var carePlanReq = new UpdateIcuCarePlanRequest(
            acuityLevel: "Level2IntensiveMonitoring",
            monitoringFrequencyMinutes: 30,
            ventilationMode: "NonInvasiveCpapBiPap",
            primaryNurseId: null,
            carePlanNotes: "Extübe edildi, BiPAP ile oksijenizasyon stabil");

        var carePlanResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{adm1.Id}/care-plan",
            carePlanReq);

        Assert.Equal(HttpStatusCode.OK, carePlanResp.StatusCode);
        var updatedAdm = await carePlanResp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(updatedAdm);
        Assert.Equal("Level2IntensiveMonitoring", updatedAdm.AcuityLevel);
        Assert.Equal("NonInvasiveCpapBiPap", updatedAdm.VentilationMode);

        // 5. Transfer to Regular Ward
        var transferReq = new IcuDischargeOrTransferRequest(
            destinationStatus: "TransferredToWard",
            dischargeNotes: "Hasta stabil, solunum oda havasında yeterli, dahiliye servisine transfer edildi.");

        var transferResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{adm1.Id}/discharge-or-transfer",
            transferReq);

        Assert.Equal(HttpStatusCode.OK, transferResp.StatusCode);
        var transferredAdm = await transferResp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(transferredAdm);
        Assert.Equal("TransferredToWard", transferredAdm.Status);

        // 6. Verify Bed 1 is now free
        var updatedBeds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        Assert.NotNull(updatedBeds);
        var updatedBed1 = updatedBeds.First(b => b.BedCode == "DEMO-ICU-01");
        Assert.False(updatedBed1.IsOccupied);

        // 7. Security check: SystemAdministrator has NO clinical permission -> 403 Forbidden
        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var forbiddenResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/icu/admissions", admit1Req);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);
    }

    private static HttpClient CreateSecureClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("https://localhost/"),
        });

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        PostWithAntiforgeryAsync(
            client,
            "/api/v1/identity/sessions",
            new LoginRequest { Email = email, Password = password });

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        return await client.SendAsync(request);
    }

    private static async Task RunAllMigrationsAndSeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        var idDb = sp.GetRequiredService<IdentityAccessDbContext>();
        await idDb.Database.MigrateAsync();

        var auditDb = sp.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var patDb = sp.GetRequiredService<PatientsDbContext>();
        await patDb.Database.MigrateAsync();

        var orgDb = sp.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var clinDb = sp.GetRequiredService<ClinicalRecordsDbContext>();
        await clinDb.Database.MigrateAsync();

        var diagDb = sp.GetRequiredService<DiagnosticsDbContext>();
        await diagDb.Database.MigrateAsync();

        var rxDb = sp.GetRequiredService<PharmacyDbContext>();
        await rxDb.Database.MigrateAsync();

        var schedDb = sp.GetRequiredService<SchedulingDbContext>();
        await schedDb.Database.MigrateAsync();

        var inpDb = sp.GetRequiredService<InpatientDbContext>();
        await inpDb.Database.MigrateAsync();

        var notifDb = sp.GetRequiredService<NotificationsDbContext>();
        await notifDb.Database.MigrateAsync();

        var emgDb = sp.GetRequiredService<EmergencyDbContext>();
        await emgDb.Database.MigrateAsync();

        var surgDb = sp.GetRequiredService<SurgeryDbContext>();
        await surgDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var inpSeeder = sp.GetRequiredService<IInpatientDataSeeder>();
        await inpSeeder.SeedAsync();

        var emgSeeder = sp.GetRequiredService<IEmergencyDataSeeder>();
        await emgSeeder.SeedAsync();

        var surgSeeder = sp.GetRequiredService<ISurgeryDataSeeder>();
        await surgSeeder.SeedAsync();
    }
}
