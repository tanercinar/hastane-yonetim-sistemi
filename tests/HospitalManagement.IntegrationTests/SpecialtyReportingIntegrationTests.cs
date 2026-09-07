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

public sealed class SpecialtyReportingIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G05")]
    public async Task SpecialtyOperationalSummaryReturnsAggregatedMetricsWithoutSensitiveData()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var midwifeId = Guid.Parse("00000000-0000-0000-0000-000000000103");
        var encounterId = await ClinicalTestData.SeedAppointmentLinkedStartedEncounterAsync(application, patientId);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Create Pregnancy Episode
        var pregReq = new CreatePregnancyEpisodeRequest
        {
            PatientId = patientId,
            OpeningEncounterId = encounterId,
            LastMenstrualPeriodUtc = DateTime.UtcNow.AddDays(-140),
            Gravida = 2,
            Para = 1,
            Abortus = 0,
            LivingChildren = 1,
            BloodGroupAndRh = "A+",
            RiskCategory = "HighRisk",
            RiskFactorsNotes = "GDM şüphesi",
            AssignedDoctorId = doctorId,
            AssignedMidwifeId = midwifeId,
        };
        var pregResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/pregnancy-episodes", pregReq);
        Assert.Equal(HttpStatusCode.Created, pregResp.StatusCode);

        // 2. Create Delivery Record
        var newbornPatientId = await SpecialtyTestData.SeedNewbornPatientAsync(
            application,
            DateOnly.FromDateTime(DateTime.UtcNow),
            HospitalManagement.Modules.Patients.Domain.Gender.Female);
        var delReq = new CreateDeliveryRecordRequest
        {
            MotherPatientId = patientId,
            DeliveryTimeUtc = DateTime.UtcNow,
            DeliveryMode = "SpontaneousVaginal",
            AttendingDoctorId = doctorId,
            GestationalAgeWeeks = 39,
            GestationalAgeDays = 2,
            EstimatedBloodLossMl = 250,
            PerinealTear = "FirstDegree",
            Newborns =
            [
                new AddNewbornRequest
                {
                    NewbornPatientId = newbornPatientId,
                    BirthOrder = 1,
                    BirthTimeUtc = DateTime.UtcNow,
                    Gender = "Female",
                    BirthWeightGrams = 3200,
                    BirthLengthCm = 50.0m,
                    HeadCircumferenceCm = 35.0m,
                    ApgarScore1Min = 9,
                    ApgarScore5Min = 10,
                }
            ],
        };
        var delResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/deliveries", delReq);
        Assert.Equal(HttpStatusCode.Created, delResp.StatusCode);

        // 3. Create Dental Examination and Procedure
        var examReq = new CreateDentalExaminationRequest
        {
            PatientId = patientId,
            DentistId = doctorId,
            ExaminationDateUtc = DateTime.UtcNow,
            ChiefComplaint = "Diş ağrısı",
        };
        var examResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/examinations", examReq);
        Assert.Equal(HttpStatusCode.Created, examResp.StatusCode);

        var procReq = new PlanDentalProcedureRequest
        {
            PatientId = patientId,
            ToothNumber = 16,
            ProcedureCode = "DNT-FILLING",
            ProcedureName = "Kompozit Dolgu",
            EstimatedCost = 800,
            PerformedByDoctorId = doctorId,
        };
        var procResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/procedures", procReq);
        Assert.Equal(HttpStatusCode.Created, procResp.StatusCode);

        // 4. Request Home Health Visit
        var homeReq = new RequestHomeHealthVisitRequest
        {
            PatientId = patientId,
            ServiceType = "GeneralNursing",
            Priority = "Urgent",
            City = "İstanbul",
            District = "Kadıköy",
            AddressDetail = "Moda Cad. No: 12",
            ContactPhone = "05320000000",
        };
        var homeResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/home-health/visits", homeReq);
        Assert.Equal(HttpStatusCode.Created, homeResp.StatusCode);

        // 5. Query Operational Summary
        var managerClient = CreateSecureClient(application);
        var managerLogin = await LoginAsync(managerClient, "DEMO-manager@hospital.invalid", "DEMO-Manager-Pass!1");
        Assert.Equal(HttpStatusCode.OK, managerLogin.StatusCode);

        var summaryResp = await managerClient.GetAsync("/api/v1/specialty/reports/operational-summary");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);
        var summary = await summaryResp.Content.ReadFromJsonAsync<SpecialtyOperationalSummaryResponse>();
        Assert.NotNull(summary);

        Assert.Equal(1, summary.TotalDeliveriesCount);
        Assert.Equal(1, summary.NormalDeliveriesCount);
        Assert.Equal(0, summary.CesareanDeliveriesCount);
        Assert.Equal(1, summary.TotalDentalProceduresCount);
        Assert.Equal(1, summary.PlannedDentalProceduresCount);
        Assert.Equal(1, summary.TotalDentalExaminationsCount);
        Assert.Equal(1, summary.ActiveHomeVisitsCount);
        Assert.Equal(1, summary.PendingHomeVisitRequestsCount);
        Assert.Equal(1, summary.UrgentHomeVisitsCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G05")]
    public async Task SystemAdminCannotViewOperationalReports()
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

        var summaryResp = await adminClient.GetAsync("/api/v1/specialty/reports/operational-summary");
        Assert.Equal(HttpStatusCode.Forbidden, summaryResp.StatusCode);
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
