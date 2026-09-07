using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using HospitalManagement.Contracts.ClinicalRecords;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class ClinicalAttachmentsIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G07")]
    public async Task DoctorCanUploadAndDownloadValidAttachmentAndRejectSpoofing()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var doctorClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);

        // 1. Upload valid PDF attachment with path traversal filename
        var pdfBytes = "%PDF-1.4\n1 0 obj\n<< /Title (Lab Sonucu) >>\nendobj\n%%EOF"u8.ToArray();
        var token = await doctorClient.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(token);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(encounterId.ToString()), "encounterId");
        form.Add(new StringContent(patientId.ToString()), "patientId");
        form.Add(new StringContent("LabReport"), "attachmentType");
        form.Add(new StringContent("Tam Kan Sayımı Raporu"), "description");

        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "../../../sensitive/report.pdf");

        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/clinical-records/attachments")
        {
            Content = form,
        };
        uploadReq.Headers.Add("X-HMS-CSRF", token.Token);

        var uploadResp = await doctorClient.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);

        var attachment = await uploadResp.Content.ReadFromJsonAsync<ClinicalAttachmentResponse>();
        Assert.NotNull(attachment);
        Assert.Equal("report.pdf", attachment.FileName); // Path traversal stripped!
        Assert.Equal("application/pdf", attachment.ContentType);
        Assert.Equal(pdfBytes.Length, attachment.ByteSize);

        var attachmentId = attachment.Id;

        // 2. Download attachment
        var downloadResp = await doctorClient.GetAsync($"/api/v1/clinical-records/attachments/{attachmentId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);
        Assert.Equal("application/pdf", downloadResp.Content.Headers.ContentType?.MediaType);
        var downloadedBytes = await downloadResp.Content.ReadAsByteArrayAsync();
        Assert.Equal(pdfBytes, downloadedBytes);

        // 3. Attempt to upload spoofed file (declared PDF but EXE payload)
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 };
        using var spoofedForm = new MultipartFormDataContent();
        spoofedForm.Add(new StringContent(encounterId.ToString()), "encounterId");
        spoofedForm.Add(new StringContent(patientId.ToString()), "patientId");
        spoofedForm.Add(new StringContent("LabReport"), "attachmentType");

        var spoofedFile = new ByteArrayContent(exeBytes);
        spoofedFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        spoofedForm.Add(spoofedFile, "file", "malicious.pdf");

        using var spoofedReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/clinical-records/attachments")
        {
            Content = spoofedForm,
        };
        spoofedReq.Headers.Add("X-HMS-CSRF", token.Token);

        var spoofedResp = await doctorClient.SendAsync(spoofedReq);
        Assert.Equal(HttpStatusCode.BadRequest, spoofedResp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F04-G07")]
    public async Task PatientCanViewAndDownloadOwnAttachmentsAndOtherPatientAccessIsForbidden()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAsync(application);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        // Doctor uploads attachment for Patient 109
        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var pdfBytes = "%PDF-1.4\nDemo Patient Lab Report\n%%EOF"u8.ToArray();

        var token = await doctorClient.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(token);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(encounterId.ToString()), "encounterId");
        form.Add(new StringContent(patientId.ToString()), "patientId");
        form.Add(new StringContent("LabReport"), "attachmentType");
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", "patient_lab.pdf");

        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/clinical-records/attachments")
        {
            Content = form,
        };
        uploadReq.Headers.Add("X-HMS-CSRF", token.Token);
        var uploadResp = await doctorClient.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);
        var attachment = await uploadResp.Content.ReadFromJsonAsync<ClinicalAttachmentResponse>();
        Assert.NotNull(attachment);

        // 1. Patient views and downloads own attachment -> 200 OK
        var patientClient = CreateSecureClient(application);
        await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");

        var listResp = await patientClient.GetAsync($"/api/v1/clinical-records/attachments/by-patient/{patientId}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

        var downloadResp = await patientClient.GetAsync($"/api/v1/clinical-records/attachments/{attachment.Id}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResp.StatusCode);

        // 2. Patient tries to access another patient's attachment list -> 403 Forbidden
        var otherPatientId = Guid.NewGuid();
        var forbiddenListResp = await patientClient.GetAsync($"/api/v1/clinical-records/attachments/by-patient/{otherPatientId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenListResp.StatusCode);
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

    private static async Task RunAllMigrationsAsync(ApiWebApplicationFactory application)
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
    }
}
