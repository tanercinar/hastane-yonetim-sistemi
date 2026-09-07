using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
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

public sealed class Phase6ProductGateEndToEndTests
{
    private static readonly Guid PatientId =
        Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid DoctorId =
        Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid CardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F06-KAPI")]
    public async Task DoctorLabTechAndPatientCompleteLaboratoryJourneyInBrowser()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase6BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var encounterId = await application.PrepareGateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        var pageErrors = new ConcurrentQueue<string>();

        // 1. Doctor creates diagnostic order in browser
        await using var doctorContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var doctorPage = await doctorContext.NewPageAsync();
        doctorPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(doctorPage, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await doctorPage.GotoAsync($"/doctor/diagnostic-orders/{encounterId}", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        await Expect(doctorPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Klinik Tanısal İstem Yönetimi",
            Exact = true,
        })).ToBeVisibleAsync();

        await doctorPage.GetByLabel("Laboratuvar Testi veya Paneli Ara", new()
        {
            Exact = true,
        }).FillAsync("Tam Kan");
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Katalogda Ara",
            Exact = true,
        }).ClickAsync();

        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Tam Kan Sayımı (Hemogram 18 Parametre) isteme ekle",
            Exact = true,
        }).ClickAsync();

        await doctorPage.Locator("input[placeholder^='Örn: Anemi']").FillAsync("DEMO rutin preop kontrol");

        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "İstemi Onayla ve Gönder",
            Exact = true,
        }).ClickAsync();

        await Expect(doctorPage.Locator(".badge").Filter(new()
        {
            HasTextString = "İletildi",
        })).ToBeVisibleAsync();

        // 2. Lab Tech processes specimen and submits lab results
        await using var labContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var labPage = await labContext.NewPageAsync();
        labPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(labPage, "DEMO-labtech@hospital.invalid", "DEMO-LabTech-Pass!1");

        await labPage.GotoAsync("/laboratory/specimens", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(labPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Numune ve Barkod Takip Zinciri",
            Exact = true,
        })).ToBeVisibleAsync();

        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Numune Al ve Barkodla",
            Exact = true,
        }).ClickAsync();
        await Expect(labPage.Locator(".modal-title").Filter(new()
        {
            HasTextString = "Numune Alma",
        })).ToBeVisibleAsync();
        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Numuneyi Kaydet",
            Exact = true,
        }).ClickAsync();

        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Taşımaya Ver",
            Exact = true,
        }).ClickAsync();
        await Expect(labPage.Locator(".badge").Filter(new()
        {
            HasTextString = "Taşımada"
        }).First)
            .ToBeVisibleAsync();
        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Laboratuvara Kabul Et",
            Exact = true,
        }).ClickAsync();
        await Expect(labPage.Locator(".badge").Filter(new()
        {
            HasTextString = "Kabul Edildi"
        }).First)
            .ToBeVisibleAsync();

        await labPage.GotoAsync("/laboratory/results", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(labPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Laboratuvar Sonuç Girişi ve Onay",
            Exact = true,
        })).ToBeVisibleAsync();
        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Sonuç Taslağı Oluştur",
            Exact = true,
        }).ClickAsync();

        var resultInputs = labPage.Locator("[data-testid='lab-result-detail-card'] input[type='number']");
        await Expect(resultInputs.First).ToBeVisibleAsync();
        var normalValues = new[] { "7.4", "5.0", "14.1", "44", "250", "60", "30" };
        for (var index = 0; index < await resultInputs.CountAsync(); index++)
        {
            await resultInputs.Nth(index).FillAsync(normalValues[Math.Min(index, normalValues.Length - 1)]);
        }

        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Kaydet",
            Exact = true
        }).ClickAsync();
        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Teknik Onay",
            Exact = true
        }).ClickAsync();
        await Expect(labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Klinik Onay (Kesinleştir)",
            Exact = true,
        })).ToBeVisibleAsync();
        await labPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Klinik Onay (Kesinleştir)",
            Exact = true,
        }).ClickAsync();
        await Expect(labPage.Locator("[data-testid='lab-result-detail-card']")).ToContainTextAsync("Kesinleşmiş (Onaylı)");

        // 3. Patient views approved results in patient portal
        await using var patientContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var patientPage = await patientContext.NewPageAsync();
        patientPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(patientPage, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        await patientPage.GotoAsync("/patient/diagnostic-results", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        await Expect(patientPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Tahlil ve Tanı Sonuçlarım",
            Exact = true,
        })).ToBeVisibleAsync();

        await Expect(patientPage.Locator(".card").Filter(new()
        {
            HasTextString = "Tam Kan Sayımı",
        })).ToBeVisibleAsync();

        // 4. Verify persisted state and audit evidence
        await application.AssertLaboratoryJourneyIntegrityAsync(encounterId);
        Assert.Empty(pageErrors);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F06-KAPI")]
    public async Task DoctorRadiologyStaffAndPatientCompleteRadiologyJourneyInBrowser()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase6BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var encounterId = await application.PrepareGateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
        var pageErrors = new ConcurrentQueue<string>();

        await using var doctorContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var doctorPage = await doctorContext.NewPageAsync();
        doctorPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(doctorPage, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await doctorPage.GotoAsync($"/doctor/diagnostic-orders/{encounterId}", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await doctorPage.GetByLabel("İstem Türü", new()
        {
            Exact = true
        }).SelectOptionAsync("Radiology");
        await doctorPage.GetByLabel("Radyoloji Tetkiki Ara", new()
        {
            Exact = true
        }).FillAsync("Akciğer");
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Katalogda Ara",
            Exact = true
        }).ClickAsync();
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Akciğer Grafisi (PA / Göğüs Radyografisi) isteme ekle",
            Exact = true,
        }).ClickAsync();
        await doctorPage.Locator("input[placeholder^='Örn: Anemi']").FillAsync("DEMO akciğer değerlendirmesi");
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "İstemi Onayla ve Gönder",
            Exact = true
        }).ClickAsync();
        await Expect(doctorPage.Locator(".alert-success")).ToContainTextAsync("radyoloji birimine iletildi");

        await using var radiologyContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var radiologyPage = await radiologyContext.NewPageAsync();
        radiologyPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(radiologyPage, "DEMO-radtech@hospital.invalid", "DEMO-RadTech-Pass!1");
        await radiologyPage.GotoAsync("/radiology/worklist", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Expect(radiologyPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Radyoloji İş Listesi ve Raporlama",
            Exact = true,
        })).ToBeVisibleAsync();

        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "İş Listesine Al",
            Exact = true
        }).ClickAsync();
        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Randevu",
            Exact = true
        }).ClickAsync();
        await radiologyPage.Locator("input[type='datetime-local']").FillAsync(
            DateTime.Now.AddHours(2).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture));
        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Randevuyu Kaydet",
            Exact = true
        }).ClickAsync();

        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Çekimi Tamamla",
            Exact = true
        }).ClickAsync();
        await radiologyPage.Locator("textarea[placeholder^='Örn: Standart AP/PA']").FillAsync("DEMO standart çekim tamamlandı.");
        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Çekimi Tamamla & Raporlamaya Gönder",
            Exact = true,
        }).ClickAsync();

        await radiologyPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Rapor Yaz",
            Exact = true
        }).ClickAsync();
        var reportModal = radiologyPage.GetByRole(AriaRole.Dialog);
        await reportModal.GetByLabel("Bulgular ve Rapor Metni *", new()
        {
            Exact = true
        })
            .FillAsync("DEMO sentetik radyoloji bulguları doğal sınırlardadır.");
        await reportModal.GetByLabel("Klinik Sonuç / Kanaat (Impression) *", new()
        {
            Exact = true
        })
            .FillAsync("DEMO normal akciğer grafisi.");
        await reportModal.GetByRole(AriaRole.Button, new()
        {
            Name = "Raporu Kesinleştir",
            Exact = true
        }).ClickAsync();
        await Expect(radiologyPage.Locator("tbody")).ToContainTextAsync("Rapor Kesinleşti");

        await using var patientContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var patientPage = await patientContext.NewPageAsync();
        patientPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(patientPage, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        await patientPage.GotoAsync("/patient/diagnostic-results", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Expect(patientPage.Locator(".card").Filter(new()
        {
            HasTextString = "Akciğer Grafisi (PA / Göğüs Radyografisi)",
        })).ToBeVisibleAsync();

        await application.AssertRadiologyJourneyIntegrityAsync(encounterId);
        Assert.Empty(pageErrors);
    }

    private static async Task<IBrowserContext> CreateBrowserContextAsync(
        IBrowser browser,
        Uri baseAddress,
        int width,
        int height) =>
        await browser.NewContextAsync(new()
        {
            BaseURL = baseAddress.ToString(),
            IgnoreHTTPSErrors = true,
            ViewportSize = new()
            {
                Width = width,
                Height = height,
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
            Exact = true
        }).FillAsync(email);
        await page.GetByLabel("Parola").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Giriş yap",
            Exact = true,
        }).ClickAsync();
        await page.WaitForURLAsync(url => !url.EndsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith("/login", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Phase6BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly X509Certificate2 _certificate;
        private Uri? _baseAddress;

        public Phase6BrowserWebApplicationFactory(string connectionString)
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

        public async Task<Guid> PrepareGateAsync()
        {
            await using var scope = Services.CreateAsyncScope();

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

            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();

            var organizationSeeder = scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>();
            await organizationSeeder.SeedAsync();

            var labSeeder = scope.ServiceProvider.GetRequiredService<ILabCatalogDataSeeder>();
            await labSeeder.SeedAsync();

            var radSeeder = scope.ServiceProvider.GetRequiredService<IRadiologyCatalogDataSeeder>();
            await radSeeder.SeedAsync();

            var bloodSeeder = scope.ServiceProvider.GetRequiredService<IBloodBankDataSeeder>();
            await bloodSeeder.SeedAsync();

            var encounterId = Guid.NewGuid();
            var nowUtc = DateTime.UtcNow;
            var encounter = Encounter.Create(
                encounterId,
                appointmentId: null,
                PatientId,
                CardiologyDepartmentId,
                DoctorId,
                EncounterType.Outpatient,
                plannedStartTimeUtc: nowUtc,
                chiefComplaint: "DEMO Faz 6 tanısal süreç akışı",
                nowUtc,
                startImmediately: true);
            clinicalDb.Encounters.Add(encounter);
            await clinicalDb.SaveChangesAsync();

            return encounter.Id;
        }

        public async Task AssertLaboratoryJourneyIntegrityAsync(Guid encounterId)
        {
            await using var scope = Services.CreateAsyncScope();
            var diagnosticsDb = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
            var order = await diagnosticsDb.DiagnosticOrders
                .Include(o => o.Items)
                .SingleAsync(o => o.EncounterId == encounterId && o.OrderType == DiagnosticOrderType.Laboratory);
            Assert.Equal(DiagnosticOrderStatus.Completed, order.Status);
            Assert.All(order.Items, i => Assert.Equal(DiagnosticOrderItemStatus.Reported, i.Status));

            var specimen = await diagnosticsDb.Specimens
                .SingleAsync(s => s.DiagnosticOrderId == order.Id);
            Assert.Equal(SpecimenStatus.Received, specimen.Status);

            var labResult = await diagnosticsDb.LabResults
                .Include(r => r.Items)
                .SingleAsync(r => r.DiagnosticOrderId == order.Id);
            Assert.Equal(LabResultStatus.FinalApproved, labResult.Status);
            Assert.NotEmpty(labResult.Items);

            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs
                .Select(a => a.Action)
                .ToListAsync();

            Assert.Contains("Diagnostics.OrderCreateDraft", audits);
            Assert.Contains("Diagnostics.OrderPlace", audits);
            Assert.Contains("Diagnostics.SpecimenCollect", audits);
            Assert.Contains("Diagnostics.SpecimenTransit", audits);
            Assert.Contains("Diagnostics.SpecimenReceive", audits);
            Assert.Contains("Diagnostics.LabResultCreateDraft", audits);
            Assert.Contains("Diagnostics.LabResultTechnicalApprove", audits);
            Assert.Contains("Diagnostics.LabResultClinicalApprove", audits);
            Assert.Contains("Diagnostics.TimelinePatientPortalView", audits);
        }

        public async Task AssertRadiologyJourneyIntegrityAsync(Guid encounterId)
        {
            await using var scope = Services.CreateAsyncScope();
            var diagnosticsDb = scope.ServiceProvider.GetRequiredService<DiagnosticsDbContext>();
            var order = await diagnosticsDb.DiagnosticOrders
                .Include(o => o.Items)
                .SingleAsync(o => o.EncounterId == encounterId && o.OrderType == DiagnosticOrderType.Radiology);
            Assert.Equal(DiagnosticOrderStatus.Completed, order.Status);
            Assert.All(order.Items, item => Assert.Equal(DiagnosticOrderItemStatus.Reported, item.Status));

            var study = await diagnosticsDb.RadiologyStudies
                .SingleAsync(item => item.DiagnosticOrderId == order.Id);
            Assert.Equal(RadiologyStudyStatus.ReportFinalized, study.Status);
            Assert.False(string.IsNullOrWhiteSpace(study.ReportText));
            Assert.False(string.IsNullOrWhiteSpace(study.Impression));

            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var audits = await auditDb.AuditLogs.Select(item => item.Action).ToListAsync();
            Assert.Contains("Diagnostics.OrderCreateDraft", audits);
            Assert.Contains("Diagnostics.OrderPlace", audits);
            Assert.Contains("Diagnostics.RadiologyStudyCreate", audits);
            Assert.Contains("Diagnostics.RadiologyStudySchedule", audits);
            Assert.Contains("Diagnostics.RadiologyAcquisitionComplete", audits);
            Assert.Contains("Diagnostics.RadiologyReportFinalize", audits);
            Assert.Contains("Diagnostics.TimelinePatientPortalView", audits);
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
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-phase6-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-PHASE6-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-PHASE6-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-phase6-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string certificatePassword = "DEMO-PHASE6-E2E-CERTIFICATE";
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
                .WithDatabase($"hms_phase6_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_phase6_e2e")
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
