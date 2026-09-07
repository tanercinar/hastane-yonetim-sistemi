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
using HospitalManagement.Modules.Inpatient.Infrastructure;
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

public sealed class InpatientAdmissionIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G02")]
    public async Task InpatientAdmissionWorkflowEnforcesSingleActiveAdmissionBedAssignmentAndAuditLogging()
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

        // 1. Doctor logs in and requests admission
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // Fetch Cardiology Ward
        var wardsResponse = await doctorClient.GetAsync("/api/v1/inpatient/wards");
        Assert.Equal(HttpStatusCode.OK, wardsResponse.StatusCode);
        var wards = await wardsResponse.Content.ReadFromJsonAsync<List<WardResponse>>();
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");

        await using (var validationScope = application.Services.CreateAsyncScope())
        {
            var admissionService = validationScope.ServiceProvider.GetRequiredService<IInpatientAdmissionService>();
            var mismatchedDepartment = await admissionService.RequestAdmissionAsync(
                new CreateAdmissionDto(
                    patientId,
                    null,
                    Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    cardWard.Id,
                    doctorPersonId,
                    "DEMO bölüm ve servis eşleşme doğrulaması",
                    null,
                    null,
                    "Standard",
                    0,
                    IsolationType.None,
                    null,
                    null),
                doctorPersonId);
            Assert.Equal(InpatientOperationStatus.ValidationFailed, mismatchedDepartment.Status);
            Assert.Contains("DepartmentId", mismatchedDepartment.ValidationErrors.Keys);
        }

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Akut anterior MI ve dekompanse kalp yetmezliği izlemi.",
            DiagnosisCode = "I21.0",
            DiagnosisDescription = "Akut transmural anterior miyokart enfarktüsü",
            DietType = "LowSodium",
            FallRiskScore = 40,
            IsolationRequired = "None",
            EstimatedStayDays = 5,
        };

        var reqResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, reqResponse.StatusCode);

        var createdAdmission = await reqResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(createdAdmission);
        Assert.Equal("Requested", createdAdmission.Status);
        Assert.Equal(patientId, createdAdmission.PatientId);
        Assert.StartsWith("DEMO-ADM-", createdAdmission.AdmissionNumber, StringComparison.Ordinal);

        // 2. Doctor attempts to request a second concurrent admission for the SAME patient -> 409 Conflict
        var secondAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Mükerrer aktif yatış istem denemesi.",
            DietType = "Standard",
            FallRiskScore = 0,
            IsolationRequired = "None",
        };

        var conflictResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", secondAdmissionReq);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);

        // 3. Nurse logs in and accepts the admission
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var acceptResponse = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/accept", new AcceptAdmissionRequest { Notes = "Servis hemşiresi kabul etti." });
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var acceptedAdmission = await acceptResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(acceptedAdmission);
        Assert.Equal("Accepted", acceptedAdmission.Status);

        await using (var validationScope = application.Services.CreateAsyncScope())
        {
            var admissionService = validationScope.ServiceProvider.GetRequiredService<IInpatientAdmissionService>();
            var wrongWardBed = await admissionService.AdmitPatientAsync(
                createdAdmission.Id,
                new AdmitPatientDto(InpatientDataSeeder.Bed201AId),
                ClinicalTestData.DemoNursePersonId);
            Assert.Equal(InpatientOperationStatus.NotFound, wrongWardBed.Status);
        }

        // 4. Fetch available beds in cardiology ward and admit patient to bed
        var bedsResponse = await nurseClient.GetAsync($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        Assert.Equal(HttpStatusCode.OK, bedsResponse.StatusCode);
        var availableBeds = await bedsResponse.Content.ReadFromJsonAsync<List<BedResponse>>();
        Assert.NotNull(availableBeds);
        Assert.NotEmpty(availableBeds);
        var selectedBed = availableBeds[0];

        var admitResponse = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/admit", new AdmitPatientRequest { BedId = selectedBed.Id });
        Assert.Equal(HttpStatusCode.OK, admitResponse.StatusCode);

        var admittedAdmission = await admitResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(admittedAdmission);
        Assert.Equal("Admitted", admittedAdmission.Status);
        Assert.Equal(selectedBed.Id, admittedAdmission.AssignedBedId);

        // 5. Verify the bed is now Occupied
        var bedCheckResp = await nurseClient.GetAsync($"/api/v1/inpatient/beds/{selectedBed.Id}");
        Assert.Equal(HttpStatusCode.OK, bedCheckResp.StatusCode);
        var bedCheck = await bedCheckResp.Content.ReadFromJsonAsync<BedResponse>();
        Assert.NotNull(bedCheck);
        Assert.Equal("Occupied", bedCheck.Status);
        Assert.Equal(createdAdmission.Id, bedCheck.CurrentAdmissionId);
        Assert.Equal(patientId, bedCheck.CurrentPatientId);

        // 6. Update Care Details
        var invalidAttendingDoctorResponse = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/admissions/{createdAdmission.Id}/care-details",
            new UpdateCareDetailsRequest
            {
                AttendingDoctorId = Guid.NewGuid(),
                DietType = "Standard",
                FallRiskScore = 20,
                IsolationRequired = "None",
            });
        Assert.Equal(HttpStatusCode.Forbidden, invalidAttendingDoctorResponse.StatusCode);

        var careReq = new UpdateCareDetailsRequest
        {
            AttendingDoctorId = doctorPersonId,
            DietType = "Diabetic",
            FallRiskScore = 60,
            IsolationRequired = "Contact",
        };
        var careResponse = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/care-details", careReq);
        Assert.Equal(HttpStatusCode.OK, careResponse.StatusCode);

        var updatedAdmission = await careResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(updatedAdmission);
        Assert.Equal("Diabetic", updatedAdmission.DietType);
        Assert.Equal(60, updatedAdmission.FallRiskScore);
        Assert.Equal("Contact", updatedAdmission.IsolationRequired);

        // 7. Verify AuditLogs
        await using var scope = application.Services.CreateAsyncScope();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditLogs = await auditDb.AuditLogs
            .Where(a => a.TargetResourceId == createdAdmission.Id.ToString())
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.AdmissionRequest");
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.AdmissionAccept");
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.AdmissionAdmit");
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.AdmissionCareDetailsUpdate");
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password) =>
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
