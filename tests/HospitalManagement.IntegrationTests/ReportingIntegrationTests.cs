using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Reporting;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.Emergency.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;
using HospitalManagement.Modules.Interoperability.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Reporting.Application;
using HospitalManagement.Modules.Reporting.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class ReportingIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G01")]
    public async Task RebuildProjectionsEndpointRebuildsAndReturnsCheckpoints()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Rebuild projections
        var rebuildReq = new RebuildProjectionsRequest();
        var rebuildResp = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/reporting/projections/rebuild", rebuildReq);
        Assert.Equal(HttpStatusCode.OK, rebuildResp.StatusCode);

        var rebuildSummary = await rebuildResp.Content.ReadFromJsonAsync<RebuildProjectionsResponse>();
        Assert.NotNull(rebuildSummary);
        Assert.True(rebuildSummary.Success);
        Assert.Equal(4, rebuildSummary.TotalProjectionsRebuilt);

        // 2. Query checkpoints
        var checkpointsResp = await doctorClient.GetAsync("/api/v1/reporting/projections/checkpoints");
        Assert.Equal(HttpStatusCode.OK, checkpointsResp.StatusCode);

        var checkpoints = await checkpointsResp.Content.ReadFromJsonAsync<List<ProjectionCheckpointResponse>>();
        Assert.NotNull(checkpoints);
        Assert.Equal(4, checkpoints.Count);
        Assert.All(checkpoints, c => Assert.Equal("Active", c.Status));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G01")]
    public async Task ProjectionEngineAndMetricsApiWorkWithPostgreSqlIdempotently()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var eventId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        // Project event via scoped engine
        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            var projected = await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                eventId,
                Guid.NewGuid(),
                date,
                deptId,
                "Kardiyoloji",
                null,
                null,
                "None",
                "Scheduled",
                now));
            Assert.True(projected);

            // Re-project identical event to test database idempotency
            var duplicateProjected = await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                eventId,
                Guid.NewGuid(),
                date,
                deptId,
                "Kardiyoloji",
                null,
                null,
                "None",
                "Scheduled",
                now));
            Assert.False(duplicateProjected);

            var sourceEventCount = await scope.ServiceProvider
                .GetRequiredService<ReportingDbContext>()
                .ProjectionSourceEvents
                .CountAsync();
            Assert.Equal(1, sourceEventCount);

            var rebuilder = scope.ServiceProvider.GetRequiredService<IProjectionRebuilder>();
            var rebuild = await rebuilder.RebuildProjectionAsync("DailyOutpatient");
            Assert.True(rebuild.Success);
        }

        // Query metrics via REST API
        var resp = await doctorClient.GetAsync($"/api/v1/reporting/metrics/outpatient?startDate={date:yyyy-MM-dd}&departmentId={deptId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var metrics = await resp.Content.ReadFromJsonAsync<List<DailyOutpatientMetricResponse>>();
        Assert.NotNull(metrics);
        Assert.Single(metrics);
        Assert.Equal(1, metrics[0].TotalAppointments);
        Assert.Equal(1, metrics[0].ScheduledCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G01")]
    public async Task ReportingEndpointsWithoutPermissionReturnForbidden()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientClient = CreateSecureClient(application);
        var patLogin = await LoginAsync(patientClient, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        Assert.Equal(HttpStatusCode.OK, patLogin.StatusCode);

        var resp = await patientClient.GetAsync("/api/v1/reporting/metrics/outpatient");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G02")]
    public async Task OutpatientDashboardEndpointsReturnSummaryAndBreakdowns()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var date = new DateOnly(2026, 9, 4);
        var deptId = Guid.NewGuid();
        var docId = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var now = DateTime.UtcNow;

        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                date,
                deptId,
                "Kardiyoloji",
                docId,
                "Dr. Ali",
                "None",
                "Scheduled",
                now));

            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                date,
                deptId,
                "Kardiyoloji",
                docId,
                "Dr. Ali",
                "Scheduled",
                "CheckedIn",
                now));
        }

        // Summary endpoint
        var summaryResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/outpatient/summary?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var summary = await summaryResp.Content.ReadFromJsonAsync<OutpatientDashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalAppointments);
        Assert.Equal(1, summary.CheckedInCount);
        Assert.Equal(1, summary.WaitingQueueCount);

        // Departments breakdown
        var deptResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/outpatient/departments?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, deptResp.StatusCode);

        var depts = await deptResp.Content.ReadFromJsonAsync<List<OutpatientDepartmentMetricResponse>>();
        Assert.NotNull(depts);
        Assert.Single(depts);
        Assert.Equal("Kardiyoloji", depts[0].DepartmentName);

        // Doctors breakdown
        var docResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/outpatient/doctors?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, docResp.StatusCode);

        var docs = await docResp.Content.ReadFromJsonAsync<List<OutpatientDoctorMetricResponse>>();
        Assert.NotNull(docs);
        Assert.Single(docs);
        Assert.Equal("Dr. Ali", docs[0].DoctorName);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G03")]
    public async Task DiagnosticDashboardEndpointsReturnSummaryAndModalities()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            await engine.ProjectDiagnosticEventAsync(new DiagnosticProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                date,
                "Biyokimya",
                "None",
                "Ordered",
                true,
                null,
                now));

            await engine.ProjectDiagnosticEventAsync(new DiagnosticProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                date,
                "Biyokimya",
                "Ordered",
                "Finalized",
                false,
                42.0,
                now));
        }

        // Summary endpoint
        var summaryResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/diagnostics/summary?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var summary = await summaryResp.Content.ReadFromJsonAsync<DiagnosticDashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalOrders);
        Assert.Equal(1, summary.FinalizedCount);
        Assert.Equal(1, summary.CriticalCount);
        Assert.Equal(42.0, summary.AvgTurnaroundMinutes);

        // Modalities endpoint
        var modResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/diagnostics/modalities?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, modResp.StatusCode);

        var modalities = await modResp.Content.ReadFromJsonAsync<List<DiagnosticModalityMetricResponse>>();
        Assert.NotNull(modalities);
        Assert.Single(modalities);
        Assert.Equal("Biyokimya", modalities[0].ModalityOrSection);

        // Critical alerts endpoint
        var alertResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/diagnostics/critical-alerts?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, alertResp.StatusCode);

        var alerts = await alertResp.Content.ReadFromJsonAsync<List<DiagnosticCriticalAlertMetricResponse>>();
        Assert.NotNull(alerts);
        Assert.Single(alerts);
        Assert.Equal(1, alerts[0].CriticalCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G04")]
    public async Task InpatientOperationsDashboardEndpointsReturnSummaryAndWards()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var date = new DateOnly(2026, 9, 4);
        var deptId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            await engine.ProjectBedOccupancyEventAsync(new BedOccupancyProjectedEvent(
                Guid.NewGuid(),
                date,
                deptId,
                "Genel Cerrahi",
                "Servis",
                20,
                14,
                2,
                now));
        }

        // Summary endpoint
        var summaryResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/inpatient-operations/summary?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var summary = await summaryResp.Content.ReadFromJsonAsync<InpatientOperationsSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(20, summary.TotalBeds);
        Assert.Equal(14, summary.OccupiedBeds);
        Assert.Equal(6, summary.AvailableBeds);
        Assert.Equal(70.0, summary.OverallOccupancyRate);
        Assert.Equal(2, summary.PendingTransfers);

        // Wards endpoint
        var wardResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/inpatient-operations/wards?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, wardResp.StatusCode);

        var wards = await wardResp.Content.ReadFromJsonAsync<List<WardOccupancyDetailResponse>>();
        Assert.NotNull(wards);
        Assert.Single(wards);
        Assert.Equal("Genel Cerrahi", wards[0].DepartmentName);
        Assert.Equal(70.0, wards[0].OccupancyRatePercentage);

        // Emergency triage endpoint
        var triageResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/inpatient-operations/emergency-triage?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, triageResp.StatusCode);

        var triages = await triageResp.Content.ReadFromJsonAsync<List<EmergencyTriageQueueMetricResponse>>();
        Assert.NotNull(triages);
        Assert.Equal(3, triages.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G05")]
    public async Task PharmacyDashboardEndpointsReturnSummaryAndAlerts()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var pharmacistClient = CreateSecureClient(application);
        var pharmLogin = await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        Assert.Equal(HttpStatusCode.OK, pharmLogin.StatusCode);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            await engine.ProjectPharmacyEventAsync(new PharmacyProjectedEvent(
                Guid.NewGuid(),
                date,
                "DispenseRecorded",
                2,
                1,
                1,
                1,
                now));
        }

        // Summary endpoint
        var summaryResp = await pharmacistClient.GetAsync($"/api/v1/reporting/dashboards/pharmacy/summary?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, summaryResp.StatusCode);

        var summary = await summaryResp.Content.ReadFromJsonAsync<PharmacyDashboardSummaryResponse>();
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalPrescriptions);
        Assert.Equal(2, summary.PendingDispenseCount);
        Assert.Equal(1, summary.DispensedCount);
        Assert.Equal(1, summary.LowStockItemCount);
        Assert.Equal(1, summary.NearExpiryLotCount);

        // Stock alerts endpoint
        var alertsResp = await pharmacistClient.GetAsync($"/api/v1/reporting/dashboards/pharmacy/stock-alerts?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, alertsResp.StatusCode);

        var alerts = await alertsResp.Content.ReadFromJsonAsync<List<PharmacyStockAlertMetricResponse>>();
        Assert.NotNull(alerts);
        Assert.NotEmpty(alerts);
        Assert.Contains(alerts, a => a.AlertType.Contains("Kritik"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G06")]
    public async Task SecureExportEndpointEnforcesPermissionsAndSanitization()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var date = new DateOnly(2026, 9, 4);
        var now = DateTime.UtcNow;

        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                date,
                Guid.NewGuid(),
                "=Kardiyoloji",
                Guid.NewGuid(),
                "+Dr. Ahmet",
                "None",
                "Scheduled",
                now));
        }

        // 1. Doctor has report.operations.view but NOT report.operations.export -> Expect 403 Forbidden
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var forbiddenExportResp = await doctorClient.GetAsync($"/api/v1/reporting/exports/csv?reportType=outpatient-metrics&startDate={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenExportResp.StatusCode);

        // 2. CMO has report.operations.export -> Expect 200 OK with sanitized CSV content
        var cmoClient = CreateSecureClient(application);
        var cmoLogin = await LoginAsync(cmoClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1");
        Assert.Equal(HttpStatusCode.OK, cmoLogin.StatusCode);

        var cmoExportResp = await cmoClient.GetAsync($"/api/v1/reporting/exports/csv?reportType=outpatient-metrics&startDate={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, cmoExportResp.StatusCode);
        Assert.Equal("text/csv", cmoExportResp.Content.Headers.ContentType?.MediaType);

        var csvBody = await cmoExportResp.Content.ReadAsStringAsync();
        // Malicious formula prefixes sanitized with leading quote
        Assert.Contains("'=Kardiyoloji", csvBody);
        Assert.Contains("'+Dr. Ahmet", csvBody);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-G07")]
    public async Task ProjectionLagEndpointReturnsCheckpointsAndLag()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Anonymous access is challenged/unauthorized
        var anonClient = CreateSecureClient(application);
        var anonResp = await anonClient.GetAsync("/api/v1/reporting/projections/lag");
        Assert.True(anonResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect);

        // 2. Doctor has report.operations.view -> Expect 200 OK
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        var docLagResp = await doctorClient.GetAsync("/api/v1/reporting/projections/lag");
        Assert.Equal(HttpStatusCode.OK, docLagResp.StatusCode);

        var lags = await docLagResp.Content.ReadFromJsonAsync<List<ProjectionLagInfo>>();
        Assert.NotNull(lags);
        Assert.All(lags, lag =>
        {
            Assert.False(string.IsNullOrWhiteSpace(lag.ProjectionName));
            Assert.True(lag.LagSeconds >= 0);
            Assert.True(lag.IsHealthy);
        });
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

        var interopDb = sp.GetRequiredService<InteroperabilityDbContext>();
        await interopDb.Database.MigrateAsync();

        var repDb = sp.GetRequiredService<ReportingDbContext>();
        await repDb.Database.MigrateAsync();

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();
    }
}
