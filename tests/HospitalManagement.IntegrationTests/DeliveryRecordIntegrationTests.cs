using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Specialty;
using HospitalManagement.Host.Authorization;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
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
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class DeliveryRecordIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G02")]
    public async Task DeliveryRecordLifecycleWithTwinsAndNewbornsSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var motherPatientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var encounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(application, motherPatientId);
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Create Active Pregnancy Episode
        var lmp = DateTime.UtcNow.AddDays(-270);
        var createEpReq = new CreatePregnancyEpisodeRequest
        {
            PatientId = motherPatientId,
            OpeningEncounterId = encounterId,
            Gravida = 2,
            Para = 1,
            Abortus = 0,
            LivingChildren = 1,
            LastMenstrualPeriodUtc = lmp,
            BloodGroupAndRh = "0 Rh(+)",
            RiskCategory = "MultipleGestation",
            RiskFactorsNotes = "İkiz gebelik takibi",
        };

        var epResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/pregnancy-episodes", createEpReq);
        Assert.Equal(HttpStatusCode.Created, epResp.StatusCode);
        var episode = await epResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(episode);
        Assert.Equal("Active", episode.Status);

        // 2. Create Delivery Record with Twin #1
        var deliveryTime = DateTime.UtcNow;
        var baby1PatientId = await SpecialtyTestData.SeedNewbornPatientAsync(
            application,
            DateOnly.FromDateTime(deliveryTime),
            HospitalManagement.Modules.Patients.Domain.Gender.Male);
        var baby2PatientId = await SpecialtyTestData.SeedNewbornPatientAsync(
            application,
            DateOnly.FromDateTime(deliveryTime),
            HospitalManagement.Modules.Patients.Domain.Gender.Female);
        var createDelReq = new CreateDeliveryRecordRequest
        {
            PregnancyEpisodeId = episode.Id,
            MotherPatientId = motherPatientId,
            DeliveryMode = "CesareanEmergency",
            DeliveryTimeUtc = deliveryTime,
            GestationalAgeWeeks = 37,
            GestationalAgeDays = 4,
            PerinealTear = "None",
            EstimatedBloodLossMl = 600,
            AttendingDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102"),
            AssistingMidwifeId = Guid.Parse("00000000-0000-0000-0000-000000000103"),
            DeliverySummaryNotes = "Acil C/S ile ikiz doğum gerçekleştirildi",
            Newborns =
            [
                new AddNewbornRequest
                {
                    NewbornPatientId = baby1PatientId,
                    BirthOrder = 1,
                    BirthTimeUtc = deliveryTime,
                    Gender = "Male",
                    BirthWeightGrams = 2750,
                    BirthLengthCm = 47.0m,
                    HeadCircumferenceCm = 33.5m,
                    ApgarScore1Min = 7,
                    ApgarScore5Min = 9,
                    ApgarScore10Min = 10,
                    ResuscitationGiven = "SupplementalOxygen",
                    CordBloodPh = "7.33",
                    ComplicationsNotes = "1. bebek erkek, solunum spontan",
                }
            ]
        };

        createDelReq.Newborns[0].NewbornPatientId = motherPatientId;
        var invalidNewbornResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/deliveries",
            createDelReq);
        Assert.Equal(HttpStatusCode.BadRequest, invalidNewbornResponse.StatusCode);
        createDelReq.Newborns[0].NewbornPatientId = Guid.Empty;
        var missingNewbornResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/deliveries",
            createDelReq);
        Assert.Equal(HttpStatusCode.BadRequest, missingNewbornResponse.StatusCode);
        createDelReq.Newborns[0].NewbornPatientId = Guid.NewGuid();
        var unknownNewbornResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/specialty/deliveries",
            createDelReq);
        Assert.Equal(HttpStatusCode.BadRequest, unknownNewbornResponse.StatusCode);
        createDelReq.Newborns[0].NewbornPatientId = baby1PatientId;

        var delResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/deliveries", createDelReq);
        Assert.Equal(HttpStatusCode.Created, delResp.StatusCode);
        var delivery = await delResp.Content.ReadFromJsonAsync<DeliveryRecordResponse>();
        Assert.NotNull(delivery);
        Assert.Equal(motherPatientId, delivery.MotherPatientId);
        Assert.Equal("CesareanEmergency", delivery.DeliveryMode);
        Assert.Single(delivery.Newborns);
        Assert.Equal(1, delivery.Newborns[0].BirthOrder);
        Assert.Equal(baby1PatientId, delivery.Newborns[0].NewbornPatientId);

        // 3. Add Twin #2 via AddNewborn endpoint
        var baby2Req = new AddNewbornRequest
        {
            NewbornPatientId = baby1PatientId,
            BirthOrder = 2,
            BirthTimeUtc = deliveryTime.AddMinutes(3),
            Gender = "Female",
            BirthWeightGrams = 2600,
            BirthLengthCm = 46.0m,
            HeadCircumferenceCm = 33.0m,
            ApgarScore1Min = 8,
            ApgarScore5Min = 9,
            ApgarScore10Min = 10,
            ResuscitationGiven = "None",
            CordBloodPh = "7.36",
            ComplicationsNotes = "2. bebek kız, canlı ve pembe",
        };

        var reusedPatientResponse = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/deliveries/{delivery.Id}/newborns",
            baby2Req);
        Assert.Equal(HttpStatusCode.BadRequest, reusedPatientResponse.StatusCode);

        baby2Req.NewbornPatientId = baby2PatientId;

        var baby2Resp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/specialty/deliveries/{delivery.Id}/newborns",
            baby2Req);
        Assert.Equal(HttpStatusCode.Created, baby2Resp.StatusCode);
        var baby2 = await baby2Resp.Content.ReadFromJsonAsync<NewbornResponse>();
        Assert.NotNull(baby2);
        Assert.Equal(2, baby2.BirthOrder);
        Assert.Equal("Female", baby2.Gender);
        Assert.Equal(baby2PatientId, baby2.NewbornPatientId);

        // 4. Query Delivery by Id and verify twin array
        var getDelResp = await doctorClient.GetAsync($"/api/v1/specialty/deliveries/{delivery.Id}");
        Assert.Equal(HttpStatusCode.OK, getDelResp.StatusCode);
        var fullDelivery = await getDelResp.Content.ReadFromJsonAsync<DeliveryRecordResponse>();
        Assert.NotNull(fullDelivery);
        Assert.Equal(2, fullDelivery.Newborns.Count);

        // 5. Query Delivery by Mother Patient Id
        var motherDelResp = await doctorClient.GetAsync($"/api/v1/specialty/deliveries/mother/{motherPatientId}");
        Assert.Equal(HttpStatusCode.OK, motherDelResp.StatusCode);
        var motherDeliveries = await motherDelResp.Content.ReadFromJsonAsync<List<DeliveryRecordResponse>>();
        Assert.NotNull(motherDeliveries);
        Assert.Single(motherDeliveries);

        // 6. Verify Pregnancy Episode was automatically transitioned to Delivered
        var getEpResp = await doctorClient.GetAsync($"/api/v1/specialty/pregnancy-episodes/{episode.Id}");
        Assert.Equal(HttpStatusCode.OK, getEpResp.StatusCode);
        var updatedEp = await getEpResp.Content.ReadFromJsonAsync<PregnancyEpisodeResponse>();
        Assert.NotNull(updatedEp);
        Assert.Equal("Delivered", updatedEp.Status);

        using var constraintScope = application.Services.CreateScope();
        var specialtyDb = constraintScope.ServiceProvider.GetRequiredService<SpecialtyCareDbContext>();
        var competingDelivery = HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics.DeliveryRecord.Create(
            Guid.NewGuid(),
            pregnancyEpisodeId: null,
            motherPatientId,
            encounterId: null,
            HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics.DeliveryMode.SpontaneousVaginal,
            deliveryTime.AddHours(1),
            gestationalAgeWeeks: 38,
            gestationalAgeDays: 0,
            HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics.PerinealTearDegree.None,
            estimatedBloodLossMl: 200,
            attendingDoctorId: Guid.Parse("00000000-0000-0000-0000-000000000102"),
            assistingMidwifeId: null,
            pediatricianDoctorId: null,
            maternalComplicationsNotes: null,
            deliverySummaryNotes: null,
            DateTime.UtcNow);
        competingDelivery.AddNewborn(
            Guid.NewGuid(),
            baby1PatientId,
            birthOrder: 1,
            deliveryTime.AddHours(1),
            HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics.NewbornGender.Male,
            birthWeightGrams: 2800,
            birthLengthCm: 48,
            headCircumferenceCm: 34,
            apgarScore1Min: 8,
            apgarScore5Min: 9,
            apgarScore10Min: null,
            HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics.ResuscitationIntervention.None,
            cordBloodPh: null,
            complicationsNotes: null,
            DateTime.UtcNow);
        specialtyDb.DeliveryRecords.Add(competingDelivery);
        await Assert.ThrowsAsync<DbUpdateException>(() => specialtyDb.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G02")]
    public async Task SystemAdminCannotCreateDeliveryRecordReturnsForbidden()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var adminClient = CreateSecureClient(application);
        var adminLogin = await LoginAsync(adminClient, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
        Assert.Equal(HttpStatusCode.OK, adminLogin.StatusCode);

        var createDelReq = new CreateDeliveryRecordRequest
        {
            MotherPatientId = Guid.Parse("00000000-0000-0000-0000-000000000201"),
            DeliveryMode = "SpontaneousVaginal",
            DeliveryTimeUtc = DateTime.UtcNow,
            GestationalAgeWeeks = 39,
            GestationalAgeDays = 0,
            PerinealTear = "None",
            EstimatedBloodLossMl = 200,
            AttendingDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102"),
        };

        var response = await PostWithAntiforgeryAsync(adminClient, "/api/v1/specialty/deliveries", createDelReq);
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
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", antiforgeryResp.Token);

        return await client.SendAsync(request);
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
