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

public sealed class Phase11ReportingGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F11-KAPI")]
    public async Task Phase11GateFullReportingPipelineAndSecurityBoundariesPass()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var cardioDeptId = Guid.Parse("30000000-0000-0000-0000-000000000003");
        var doctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        // 1. Source Event Projections
        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();

            // Outpatient appointments
            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                today,
                cardioDeptId,
                "Kardiyoloji",
                doctorId,
                "Dr. Test Doctor",
                "None",
                "Scheduled",
                now));

            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                today,
                cardioDeptId,
                "Kardiyoloji",
                doctorId,
                "Dr. Test Doctor",
                "Scheduled",
                "CheckedIn",
                now.AddMinutes(10)));

            // Diagnostic test order: Ordered then Finalized
            await engine.ProjectDiagnosticEventAsync(new DiagnosticProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                today,
                "Biyokimya",
                "None",
                "Ordered",
                false,
                null,
                now));

            await engine.ProjectDiagnosticEventAsync(new DiagnosticProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                today,
                "Biyokimya",
                "Ordered",
                "Finalized",
                false,
                45.0,
                now.AddMinutes(45)));

            // Bed occupancy
            await engine.ProjectBedOccupancyEventAsync(new BedOccupancyProjectedEvent(
                Guid.NewGuid(),
                today,
                cardioDeptId,
                "Kardiyoloji",
                "Kardiyoloji Servisi",
                20,
                15,
                2,
                now));

            // Pharmacy dispensing
            await engine.ProjectPharmacyEventAsync(new PharmacyProjectedEvent(
                Guid.NewGuid(),
                today,
                "DispenseCompleted",
                PendingDelta: 5,
                DispensedDelta: 3,
                LowStockCount: 2,
                NearExpiryCount: 1,
                OccurredAtUtc: now));
        }

        // 2. Security Gate: Anonymous access challenged
        var anonClient = CreateSecureClient(application);
        var anonResp = await anonClient.GetAsync($"/api/v1/reporting/dashboards/outpatient/summary?date={today:yyyy-MM-dd}");
        Assert.True(anonResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect);

        // 3. Operational Dashboards: Authorized doctor query
        var doctorClient = CreateSecureClient(application);
        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // Outpatient summary verified
        var outpatientResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/outpatient/summary?date={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, outpatientResp.StatusCode);
        var outpatientSummary = await outpatientResp.Content.ReadFromJsonAsync<OutpatientDashboardSummaryResponse>();
        Assert.NotNull(outpatientSummary);
        Assert.True(outpatientSummary.TotalAppointments >= 1);

        // Diagnostics summary verified
        var diagResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/diagnostics/summary?date={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, diagResp.StatusCode);
        var diagSummary = await diagResp.Content.ReadFromJsonAsync<DiagnosticDashboardSummaryResponse>();
        Assert.NotNull(diagSummary);
        Assert.True(diagSummary.TotalOrders >= 1);

        // Inpatient operations summary verified
        var inpatientResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/inpatient-operations/summary?date={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, inpatientResp.StatusCode);
        var inpatientSummary = await inpatientResp.Content.ReadFromJsonAsync<InpatientOperationsSummaryResponse>();
        Assert.NotNull(inpatientSummary);
        Assert.True(inpatientSummary.OccupiedBeds >= 15);

        // Pharmacy summary verified
        var pharmResp = await doctorClient.GetAsync($"/api/v1/reporting/dashboards/pharmacy/summary?date={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, pharmResp.StatusCode);
        var pharmSummary = await pharmResp.Content.ReadFromJsonAsync<PharmacyDashboardSummaryResponse>();
        Assert.NotNull(pharmSummary);
        Assert.True(pharmSummary.TotalPrescriptions >= 5);

        // 4. Projection Lag Gate
        var lagResp = await doctorClient.GetAsync("/api/v1/reporting/projections/lag");
        Assert.Equal(HttpStatusCode.OK, lagResp.StatusCode);
        var lags = await lagResp.Content.ReadFromJsonAsync<List<ProjectionLagInfo>>();
        Assert.NotNull(lags);
        Assert.True(lags.Count >= 4);
        Assert.All(lags, lag =>
        {
            Assert.True(lag.IsHealthy);
            Assert.True(lag.LagSeconds < 60);
        });

        // 5. Permission & Export Boundary Gate
        // Doctor cannot export CSV (403 Forbidden)
        var docExportResp = await doctorClient.GetAsync($"/api/v1/reporting/exports/csv?reportType=outpatient-metrics&startDate={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.Forbidden, docExportResp.StatusCode);

        // CMO has report.operations.export -> 200 OK
        var cmoClient = CreateSecureClient(application);
        var cmoLogin = await LoginAsync(cmoClient, "DEMO-chief@hospital.invalid", "DEMO-Chief-Pass!1");
        Assert.Equal(HttpStatusCode.OK, cmoLogin.StatusCode);

        var cmoExportResp = await cmoClient.GetAsync($"/api/v1/reporting/exports/csv?reportType=outpatient-metrics&startDate={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, cmoExportResp.StatusCode);
        Assert.Equal("text/csv", cmoExportResp.Content.Headers.ContentType?.MediaType);

        // 6. CSV Formula Injection Attack Mitigation
        // Inject an attack payload and verify sanitization
        using (var scope = application.Services.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IReportingProjectionEngine>();
            var attackDeptId = Guid.NewGuid();
            var attackDocId = Guid.NewGuid();

            await engine.ProjectAppointmentEventAsync(new AppointmentProjectedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                today,
                attackDeptId,
                "=cmd|'/C calc'!A0",
                attackDocId,
                "@SUM(1,2)",
                "None",
                "Scheduled",
                now));
        }

        var attackExportResp = await cmoClient.GetAsync($"/api/v1/reporting/exports/csv?reportType=outpatient-metrics&startDate={today:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, attackExportResp.StatusCode);
        var csvContent = await attackExportResp.Content.ReadAsStringAsync();

        // Must be prepended with single quote to neutralize spreadsheet execution
        Assert.Contains("'=cmd|'/C calc'!A0", csvContent);
        Assert.Contains("\"'@SUM(1,2)\"", csvContent);
        Assert.DoesNotContain(",=cmd", csvContent);
        Assert.DoesNotContain(",@SUM", csvContent);
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
