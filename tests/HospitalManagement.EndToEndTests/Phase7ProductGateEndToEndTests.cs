using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Inpatient;
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
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

using Testcontainers.PostgreSql;

using static Microsoft.Playwright.Assertions;

namespace HospitalManagement.EndToEndTests;

public sealed class Phase7ProductGateEndToEndTests
{
    private static readonly Guid PatientId =
        Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid DoctorId =
        Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid CardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid ParacetamolCatalogItemId =
        Guid.Parse("00000000-0000-0000-0000-000000000604");

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F07-KAPI")]
    public async Task NurseAndDoctorCompleteObservationEmarAndDischargeJourneyInBrowser()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase7BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var gate = await application.PrepareGateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        var pageErrors = new ConcurrentQueue<string>();

        await using var nurseContext = await CreateBrowserContextAsync(browser, baseAddress);
        var nursePage = await nurseContext.NewPageAsync();
        nursePage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(nursePage, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");

        await nursePage.GotoAsync("/inpatient/nursing", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(nursePage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Hemşire Gözlem & Bakım Planı",
            Exact = true,
        })).ToBeVisibleAsync();
        await Expect(nursePage.Locator("select.form-select-lg")).ToHaveValueAsync(gate.AdmissionId.ToString());

        await nursePage.GetByRole(AriaRole.Button, new()
        {
            Name = "Vital & Gözlem Gir",
            Exact = true,
        }).ClickAsync();
        var observationModal = nursePage.Locator(".modal-content").Filter(new()
        {
            HasTextString = "Yeni Vital & Hemşire Gözlemi Girişi",
        });
        await Expect(observationModal).ToBeVisibleAsync();
        await observationModal.GetByRole(AriaRole.Button, new()
        {
            Name = "Kaydet",
            Exact = true,
        }).ClickAsync();
        await Expect(nursePage.Locator(".alert-success[role='alert']")).ToContainTextAsync("Vital bulgular ve gözlem başarıyla kaydedildi");

        await nursePage.GotoAsync("/inpatient/emar", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(nursePage.GetByRole(AriaRole.Heading, new()
        {
            Name = "İlaç Uygulama Kaydı (eMAR)",
            Exact = true,
        })).ToBeVisibleAsync();
        await Expect(nursePage.Locator("select.form-select-lg")).ToHaveValueAsync(gate.AdmissionId.ToString());

        await nursePage.GetByRole(AriaRole.Button, new()
        {
            Name = "Yeni İlaç Dozu Planla",
            Exact = true,
        }).ClickAsync();
        var scheduleModal = nursePage.Locator(".modal-content").Filter(new()
        {
            HasTextString = "Yeni İlaç Dozu Planla",
        });
        await Expect(scheduleModal.Locator("#activeMedicationOrder")).ToContainTextAsync("DEMO-Parasetamol 500mg Tablet");
        await scheduleModal.GetByRole(AriaRole.Button, new()
        {
            Name = "Dozu Kaydet",
            Exact = true,
        }).ClickAsync();
        await Expect(nursePage.Locator(".alert-success[role='alert']")).ToContainTextAsync("Yeni ilaç dozu başarıyla planlandı");

        await nursePage.GetByRole(AriaRole.Button, new()
        {
            Name = "Uygula",
            Exact = true,
        }).ClickAsync();
        var administerModal = nursePage.Locator(".modal-content").Filter(new()
        {
            HasTextString = "eMAR İlaç Uygulama Güvenlik Doğrulaması",
        });
        var administerButton = administerModal.GetByRole(AriaRole.Button, new()
        {
            Name = "İlaç Uygulamasını Onayla",
            Exact = true,
        });
        await Expect(administerButton).ToBeDisabledAsync();
        await administerModal.Locator("#chkRightPatient").CheckAsync();
        await administerModal.Locator("#chkRightMed").CheckAsync();
        await administerModal.Locator("#chkRightDose").CheckAsync();
        await administerModal.Locator("#chkRightTime").CheckAsync();
        await administerModal.Locator("#chkRightRoute").CheckAsync();
        await Expect(administerButton).ToBeEnabledAsync();
        await administerButton.ClickAsync();
        await Expect(nursePage.Locator(".alert-success[role='alert']")).ToContainTextAsync("İlaç uygulaması başarıyla kaydedildi");

        await using var doctorContext = await CreateBrowserContextAsync(browser, baseAddress);
        var doctorPage = await doctorContext.NewPageAsync();
        doctorPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(doctorPage, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await doctorPage.GotoAsync("/inpatient/discharges", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(doctorPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Taburculuk ve Sevk İşlemleri",
            Exact = true,
        })).ToBeVisibleAsync();
        var dischargeButton = doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Taburcu Et / Sevk",
            Exact = true,
        });
        await dischargeButton.DispatchEventAsync("click");
        var dischargeModal = doctorPage.Locator(".modal-content").Filter(new()
        {
            HasTextString = "Hasta Taburculuk ve Sevk Formu",
        });
        await dischargeModal.GetByRole(AriaRole.Button, new()
        {
            Name = "Taburculuk İşlemini Tamamla ve Yatağı Boşalt",
            Exact = true,
        }).ClickAsync();
        await Expect(doctorPage.Locator(".alert-success[role='alert']")).ToContainTextAsync("Taburculuk işlemi başarıyla tamamlandı");

        await application.AssertJourneyIntegrityAsync(gate);
        Assert.Empty(pageErrors);
    }

    private static async Task<IBrowserContext> CreateBrowserContextAsync(IBrowser browser, Uri baseAddress) =>
        await browser.NewContextAsync(new()
        {
            BaseURL = baseAddress.AbsoluteUri,
            IgnoreHTTPSErrors = true,
            Locale = "tr-TR",
            TimezoneId = "Europe/Istanbul",
            ViewportSize = new ViewportSize
            {
                Width = 1280,
                Height = 900,
            },
        });

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/account/login", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true,
        }).FillAsync(email);
        await page.GetByLabel("Parola", new()
        {
            Exact = true,
        }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Giriş yap",
            Exact = true,
        }).ClickAsync();
        await page.WaitForURLAsync(url => !url.EndsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith("/login", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record Phase7GateData(Guid AdmissionId, Guid BedId);

    private sealed class Phase7BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly X509Certificate2 _certificate;
        private Uri? _baseAddress;

        public Phase7BrowserWebApplicationFactory(string connectionString)
        {
            _connectionString = connectionString;
            _certificate = CreateTestCertificate();
            UseKestrel();
        }

        public Uri StartAndGetBaseAddress()
        {
            StartServer();
            var server = Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
                ?? throw new InvalidOperationException("Kestrel did not expose a listening address.");
            var chosen = addresses.Single(address =>
                address.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            _baseAddress = new Uri(chosen, UriKind.Absolute);
            return _baseAddress;
        }

        public async Task<Phase7GateData> PrepareGateAsync()
        {
            await MigrateAndSeedAsync();
            var encounterId = await SeedSignedMedicationOrderAsync();

            using var doctorClient = CreateKestrelClient();
            var doctorLogin = await LoginApiAsync(
                doctorClient,
                "DEMO-doctor@hospital.invalid",
                "DEMO-Doc-Pass!1");
            Assert.Equal(HttpStatusCode.OK, doctorLogin.StatusCode);

            var wards = await doctorClient.GetFromJsonAsync<List<WardResponse>>("/api/v1/inpatient/wards");
            Assert.NotNull(wards);
            var ward = wards.Single(candidate => candidate.Code == "DEMO-WRD-CARD");

            var admissionResponse = await PostWithAntiforgeryAsync(
                doctorClient,
                "/api/v1/inpatient/admissions",
                new CreateAdmissionRequest
                {
                    PatientId = PatientId,
                    EncounterId = encounterId,
                    DepartmentId = CardiologyDepartmentId,
                    AdmittingWardId = ward.Id,
                    AttendingDoctorId = DoctorId,
                    AdmissionReason = "DEMO F7 tarayıcı yatış süreci",
                    DiagnosisCode = "I20.0",
                    DiagnosisDescription = "DEMO kararlı anjina",
                    DietType = "LowSodium",
                    FallRiskScore = 35,
                    IsolationRequired = "None",
                    EstimatedStayDays = 2,
                });
            Assert.Equal(HttpStatusCode.Created, admissionResponse.StatusCode);
            var admission = await admissionResponse.Content.ReadFromJsonAsync<AdmissionResponse>();
            Assert.NotNull(admission);

            using var nurseClient = CreateKestrelClient();
            var nurseLogin = await LoginApiAsync(
                nurseClient,
                "DEMO-nurse@hospital.invalid",
                "DEMO-Nurse-Pass!1");
            Assert.Equal(HttpStatusCode.OK, nurseLogin.StatusCode);

            var acceptResponse = await PostWithAntiforgeryAsync(
                nurseClient,
                $"/api/v1/inpatient/admissions/{admission.Id}/accept",
                new AcceptAdmissionRequest());
            Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

            var beds = await nurseClient.GetFromJsonAsync<List<BedResponse>>(
                $"/api/v1/inpatient/beds?wardId={ward.Id}&status=Available");
            Assert.NotNull(beds);
            var bed = beds.First();
            var admitResponse = await PostWithAntiforgeryAsync(
                nurseClient,
                $"/api/v1/inpatient/admissions/{admission.Id}/admit",
                new AdmitPatientRequest { BedId = bed.Id });
            Assert.Equal(HttpStatusCode.OK, admitResponse.StatusCode);

            return new Phase7GateData(admission.Id, bed.Id);
        }

        public async Task AssertJourneyIntegrityAsync(Phase7GateData gate)
        {
            await using var scope = Services.CreateAsyncScope();
            var inpatientDb = scope.ServiceProvider.GetRequiredService<InpatientDbContext>();
            var admission = await inpatientDb.Admissions.SingleAsync(item => item.Id == gate.AdmissionId);
            var bed = await inpatientDb.Beds.SingleAsync(item => item.Id == gate.BedId);
            var observationCount = await inpatientDb.NursingObservations.CountAsync(item =>
                item.AdmissionId == gate.AdmissionId);
            var medication = await inpatientDb.MedicationAdministrations.SingleAsync(item =>
                item.AdmissionId == gate.AdmissionId);

            Assert.Equal(AdmissionStatus.Discharged, admission.Status);
            Assert.Equal(BedStatus.Cleaning, bed.Status);
            Assert.Equal(1, observationCount);
            Assert.Equal(MedicationAdministrationStatus.Administered, medication.Status);
            Assert.True(medication.Verified5Rights);

            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var actions = await auditDb.AuditLogs.Select(item => item.Action).ToListAsync();
            Assert.Contains("Inpatient.ObservationRecord", actions);
            Assert.Contains("Inpatient.MedicationSchedule", actions);
            Assert.Contains("Inpatient.MedicationAdminister", actions);
            Assert.Contains("Inpatient.AdmissionDischarge", actions);
        }

        private async Task MigrateAndSeedAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PatientsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PharmacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<InpatientDbContext>().Database.MigrateAsync();

            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IInpatientDataSeeder>().SeedAsync();
        }

        private async Task<Guid> SeedSignedMedicationOrderAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var catalogItem = await pharmacyDb.MedicationCatalogItems.SingleAsync(item =>
                item.Id == ParacetamolCatalogItemId);
            var nowUtc = DateTime.UtcNow;
            var encounterId = Guid.NewGuid();
            var prescription = Prescription.CreateDraft(
                Guid.NewGuid(),
                $"DEMO-RX-F07-{nowUtc:yyyyMMddHHmmss}",
                PatientId,
                encounterId,
                DoctorId,
                CardiologyDepartmentId,
                "DEMO F7 eMAR order hazırlığı",
                "Yalnız sentetik tarayıcı testi içindir.",
                nowUtc);
            prescription.AddItem(
                Guid.NewGuid(),
                catalogItem.Id,
                catalogItem.Code,
                catalogItem.BrandName,
                catalogItem.GenericName,
                catalogItem.Form,
                catalogItem.Route,
                500m,
                "mg",
                "2x1",
                7,
                1,
                "kutu",
                "DEMO order",
                nowUtc);
            prescription.Sign(DoctorId, nowUtc.AddDays(7), nowUtc);
            pharmacyDb.Prescriptions.Add(prescription);
            await pharmacyDb.SaveChangesAsync();
            return encounterId;
        }

        private HttpClient CreateKestrelClient()
        {
            if (_baseAddress is null)
            {
                throw new InvalidOperationException("Kestrel must be started before creating an API client.");
            }

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = new CookieContainer(),
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            return new HttpClient(handler) { BaseAddress = _baseAddress };
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseStaticWebAssets();
            builder.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listenOptions => listenOptions.UseHttps(_certificate));
            });
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(CreateSettings(_connectionString));
            });
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<IIdentityMessageSender>();
                services.AddSingleton<IIdentityMessageSender, NoOpIdentityMessageSender>();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _certificate.Dispose();
            }
        }

        private static Dictionary<string, string?> CreateSettings(string connectionString) =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["HospitalManagement:Runtime:DataMode"] = "DEMO",
                ["HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled"] = "false",
                ["HospitalManagement:Api:RateLimit:PermitLimit"] = "500",
                ["HospitalManagement:Api:RateLimit:WindowSeconds"] = "60",
                ["HospitalManagement:Identity:SensitivePermitLimit"] = "100",
                ["ConnectionStrings:HospitalDatabase"] = connectionString,
                ["HospitalManagement:ObjectStorage:Endpoint"] = "http://127.0.0.1:9000",
                ["HospitalManagement:ObjectStorage:UseTls"] = "false",
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-phase7-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-PHASE7-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-PHASE7-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-phase7-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string certificatePassword = "DEMO-PHASE7-E2E-CERTIFICATE";
            using var privateKey = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=localhost",
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new("1.3.6.1.5.5.7.3.1") },
                false));
            using var generated = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddHours(1));
            return X509CertificateLoader.LoadPkcs12(
                generated.Export(X509ContentType.Pkcs12, certificatePassword),
                certificatePassword,
                X509KeyStorageFlags.Exportable);
        }
    }

    private static Task<HttpResponseMessage> LoginApiAsync(HttpClient client, string email, string password) =>
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
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        return await client.SendAsync(request);
    }

    private sealed class NoOpIdentityMessageSender : IIdentityMessageSender
    {
        public Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class BrowserPostgreSqlDatabase : IAsyncDisposable
    {
        private readonly PostgreSqlContainer _container;

        private BrowserPostgreSqlDatabase(PostgreSqlContainer container)
        {
            _container = container;
        }

        public string ConnectionString => _container.GetConnectionString();

        public static async Task<BrowserPostgreSqlDatabase> StartAsync()
        {
            var container = new PostgreSqlBuilder("postgres:18.6")
                .WithDatabase($"hms_phase7_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_phase7_e2e")
                .WithPassword("DEMO-E2E-ONLY-NOT-A-SECRET")
                .Build();
            try
            {
                await container.StartAsync();
                return new BrowserPostgreSqlDatabase(container);
            }
            catch
            {
                await container.DisposeAsync();
                throw;
            }
        }

        public ValueTask DisposeAsync() => _container.DisposeAsync();
    }
}
