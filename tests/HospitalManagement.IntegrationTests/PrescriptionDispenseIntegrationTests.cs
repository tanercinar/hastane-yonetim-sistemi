using System.Net;
using System.Net.Http.Json;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Pharmacy;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class PrescriptionDispenseIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F05-G07")]
    public async Task PharmacistCanPerformPartialAndFullDispenseWithStockDeductionAndAudit()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        await RunAllMigrationsAndSeedAsync(application);

        // 1. Doctor creates and signs a prescription with 2 boxes of Amoxicillin
        var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

        var patientId = Guid.Parse("00000000-0000-0000-0000-000000000109");
        var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");

        Guid amoxId;
        using (var scope = application.Services.CreateScope())
        {
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var amox = await pharmacyDb.MedicationCatalogItems.FirstAsync(m => m.Code == "DEMO-MED-AMX500");
            amoxId = amox.Id;
        }

        var encounterId = await ClinicalTestData.SeedStartedEncounterAsync(application);
        var draftReq = new CreatePrescriptionDraftRequest
        {
            EncounterId = encounterId,
            PatientId = patientId,
            DepartmentId = deptId,
            DiagnosisSummary = "Pnömoni Şüphesi",
            GeneralInstructions = "Tok karnına günde 2 kez",
            Items =
            [
                new CreatePrescriptionDraftItemRequest
                {
                    MedicationCatalogItemId = amoxId,
                    Dose = 500,
                    DoseUnit = "mg",
                    Frequency = "2x1",
                    DurationDays = 14,
                    Quantity = 2,
                    QuantityUnit = "kutu",
                },
            ],
        };

        var draftRes = await PostWithAntiforgeryAsync(doctorClient, "/api/v1/pharmacy/prescriptions", draftReq);
        Assert.Equal(HttpStatusCode.Created, draftRes.StatusCode);
        var draft = await draftRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(draft);

        var signRes = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/sign",
            new SignPrescriptionRequest
            {
                ExpectedVersion = draft.Version,
                ValidDays = 14,
            });
        Assert.Equal(HttpStatusCode.OK, signRes.StatusCode);
        var signed = await signRes.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(signed);

        // 2. Pharmacist logs in and retrieves FEFO candidate lots
        var pharmacistClient = CreateSecureClient(application);
        await LoginAsync(pharmacistClient, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");

        var fefoResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/inventory/fefo-candidates/{amoxId}");
        Assert.Equal(HttpStatusCode.OK, fefoResp.StatusCode);
        var fefoList = await fefoResp.Content.ReadFromJsonAsync<List<FefoCandidateStockResponse>>();
        Assert.NotNull(fefoList);
        var selectedLot = fefoList.First();

        // 3. Step 1: Partial Dispense (1 box out of 2)
        var partialDispenseReq = new DispensePrescriptionRequest
        {
            IdempotencyKey = Guid.NewGuid(),
            ExpectedVersion = signed.Version,
            Items =
            [
                new DispensePrescriptionItemRequest
                {
                    ItemId = draft.Items[0].Id,
                    StockItemId = selectedLot.StockItemId,
                    ExpectedStockVersion = selectedLot.Version,
                    Quantity = 1,
                    Notes = "1. kutu teslim edildi",
                },
            ],
        };

        var dispenseRes1 = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/dispense",
            partialDispenseReq);
        Assert.Equal(HttpStatusCode.OK, dispenseRes1.StatusCode);
        var partialResult = await dispenseRes1.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(partialResult);
        Assert.Equal("PartiallyDispensed", partialResult.Status);
        Assert.Equal(1, partialResult.Items[0].DispensedQuantity);
        Assert.False(partialResult.Items[0].IsFullyDispensed);

        var retryResponse = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/dispense",
            partialDispenseReq);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retryResult = await retryResponse.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(retryResult);
        Assert.Equal(1, retryResult.Items[0].DispensedQuantity);

        var reusedKeyWithDifferentPayload = new DispensePrescriptionRequest
        {
            IdempotencyKey = partialDispenseReq.IdempotencyKey,
            ExpectedVersion = signed.Version,
            Items =
            [
                new DispensePrescriptionItemRequest
                {
                    ItemId = draft.Items[0].Id,
                    StockItemId = selectedLot.StockItemId,
                    ExpectedStockVersion = selectedLot.Version,
                    Quantity = 2,
                },
            ],
        };
        var reusedKeyResponse = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/dispense",
            reusedKeyWithDifferentPayload);
        Assert.Equal(HttpStatusCode.Conflict, reusedKeyResponse.StatusCode);

        var refreshedFefoResp = await pharmacistClient.GetAsync($"/api/v1/pharmacy/inventory/fefo-candidates/{amoxId}");
        var refreshedFefo = await refreshedFefoResp.Content.ReadFromJsonAsync<List<FefoCandidateStockResponse>>();
        Assert.NotNull(refreshedFefo);
        var refreshedSelectedLot = refreshedFefo.Single(lot => lot.StockItemId == selectedLot.StockItemId);

        // 4. Step 2: Full Dispense (Remaining 1 box)
        var fullDispenseReq = new DispensePrescriptionRequest
        {
            IdempotencyKey = Guid.NewGuid(),
            ExpectedVersion = partialResult.Version,
            Items =
            [
                new DispensePrescriptionItemRequest
                {
                    ItemId = draft.Items[0].Id,
                    StockItemId = selectedLot.StockItemId,
                    ExpectedStockVersion = refreshedSelectedLot.Version,
                    Quantity = 1,
                    Notes = "2. kutu teslim edildi",
                },
            ],
        };

        var dispenseRes2 = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/dispense",
            fullDispenseReq);
        Assert.Equal(HttpStatusCode.OK, dispenseRes2.StatusCode);
        var fullResult = await dispenseRes2.Content.ReadFromJsonAsync<PrescriptionDetailResponse>();
        Assert.NotNull(fullResult);
        Assert.Equal("Dispensed", fullResult.Status);
        Assert.Equal(2, fullResult.Items[0].DispensedQuantity);
        Assert.True(fullResult.Items[0].IsFullyDispensed);

        // 5. Step 3: Over-dispense rejection
        var overDispenseReq = new DispensePrescriptionRequest
        {
            IdempotencyKey = Guid.NewGuid(),
            ExpectedVersion = fullResult.Version,
            Items =
            [
                new DispensePrescriptionItemRequest
                {
                    ItemId = draft.Items[0].Id,
                    StockItemId = selectedLot.StockItemId,
                    ExpectedStockVersion = refreshedSelectedLot.Version + 1,
                    Quantity = 1,
                    Notes = "Fazladan kutu",
                },
            ],
        };

        var overDispenseRes = await PostWithAntiforgeryAsync(
            pharmacistClient,
            $"/api/v1/pharmacy/prescriptions/{draft.Id}/dispense",
            overDispenseReq);
        Assert.Equal(HttpStatusCode.Conflict, overDispenseRes.StatusCode);

        // 6. Verify stock deduction and transaction records in DB
        await using var scope2 = application.Services.CreateAsyncScope();
        var pharmacyDb2 = scope2.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        var stockItem = await pharmacyDb2.MedicationStockItems.FirstAsync(s => s.Id == selectedLot.StockItemId);
        Assert.Equal(48, stockItem.QuantityOnHand); // Başlangıç 50 idi, 2 adet düşüldü -> 48

        var transactions = await pharmacyDb2.MedicationStockTransactions
            .Where(t => t.StockItemId == selectedLot.StockItemId && t.TransactionType == StockTransactionType.Dispense)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);

        // 7. Verify audit log
        var auditDb = scope2.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
        var logs = await auditDb.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, l => l.Action == "Pharmacy.PrescriptionDispense" && l.TargetResourceId == draft.Id.ToString());
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

        var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
        await identitySeeder.SeedAsync();

        var pharmacySeeder = scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>();
        await pharmacySeeder.SeedAsync();
    }
}
