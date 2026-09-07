using System.Net;
using System.Net.Http.Json;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Contracts.Identity;
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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HospitalManagement.IntegrationTests;

public sealed class EmergencyEncounterFlowIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F08-G03")]
    public async Task EmergencyEncounterFullClinicalFlowOrdersConsultationAndDispositionSucceeds()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var doctorPersonId = Guid.Parse("00000000-0000-0000-0000-000000000102");

        var nurseClient = CreateSecureClient(application);
        var doctorClient = CreateSecureClient(application);

        var nurseLogin = await LoginAsync(nurseClient, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
        Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

        var docLogin = await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        Assert.Equal(HttpStatusCode.OK, docLogin.StatusCode);

        // 1. Nurse creates emergency admission
        var createAdmissionResp = await PostWithAntiforgeryAsync(
            nurseClient,
            "/api/v1/emergency/admissions",
            new CreateEmergencyAdmissionRequest
            {
                PatientId = patientId,
                ArrivalType = "Ambulance",
                ChiefComplaint = "Akut retrosternal göğüs ağrısı",
                AdmissionNotes = "112 ekipleri tarafından getirildi",
            });

        Assert.Equal(HttpStatusCode.Created, createAdmissionResp.StatusCode);
        var admission = await createAdmissionResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(admission);

        // 2. Nurse records triage (Red 1)
        var triageResp = await PostWithAntiforgeryAsync(
            nurseClient,
            $"/api/v1/emergency/admissions/{admission.Id}/triage",
            new RecordTriageRequest
            {
                TriageLevel = "Red1Resuscitation",
                TriageCategoryReason = "Akut EKG ST elevasyonu ve şiddetli göğüs ağrısı",
                SystolicBp = 150,
                DiastolicBp = 95,
                HeartRate = 105,
                BodyTemperatureCelsius = 36.7m,
                RespiratoryRate = 20,
                OxygenSaturationPercent = 96,
                PainScale = 9,
                Consciousness = "Açık, ajite",
                ClinicalNotes = "Resüsitasyon alanına alındı",
            });

        Assert.Equal(HttpStatusCode.OK, triageResp.StatusCode);

        // 3. Doctor assigns self & zone
        var assignResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/admissions/{admission.Id}/assign-doctor",
            new AssignEmergencyDoctorRequest
            {
                DoctorId = doctorPersonId,
                BedOrZone = "Resüsitasyon 1",
            });
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        // 4. Doctor creates STAT orders
        var tropOrderResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/emergency/orders",
            new CreateEmergencyOrderRequest(
                admissionId: admission.Id,
                orderType: "Laboratory",
                orderCatalogCode: "LAB-TROP",
                orderCatalogName: "Kardiyak Troponin I",
                priority: "Stat",
                clinicalInstructions: "Acil bakılsın"));
        Assert.Equal(HttpStatusCode.Created, tropOrderResp.StatusCode);
        var tropOrder = await tropOrderResp.Content.ReadFromJsonAsync<EmergencyOrderResponse>();
        Assert.NotNull(tropOrder);

        var ekgOrderResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/emergency/orders",
            new CreateEmergencyOrderRequest(
                admissionId: admission.Id,
                orderType: "Radiology",
                orderCatalogCode: "RAD-EKG",
                orderCatalogName: "12 Derivasyonlu EKG",
                priority: "Stat",
                clinicalInstructions: "Seri EKG çekimi"));
        Assert.Equal(HttpStatusCode.Created, ekgOrderResp.StatusCode);
        var ekgOrder = await ekgOrderResp.Content.ReadFromJsonAsync<EmergencyOrderResponse>();
        Assert.NotNull(ekgOrder);

        // 5. Complete Troponin & Cancel EKG
        var completeOrderResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/orders/{tropOrder.Id}/complete",
            new CompleteEmergencyOrderRequest("Troponin I: 4.8 ng/mL (Pozitif)"));
        Assert.Equal(HttpStatusCode.OK, completeOrderResp.StatusCode);

        var cancelOrderResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/orders/{ekgOrder.Id}/cancel",
            new CancelEmergencyOrderRequest("Mükerrer istem"));
        Assert.Equal(HttpStatusCode.OK, cancelOrderResp.StatusCode);

        // Verify orders via GET
        var ordersResp = await doctorClient.GetFromJsonAsync<List<EmergencyOrderResponse>>(
            $"/api/v1/emergency/orders/by-admission/{admission.Id}");
        Assert.NotNull(ordersResp);
        Assert.Equal(2, ordersResp.Count);
        Assert.Contains(ordersResp, o => o.Status == "Completed" && o.ResultSummary == "Troponin I: 4.8 ng/mL (Pozitif)");
        Assert.Contains(ordersResp, o => o.Status == "Cancelled" && o.CancellationReason == "Mükerrer istem");

        // 6. Doctor requests Cardiology Consultation
        var consultReqResp = await PostWithAntiforgeryAsync(
            doctorClient,
            "/api/v1/emergency/consultations",
            new RequestEmergencyConsultationRequest(
                admissionId: admission.Id,
                departmentId: Guid.NewGuid(),
                departmentName: "Kardiyoloji",
                urgency: "Immediate15Min",
                clinicalReason: "Akut inferior STEMI şüphesi, acil anjiyo / KYBÜ"));
        Assert.Equal(HttpStatusCode.Created, consultReqResp.StatusCode);
        var consult = await consultReqResp.Content.ReadFromJsonAsync<EmergencyConsultationResponse>();
        Assert.NotNull(consult);

        // 7. Consultant accepts and responds
        var acceptResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/consultations/{consult.Id}/accept",
            new
            {
            });
        Assert.Equal(HttpStatusCode.OK, acceptResp.StatusCode);

        var respondResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/consultations/{consult.Id}/respond",
            new RespondEmergencyConsultationRequest("Acil primer perkütan koroner girişim için anjiyografi laboratuvarı hazırlandı."));
        Assert.Equal(HttpStatusCode.OK, respondResp.StatusCode);

        // Verify consultation via GET
        var consultsResp = await doctorClient.GetFromJsonAsync<List<EmergencyConsultationResponse>>(
            $"/api/v1/emergency/consultations/by-admission/{admission.Id}");
        Assert.NotNull(consultsResp);
        Assert.Single(consultsResp);
        Assert.Equal("Completed", consultsResp[0].Status);
        Assert.Equal("Acil primer perkütan koroner girişim için anjiyografi laboratuvarı hazırlandı.", consultsResp[0].ConsultationResponseNotes);

        // 8. Record Disposition: AdmitToIcu
        var dispResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/emergency/admissions/{admission.Id}/disposition",
            new RecordEmergencyDispositionRequest(
                dispositionType: "AdmitToIcu",
                targetWardOrIcuId: Guid.NewGuid(),
                targetDepartmentName: "Koroner Yoğun Bakım Ünitesi",
                dispositionSummaryNotes: "Akut inferior STEMI tanısı ile acil anjiyo ve sonrasında KYBÜ yatışı kararlaştırıldı.",
                followUpInstructions: "DMAH, ASA, Tikagrelor yüklendi; monitörizasyona devam."));

        Assert.Equal(HttpStatusCode.OK, dispResp.StatusCode);
        var finalAdmission = await dispResp.Content.ReadFromJsonAsync<EmergencyAdmissionResponse>();
        Assert.NotNull(finalAdmission);
        Assert.Equal("AdmittedToIcu", finalAdmission.Status);
        Assert.NotNull(finalAdmission.CompletedAtUtc);
        Assert.Equal("Akut inferior STEMI tanısı ile acil anjiyo ve sonrasında KYBÜ yatışı kararlaştırıldı.", finalAdmission.DischargeOrDispositionNotes);
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

        var idSeeder = sp.GetRequiredService<IIdentityDataSeeder>();
        await idSeeder.SeedAsync();

        var orgSeeder = sp.GetRequiredService<IOrganizationDataSeeder>();
        await orgSeeder.SeedAsync();

        var inpSeeder = sp.GetRequiredService<IInpatientDataSeeder>();
        await inpSeeder.SeedAsync();

        var emgSeeder = sp.GetRequiredService<IEmergencyDataSeeder>();
        await emgSeeder.SeedAsync();
    }
}
