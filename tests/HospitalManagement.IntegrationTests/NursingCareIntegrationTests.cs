using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Inpatient;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class NursingCareIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G05")]
    public async Task NursingObservationsAndCarePlansWorkEndToEndWithCorrectionsAndTaskLifecycle()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var cardDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // 1. Doctor requests and accepts admission, placing patient in bed
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut Koroner Sendrom",
            DietType = "LowSodium",
            FallRiskScore = 60,
            IsolationRequired = "Contact",
            EstimatedStayDays = 4,
        };

        var admReqResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, admReqResponse.StatusCode);
        var admission = await admReqResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admission);

        // 2. Nurse logs in, accepts and admits patient
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var acceptResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/accept", new AcceptAdmissionRequest());
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);

        var availableBeds = await nurseClient.GetFromJsonAsync<List<BedResponse>>($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.NotNull(availableBeds);
        var bed = availableBeds.First();

        var admitResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{admission.Id}/admit", new AdmitPatientRequest { BedId = bed.Id });
        Assert.Equal(HttpStatusCode.OK, admitResp.StatusCode);

        // 3. Nurse records vital observation & fluid balance
        var obsReq = new RecordObservationRequest
        {
            AdmissionId = admission.Id,
            SystolicBp = 135,
            DiastolicBp = 85,
            HeartRate = 78,
            RespiratoryRate = 18,
            BodyTemperatureCelsius = 37.1m,
            OxygenSaturationPercent = 97,
            PainScale = 3,
            OralIntakeMl = 300,
            IvIntakeMl = 500,
            UrineOutputMl = 400,
            DrainOutputMl = 0,
            OtherOutputMl = 0,
            Consciousness = "Alert",
            ClinicalNotes = "Hasta dinleniyor, hafif göğüs rahatsızlığı mevcut.",
        };

        var obsResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/nursing/observations", obsReq);
        Assert.Equal(HttpStatusCode.Created, obsResp.StatusCode);
        var observation = await obsResp.Content.ReadFromJsonAsync<NursingObservationResponse>();
        Assert.NotNull(observation);
        Assert.Equal(135, observation.SystolicBp);
        Assert.False(observation.IsCorrection);

        // 4. Nurse creates a correction for the observation (e.g. repeated BP check)
        var corrReq = new CorrectObservationRequest
        {
            CorrectionReason = "Tansiyon 15 dakika dinlenme sonrası tekrar ölçüldü.",
            SystolicBp = 125,
            DiastolicBp = 80,
            HeartRate = 72,
            BodyTemperatureCelsius = 37.0m,
            OxygenSaturationPercent = 98,
            PainScale = 1,
            OralIntakeMl = 300,
            IvIntakeMl = 500,
            UrineOutputMl = 400,
            DrainOutputMl = 0,
            OtherOutputMl = 0,
            Consciousness = "Alert",
            ClinicalNotes = "Tansiyon regüle.",
        };

        var corrResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/nursing/observations/{observation.Id}/correct", corrReq);
        Assert.Equal(HttpStatusCode.OK, corrResp.StatusCode);
        var correction = await corrResp.Content.ReadFromJsonAsync<NursingObservationResponse>();
        Assert.NotNull(correction);
        Assert.True(correction.IsCorrection);
        Assert.Equal(observation.Id, correction.CorrectedObservationId);
        Assert.Equal(125, correction.SystolicBp);

        // 5. Query observations list
        var obsList = await nurseClient.GetFromJsonAsync<List<NursingObservationResponse>>($"/api/v1/inpatient/nursing/observations?admissionId={admission.Id}");
        Assert.NotNull(obsList);
        Assert.Equal(2, obsList.Count);

        // 6. Nurse creates Care Plan
        var planReq = new CreateCarePlanRequest
        {
            AdmissionId = admission.Id,
            NursingDiagnosis = "Düşme Riski ve Akut Ağrı",
            Goal = "Yatış sürecinde düşme olmaması ve ağrı skorunun 2 altında tutulması",
        };

        var planResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/nursing/care-plans", planReq);
        Assert.Equal(HttpStatusCode.Created, planResp.StatusCode);
        var plan = await planResp.Content.ReadFromJsonAsync<NursingCarePlanResponse>();
        Assert.NotNull(plan);
        Assert.Equal("Active", plan.Status);

        // 7. Nurse adds a task to Care Plan
        var taskReq = new AddCareTaskRequest
        {
            Title = "2 saatte bir yatak başı güvenlik ve ağrı değerlendirmesi",
            Frequency = "Q2H",
            DueTimeUtc = DateTime.UtcNow.AddHours(2),
        };

        var taskResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/nursing/care-plans/{plan.Id}/tasks", taskReq);
        Assert.Equal(HttpStatusCode.Created, taskResp.StatusCode);
        var task = await taskResp.Content.ReadFromJsonAsync<NursingCareTaskResponse>();
        Assert.NotNull(task);
        Assert.Equal("Pending", task.Status);

        // 8. Nurse completes the task
        var completeReq = new CompleteCareTaskRequest
        {
            Notes = "Yatak kenarlıkları kontrol edildi, ağrı skoru 1.",
        };

        var completeResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/nursing/tasks/{task.Id}/complete", completeReq);
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);
        var completedTask = await completeResp.Content.ReadFromJsonAsync<NursingCareTaskResponse>();
        Assert.NotNull(completedTask);
        Assert.Equal("Completed", completedTask.Status);
        Assert.Equal("Yatak kenarlıkları kontrol edildi, ağrı skoru 1.", completedTask.CompletionNotes);

        // 9. Two concurrent task completions produce one success and one conflict.
        var competingTaskResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/nursing/care-plans/{plan.Id}/tasks",
            new AddCareTaskRequest
            {
                Title = "DEMO eşzamanlı bakım görevi",
                Frequency = "Once",
                DueTimeUtc = DateTime.UtcNow.AddHours(1),
            });
        Assert.Equal(HttpStatusCode.Created, competingTaskResponse.StatusCode);
        var competingTask = await competingTaskResponse.Content.ReadFromJsonAsync<NursingCareTaskResponse>();
        Assert.NotNull(competingTask);
        var secondNurseClient = CreateSecureClient(application);
        var secondNurseLogin = await LoginAsync(
            secondNurseClient,
            "DEMO-nurse@hospital.invalid",
            "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, secondNurseLogin.StatusCode);
        var competingCompletions = await Task.WhenAll(
            PostWithAntiforgeryAsync(
                nurseClient,
                $"/api/v1/inpatient/nursing/tasks/{competingTask.Id}/complete",
                completeReq),
            PostWithAntiforgeryAsync(
                secondNurseClient,
                $"/api/v1/inpatient/nursing/tasks/{competingTask.Id}/complete",
                completeReq));
        Assert.Single(competingCompletions, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(competingCompletions, response => response.StatusCode == HttpStatusCode.Conflict);

        // 10. Reading overdue tasks returns a derived overdue state without mutating clinical data on GET.
        var overdueTaskResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/nursing/care-plans/{plan.Id}/tasks",
            new AddCareTaskRequest
            {
                Title = "DEMO gecikmiş bakım görevi",
                Frequency = "Once",
                DueTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            });
        Assert.Equal(HttpStatusCode.Created, overdueTaskResponse.StatusCode);
        var overdueTask = await overdueTaskResponse.Content.ReadFromJsonAsync<NursingCareTaskResponse>();
        Assert.NotNull(overdueTask);

        var overdueTasks = await nurseClient.GetFromJsonAsync<List<NursingCareTaskResponse>>(
            $"/api/v1/inpatient/nursing/tasks/overdue?admissionId={admission.Id}");
        Assert.NotNull(overdueTasks);
        Assert.Contains(overdueTasks, item => item.Id == overdueTask.Id && item.Status == "Overdue");

        await using (var verificationScope = application.Services.CreateAsyncScope())
        {
            var inpatientDb = verificationScope.ServiceProvider.GetRequiredService<InpatientDbContext>();
            var persistedStatus = await inpatientDb.NursingCareTasks
                .Where(item => item.Id == overdueTask.Id)
                .Select(item => item.Status)
                .SingleAsync();
            Assert.Equal(CareTaskStatus.Pending, persistedStatus);
        }

        // 11. Query Care Plans with tasks
        var carePlans = await nurseClient.GetFromJsonAsync<List<NursingCarePlanResponse>>($"/api/v1/inpatient/nursing/care-plans?admissionId={admission.Id}");
        Assert.NotNull(carePlans);
        Assert.Single(carePlans);
        Assert.Equal(3, carePlans[0].Tasks.Count);
        Assert.Equal(2, carePlans[0].Tasks.Count(item => item.Status == "Completed"));
        Assert.Single(carePlans[0].Tasks, item => item.Status == "Pending");

        // 12. Negative Test: Anonymous client cannot record observation
        var anonClient = application.CreateClient();
        var anonObsMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/inpatient/nursing/observations")
        {
            Content = JsonContent.Create(obsReq),
        };
        var anonResp = await anonClient.SendAsync(anonObsMsg);
        Assert.True(anonResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
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

    private static async Task RunAllMigrationsAndSeedAsync(ApiWebApplicationFactory application)
    {
        await using var scope = application.Services.CreateAsyncScope();

        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        await identityDb.Database.MigrateAsync();

        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        await auditDb.Database.MigrateAsync();

        var orgDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        await orgDb.Database.MigrateAsync();

        var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        await patientsDb.Database.MigrateAsync();

        var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        await schedulingDb.Database.MigrateAsync();

        var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await notificationsDb.Database.MigrateAsync();

        var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
        await clinicalDb.Database.MigrateAsync();

        var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        await pharmacyDb.Database.MigrateAsync();

        var diagnosticsDb = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
        await diagnosticsDb.Database.MigrateAsync();

        var inpatientDb = scope.ServiceProvider.GetRequiredService<InpatientDbContext>();
        await inpatientDb.Database.MigrateAsync();

        var orgSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var inpatientSeeder = scope.ServiceProvider.GetRequiredService<IInpatientDataSeeder>();
        await inpatientSeeder.SeedAsync();
    }
}
