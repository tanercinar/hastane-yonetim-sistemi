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

public sealed class ClinicalHandoffIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G08")]
    public async Task ClinicalHandoffFullFlowInitiateAcceptRejectAndForbiddenCheckSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patient1Id = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var patient2Id = Guid.Parse("00000000-0000-0000-0000-000000000122");

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        // 1. Doctor initiates handoff for Patient 1 (Emergency -> ICU)
        var init1Req = new InitiateClinicalHandoffRequest(
            patientId: patient1Id,
            inpatientStayId: null,
            encounterId: null,
            sourceArea: "Emergency",
            sourceLocationDetails: "Kırmızı Alan Yatak 2",
            destinationArea: "IntensiveCareUnit",
            destinationLocationDetails: "ICU Yatak 1",
            situation: "Septik şok ve solunum yetmezliği",
            background: "DM, KKY, antibiyoterapi başlandı",
            assessment: "TA: 85/50 mmHg, Laktat: 3.5, entübe edildi",
            recommendation: "Ventilatör idamesi ve inotrop titrasyonu",
            criticalAlerts: "Temas İzolasyonu");

        var init1Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-handoffs", init1Req);
        Assert.Equal(HttpStatusCode.Created, init1Resp.StatusCode);
        var handoff1 = await init1Resp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(handoff1);
        Assert.Equal("PendingAcceptance", handoff1.Status);

        // 2. Doctor attempts to self-accept -> 409 Conflict
        var selfAcceptResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/clinical-handoffs/{handoff1.Id}/accept",
            new AcceptClinicalHandoffRequest("Kendi kendime onaylıyorum"));

        Assert.Equal(HttpStatusCode.Conflict, selfAcceptResp.StatusCode);

        // 3. Nurse accepts handoff from destination team
        var acceptResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-handoffs/{handoff1.Id}/accept",
            new AcceptClinicalHandoffRequest("Hasta YBÜ yatağına kabul edildi, monitörizasyon sağlandı."));

        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);
        var acceptedHandoff = await acceptResp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(acceptedHandoff);
        Assert.Equal("Accepted", acceptedHandoff.Status);
        Assert.NotNull(acceptedHandoff.AcceptedAtUtc);

        // 4. Doctor initiates handoff for Patient 2 (Emergency -> OperatingRoom)
        var init2Req = new InitiateClinicalHandoffRequest(
            patientId: patient2Id,
            inpatientStayId: null,
            encounterId: null,
            sourceArea: "Emergency",
            sourceLocationDetails: "Sarı Alan Yatak 4",
            destinationArea: "OperatingRoom",
            destinationLocationDetails: "Salon 1",
            situation: "Akut apandisit",
            background: "Ek hastalık yok",
            assessment: "Vital stabil, batın defans+",
            recommendation: "Acil lap. apandektomi",
            criticalAlerts: null);

        var init2Resp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/clinical-handoffs", init2Req);
        Assert.Equal(HttpStatusCode.Created, init2Resp.StatusCode);
        var handoff2 = await init2Resp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(handoff2);

        // 5. Nurse rejects handoff with reason (e.g. pre-op checklist incomplete)
        var rejectResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/clinical-handoffs/{handoff2.Id}/reject",
            new RejectClinicalHandoffRequest("Onam formu ve pre-op kan grubu teyidi eksik. Tamamlayıp tekrar iletiniz."));

        Assert.Equal(HttpStatusCode.OK, rejectResp.StatusCode);
        var rejectedHandoff = await rejectResp.Content.ReadFromJsonAsync<ClinicalHandoffResponse>();
        Assert.NotNull(rejectedHandoff);
        Assert.Equal("Rejected", rejectedHandoff.Status);
        Assert.Contains("Onam formu", rejectedHandoff.StatusReason, StringComparison.Ordinal);

        // 6. Security check: SystemAdministrator forbidden check
        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var forbiddenResp = await PostWithAntiforgeryAsync(adminClient, "/api/v1/clinical-handoffs", init1Req);
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
