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

public sealed class DentalCareIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G03")]
    public async Task DentalCareLifecycleOdontogramAndProceduresSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var dentistId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Create Dental Examination
        var createExamReq = new CreateDentalExaminationRequest
        {
            PatientId = patientId,
            DentistId = dentistId,
            ExaminationDateUtc = DateTime.UtcNow,
            ChiefComplaint = "Sol üst çenede soğuk hassasiyeti",
            DiagnosisNotes = "Diş #16 oklüzal kavite tespit edildi",
            TreatmentPlanSummary = "16 no'lu dişe kompozit dolgu planlandı",
        };

        var examResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/examinations", createExamReq);
        Assert.Equal(HttpStatusCode.Created, examResp.StatusCode);
        var exam = await examResp.Content.ReadFromJsonAsync<DentalExaminationResponse>();
        Assert.NotNull(exam);
        Assert.Equal(patientId, exam.PatientId);

        // 2. Record Tooth Condition #16: Caries (v1)
        var toothCariesReq = new RecordToothConditionRequest
        {
            ToothNumber = 16,
            Condition = "Caries",
            AffectedSurfaces = 4, // Occlusal
            Notes = "Oklüzal derin çürük",
        };

        var tooth1Resp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothCariesReq);
        Assert.Equal(HttpStatusCode.Created, tooth1Resp.StatusCode);
        var tooth1 = await tooth1Resp.Content.ReadFromJsonAsync<ToothConditionResponse>();
        Assert.NotNull(tooth1);
        Assert.Equal(16, tooth1.ToothNumber);
        Assert.Equal("Caries", tooth1.Condition);
        Assert.Equal(1, tooth1.Version);

        // 3. Plan Dental Procedure (Kompozit Dolgu)
        var planProcReq = new PlanDentalProcedureRequest
        {
            PatientId = patientId,
            ToothNumber = 16,
            Surfaces = 4,
            ProcedureCode = "DNT-FILLING",
            ProcedureName = "Kompozit Dolgu",
            EstimatedCost = 850,
            PerformedByDoctorId = dentistId,
            ScheduledDateUtc = DateTime.UtcNow.AddDays(1),
            ClinicalNotes = "16 no'lu diş tek yüz dolgu",
        };

        var procResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/specialty/dental/procedures", planProcReq);
        Assert.Equal(HttpStatusCode.Created, procResp.StatusCode);
        var proc = await procResp.Content.ReadFromJsonAsync<DentalProcedureResponse>();
        Assert.NotNull(proc);
        Assert.Equal("Planned", proc.Status);

        // 4. Complete Dental Procedure
        var completeProcReq = new CompleteDentalProcedureRequest
        {
            CompletedDateUtc = DateTime.UtcNow,
            CompletionNotes = "Kompozit dolgu başarıyla uygulandı ve cila yapıldı",
        };

        var completeResp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/procedures/{proc.Id}/complete", completeProcReq);
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedProc = await completeResp.Content.ReadFromJsonAsync<DentalProcedureResponse>();
        Assert.NotNull(completedProc);
        Assert.Equal("Completed", completedProc.Status);

        // 5. Update Tooth Condition #16: Filled (v2) - verifying history preservation
        var toothFilledReq = new RecordToothConditionRequest
        {
            ToothNumber = 16,
            Condition = "Filled",
            AffectedSurfaces = 4,
            Notes = "Kompozit dolgulu sağlam",
        };

        var tooth2Resp = await PostWithAntiforgeryAsync(doctorClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothFilledReq);
        Assert.Equal(HttpStatusCode.Created, tooth2Resp.StatusCode);
        var tooth2 = await tooth2Resp.Content.ReadFromJsonAsync<ToothConditionResponse>();
        Assert.NotNull(tooth2);
        Assert.Equal("Filled", tooth2.Condition);
        Assert.Equal(2, tooth2.Version);

        // 6. Verify Tooth #16 History returns both v1 and v2
        var historyResp = await doctorClient.GetAsync($"/api/v1/specialty/dental/odontogram/{patientId}/tooth/16/history");
        Assert.Equal(HttpStatusCode.OK, historyResp.StatusCode);
        var history = await historyResp.Content.ReadFromJsonAsync<List<ToothConditionResponse>>();
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Equal(2, history[0].Version);
        Assert.Equal(1, history[1].Version);

        // 7. Verify Latest Odontogram contains tooth #16 as Filled
        var odontogramResp = await doctorClient.GetAsync($"/api/v1/specialty/dental/odontogram/{patientId}");
        Assert.Equal(HttpStatusCode.OK, odontogramResp.StatusCode);
        var odontogram = await odontogramResp.Content.ReadFromJsonAsync<List<ToothConditionResponse>>();
        Assert.NotNull(odontogram);
        Assert.Single(odontogram);
        Assert.Equal("Filled", odontogram[0].Condition);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F09-G03")]
    public async Task SystemAdminCannotRecordDentalConditionReturnsForbidden()
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

        var toothReq = new RecordToothConditionRequest
        {
            ToothNumber = 11,
            Condition = "Sound",
        };

        var response = await PostWithAntiforgeryAsync(adminClient, $"/api/v1/specialty/dental/odontogram/{patientId}/tooth", toothReq);
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
