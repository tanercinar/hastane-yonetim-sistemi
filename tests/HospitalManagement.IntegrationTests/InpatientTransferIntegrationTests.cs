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
using HospitalManagement.Modules.Organization.Domain;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class InpatientTransferIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F07-G03")]
    public async Task InpatientTransferWorkflowUpdatesBedsAtomicallyAndMaintainsMovementHistory()
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

        // 1. Doctor requests admission
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var wardsResponse = await doctorClient.GetAsync("/api/v1/inpatient/wards");
        Assert.Equal(HttpStatusCode.OK, wardsResponse.StatusCode);
        var wards = await wardsResponse.Content.ReadFromJsonAsync<List<WardResponse>>();
        Assert.NotNull(wards);
        var cardWard = wards.Single(w => w.Code == "DEMO-WRD-CARD");
        Assert.DoesNotContain(wards, w => w.Code == "DEMO-WRD-INTMED");

        var createAdmissionReq = new CreateAdmissionRequest
        {
            PatientId = patientId,
            DepartmentId = cardDeptId,
            AdmittingWardId = cardWard.Id,
            AttendingDoctorId = doctorPersonId,
            AdmissionReason = "Transfer testi başlangıç yatışı.",
            DietType = "Standard",
            FallRiskScore = 20,
            IsolationRequired = "None",
        };

        var reqResponse = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/inpatient/admissions", createAdmissionReq);
        Assert.Equal(HttpStatusCode.Created, reqResponse.StatusCode);
        var createdAdmission = await reqResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
        Assert.NotNull(createdAdmission);

        // 2. Nurse accepts and admits patient to Bed A in Cardiology
        var nurseClient = CreateSecureClient(application);
        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var nurseWards = await nurseClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
        Assert.NotNull(nurseWards);
        Assert.DoesNotContain(nurseWards, ward => ward.Code == "DEMO-WRD-INTMED");
        var transferDestinations = await nurseClient.GetFromJsonAsync<List<WardResponse>>(
            "/api/v1/inpatient/wards/transfer-destinations");
        Assert.NotNull(transferDestinations);
        var imedWard = transferDestinations.Single(w => w.Code == "DEMO-WRD-INTMED");

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/accept", new AcceptAdmissionRequest());

        var cardBedsResponse = await nurseClient.GetAsync($"/api/v1/inpatient/beds?wardId={cardWard.Id}&status=Available");
        var cardAvailableBeds = await cardBedsResponse.Content.ReadFromJsonAsync<List<BedResponse>>();
        Assert.NotNull(cardAvailableBeds);
        Assert.NotEmpty(cardAvailableBeds);
        var bedA = cardAvailableBeds[0];

        await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/admissions/{createdAdmission.Id}/admit", new AdmitPatientRequest { BedId = bedA.Id });

        var bedACheck = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedA.Id}");
        Assert.NotNull(bedACheck);
        Assert.Equal("Occupied", bedACheck.Status);

        // 3. Nurse requests transfer to Internal Medicine ward
        var transferReq = new CreateTransferRequest
        {
            AdmissionId = createdAdmission.Id,
            TargetWardId = imedWard.Id,
            TransferReason = "Dahiliye takibine devir.",
            ClinicalNotes = "Kardiyak enzimler geriledi, dahiliye servisine transfer uygundur.",
        };

        var transferPostResp = await PostWithAntiforgeryAsync(nurseClient, "/api/v1/inpatient/transfers", transferReq);
        Assert.Equal(HttpStatusCode.Created, transferPostResp.StatusCode);
        var transfer = await transferPostResp.Content.ReadFromJsonAsync<TransferResponse>();
        Assert.NotNull(transfer);
        Assert.Equal("Requested", transfer.Status);
        Assert.Equal(cardWard.Id, transfer.SourceWardId);
        Assert.Equal(bedA.Id, transfer.SourceBedId);
        Assert.Equal(imedWard.Id, transfer.TargetWardId);

        // 4. Target-ward nurse can list and accept the incoming transfer without source-ward access.
        await MoveDemoNurseToDepartmentAsync(
            application,
            Guid.Parse("30000000-0000-0000-0000-000000000002"));
        var sourceAdmissionResponse = await nurseClient.GetAsync(
            $"/api/v1/inpatient/admissions/{createdAdmission.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, sourceAdmissionResponse.StatusCode);
        var incomingTransfers = await nurseClient.GetFromJsonAsync<List<TransferSummaryResponse>>(
            "/api/v1/inpatient/transfers?status=Requested");
        Assert.NotNull(incomingTransfers);
        Assert.Contains(incomingTransfers, item => item.Id == transfer.Id);

        var acceptTransferResp = await PostWithAntiforgeryAsync(nurseClient, $"/api/v1/inpatient/transfers/{transfer.Id}/accept", new AcceptTransferRequest());
        Assert.Equal(HttpStatusCode.OK, acceptTransferResp.StatusCode);

        // 5. Nurse completes transfer by picking Bed B in Internal Medicine
        var imedBedsResponse = await nurseClient.GetAsync($"/api/v1/inpatient/beds?wardId={imedWard.Id}&status=Available");
        var imedAvailableBeds = await imedBedsResponse.Content.ReadFromJsonAsync<List<BedResponse>>();
        Assert.NotNull(imedAvailableBeds);
        Assert.NotEmpty(imedAvailableBeds);
        var bedB = imedAvailableBeds[0];

        var completeTransferResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/inpatient/transfers/{transfer.Id}/complete",
            new CompleteTransferRequest { TargetBedId = bedB.Id });
        Assert.Equal(HttpStatusCode.OK, completeTransferResp.StatusCode);

        // 6. Assert Atomic Bed & Admission State Transition
        // Bed A -> Cleaning (Released)
        var sourceBedAfterTransferResponse = await nurseClient.GetAsync($"/api/v1/inpatient/beds/{bedA.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, sourceBedAfterTransferResponse.StatusCode);

        // Bed B -> Occupied (Assigned)
        var bedBAfter = await nurseClient.GetFromJsonAsync<BedResponse>($"/api/v1/inpatient/beds/{bedB.Id}");
        Assert.NotNull(bedBAfter);
        Assert.Equal("Occupied", bedBAfter.Status);
        Assert.Equal(createdAdmission.Id, bedBAfter.CurrentAdmissionId);
        Assert.Equal(patientId, bedBAfter.CurrentPatientId);

        // Admission -> Ward updated to IMED, Bed updated to Bed B
        var admissionAfter = await nurseClient.GetFromJsonAsync<AdmissionResponse>($"/api/v1/inpatient/admissions/{createdAdmission.Id}");
        Assert.NotNull(admissionAfter);
        Assert.Equal(imedWard.Id, admissionAfter.AdmittingWardId);
        Assert.Equal(bedB.Id, admissionAfter.AssignedBedId);
        Assert.Equal("Admitted", admissionAfter.Status);

        // Transfer -> Status is Completed
        var transferAfter = await nurseClient.GetFromJsonAsync<TransferResponse>($"/api/v1/inpatient/transfers/{transfer.Id}");
        Assert.NotNull(transferAfter);
        Assert.Equal("Completed", transferAfter.Status);
        Assert.Equal(bedB.Id, transferAfter.TargetBedId);

        // 7. Verify Audit Logs
        await using var scope = application.Services.CreateAsyncScope();
        var inpatientDb = scope.ServiceProvider.GetRequiredService<InpatientDbContext>();
        var sourceBed = await inpatientDb.Beds.AsNoTracking().SingleAsync(item => item.Id == bedA.Id);
        Assert.Equal(BedStatus.Cleaning, sourceBed.Status);
        Assert.Null(sourceBed.CurrentAdmissionId);
        Assert.Null(sourceBed.CurrentPatientId);

        var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var auditLogs = await auditDb.AuditLogs
            .Where(a => a.TargetResourceId == transfer.Id.ToString())
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.TransferRequest");
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.TransferAccept");
        Assert.Contains(auditLogs, a => a.Action == "Inpatient.TransferComplete");
    }

    private static HttpClient CreateSecureClient(ApiWebApplicationFactory application) =>
        application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

    private static async Task MoveDemoNurseToDepartmentAsync(
        ApiWebApplicationFactory application,
        Guid departmentId)
    {
        await using var scope = application.Services.CreateAsyncScope();
        var organizationDb = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var nurseProfileId = await organizationDb.StaffProfiles
            .Where(profile => profile.PersonId == ClinicalTestData.DemoNursePersonId)
            .Select(profile => profile.Id)
            .SingleAsync();
        var transitionUtc = DateTime.UtcNow;
        var activeAssignments = await organizationDb.StaffDepartmentAssignments
            .Where(assignment => assignment.StaffProfileId == nurseProfileId
                && assignment.EndsAtUtc == null)
            .ToListAsync();
        foreach (var assignment in activeAssignments)
        {
            assignment.End(transitionUtc);
        }

        organizationDb.StaffDepartmentAssignments.Add(
            StaffDepartmentAssignment.Create(
                Guid.NewGuid(),
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                nurseProfileId,
                departmentId,
                isPrimary: false,
                startsAtUtc: transitionUtc));
        await organizationDb.SaveChangesAsync();
    }

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
