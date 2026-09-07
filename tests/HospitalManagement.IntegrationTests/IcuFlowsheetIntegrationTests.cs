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

public sealed class IcuFlowsheetIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G07")]
    public async Task IcuFlowsheetFullFlowAddObservationGetListFluidSummaryAndForbiddenCheckSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var stayId = Guid.NewGuid();

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Get ICU Beds and Admit Patient
        var beds = await doctorClient.GetFromJsonAsync<List<IcuBedResponse>>("/api/v1/icu/beds");
        Assert.NotNull(beds);
        var bed = beds.First(b => b.BedCode == "DEMO-ICU-01");

        var admitReq = new CreateIcuAdmissionRequest(
            inpatientStayId: stayId,
            patientId: patientId,
            encounterId: null,
            icuBedId: bed.Id,
            attendingDoctorId: doctorPersonId,
            primaryNurseId: null,
            admissionReason: "Post-Op ARDS ve ventilatör desteği",
            acuityLevel: "Level3MultiOrganSupport",
            monitoringFrequencyMinutes: 15,
            ventilationMode: "InvasiveMechanical",
            carePlanNotes: "Sedatize");

        var admitResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/icu/admissions", admitReq);
        Assert.Equal(HttpStatusCode.Created, admitResp.StatusCode);
        var admission = await admitResp.Content.ReadFromJsonAsync<IcuAdmissionResponse>();
        Assert.NotNull(admission);

        // 2. Add Flowsheet Observation Entry
        var flowsheetReq = new CreateIcuFlowsheetEntryRequest
        {
            HeartRateBpm = 86,
            SystolicBpMmHg = 125,
            DiastolicBpMmHg = 80,
            RespiratoryRateBpm = 18,
            OxygenSaturationPct = 97.5m,
            BodyTemperatureCelsius = 37.2m,
            GlasgowComaScale = 14,
            RichmondAgitationSedationScale = -1,
            VentilationMode = "InvasiveMechanical",
            FractionOfInspiredOxygenPct = 40,
            PositiveEndExpiratoryPressure = 8,
            TidalVolumeMl = 480,
            PeakInspiratoryPressure = 22,
            IvFluidIntakeMl = 120,
            EnteralNutritionIntakeMl = 60,
            UrineOutputMl = 90,
            DrainOutputMl = 15,
            ClinicalNotes = "Hemodinamik stabil, aspirasyonda az miktarda seröz salgı",
        };

        var flowsheetResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/icu/admissions/{admission.Id}/flowsheet",
            flowsheetReq);

        Assert.Equal(HttpStatusCode.Created, flowsheetResp.StatusCode);
        var entry = await flowsheetResp.Content.ReadFromJsonAsync<IcuFlowsheetEntryResponse>();
        Assert.NotNull(entry);
        Assert.Equal(86, entry.HeartRateBpm);
        Assert.Equal(95, entry.MeanArterialPressureMmHg); // (2*80 + 125)/3 = 285/3 = 95
        Assert.Equal(180, entry.TotalIntakeMl); // 120 + 60
        Assert.Equal(105, entry.TotalOutputMl); // 90 + 15
        Assert.Equal(75, entry.NetFluidBalanceMl); // 180 - 105

        // 3. Get Flowsheet Entries List
        var entries = await doctorClient.GetFromJsonAsync<List<IcuFlowsheetEntryResponse>>(
            $"/api/v1/icu/admissions/{admission.Id}/flowsheet");

        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal(entry.Id, entries[0].Id);

        // 4. Get Fluid Balance Summary
        var fluidSummary = await doctorClient.GetFromJsonAsync<IcuFluidBalanceSummaryResponse>(
            $"/api/v1/icu/admissions/{admission.Id}/flowsheet/fluid-balance");

        Assert.NotNull(fluidSummary);
        Assert.Equal(1, fluidSummary.EntryCount);
        Assert.Equal(180, fluidSummary.TotalIntakeMl);
        Assert.Equal(105, fluidSummary.TotalOutputMl);
        Assert.Equal(75, fluidSummary.NetBalanceMl);

        // 5. Non-clinical user (SystemAdministrator) forbidden check
        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var forbiddenResp = await PostWithAntiforgeryAsync(
            adminClient,
            $"/api/v1/icu/admissions/{admission.Id}/flowsheet",
            flowsheetReq);

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
