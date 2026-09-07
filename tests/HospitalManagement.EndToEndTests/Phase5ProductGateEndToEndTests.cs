using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.ClinicalRecords.Domain;
using HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

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

public sealed class Phase5ProductGateEndToEndTests
{
    private static readonly Guid PatientId =
        Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid DoctorId =
        Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid CardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F05-KAPI")]
    public async Task DoctorPharmacistAndPatientCompletePrescriptionJourneyInBrowser()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase5BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var encounterId = await application.PrepareGateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        var pageErrors = new ConcurrentQueue<string>();

        await using var doctorContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var doctorPage = await doctorContext.NewPageAsync();
        doctorPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(doctorPage, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");
        await doctorPage.GotoAsync($"/doctor/prescriptions/{encounterId}", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        await Expect(doctorPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Doktor Reçete Yönetimi",
            Exact = true,
        })).ToBeVisibleAsync();
        await Expect(doctorPage.GetByLabel("Hasta Kimliği (Patient ID)", new()
        {
            Exact = true,
        })).ToHaveValueAsync(PatientId.ToString());

        await doctorPage.Locator("input[placeholder^='İlaç adı']").FillAsync("Parasetamol");
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Ara",
            Exact = true
        }).ClickAsync();
        var medicationRow = doctorPage.GetByRole(AriaRole.Row).Filter(new()
        {
            HasTextString = "DEMO-Parasetamol",
        }).First;
        await Expect(medicationRow).ToBeVisibleAsync();
        await medicationRow.GetByRole(AriaRole.Button, new()
        {
            Name = "+ Ekle",
            Exact = true,
        }).ClickAsync();
        await doctorPage.GetByLabel("Tanı Özeti", new()
        {
            Exact = true
        }).FillAsync("DEMO akut ağrı");
        await doctorPage.GetByLabel("Genel Hasta Talimatı", new()
        {
            Exact = true
        })
            .FillAsync("Bol su ile kullanınız.");
        await doctorPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Reçeteyi İmzala",
            Exact = true,
        }).ClickAsync();
        await Expect(doctorPage.GetByRole(AriaRole.Status))
            .ToContainTextAsync("Reçete başarıyla imzalandı", new()
            {
                Timeout = 20_000
            });

        var prescriptionNumber = await application.GetPrescriptionNumberAsync(encounterId);

        await using var pharmacistContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900);
        var pharmacistPage = await pharmacistContext.NewPageAsync();
        pharmacistPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(pharmacistPage, "DEMO-pharmacist@hospital.invalid", "DEMO-Pharm-Pass!1");
        await pharmacistPage.GotoAsync("/pharmacy/worklist", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        var prescriptionRow = pharmacistPage.GetByRole(AriaRole.Row).Filter(new()
        {
            HasTextString = prescriptionNumber,
        });
        await Expect(prescriptionRow).ToBeVisibleAsync();
        await prescriptionRow.GetByRole(AriaRole.Button, new()
        {
            Name = "İncele",
            Exact = true,
        }).ClickAsync();
        await Expect(pharmacistPage.GetByText("Klinik İzolasyon Uyumu:", new()
        {
            Exact = true
        }))
            .ToBeVisibleAsync();
        await pharmacistPage.GetByRole(AriaRole.Button, new()
        {
            Name = "İlaç Teslim Et",
            Exact = true,
        }).ClickAsync();
        await Expect(pharmacistPage.GetByText("FEFO Doğrulamalı Stok", new()
        {
            Exact = true
        }))
            .ToBeVisibleAsync();
        await pharmacistPage.GetByRole(AriaRole.Button, new()
        {
            Name = "Teslimi Onayla ve Stoktan Düş",
            Exact = true,
        }).ClickAsync();
        await Expect(pharmacistPage.GetByRole(AriaRole.Alert))
            .ToContainTextAsync("teslimi başarıyla kaydedildi", new()
            {
                Timeout = 20_000
            });

        await using var patientContext = await CreateBrowserContextAsync(browser, baseAddress, 390, 844);
        var patientPage = await patientContext.NewPageAsync();
        patientPage.PageError += (_, error) => pageErrors.Enqueue(error);
        await LoginAsync(patientPage, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
        await patientPage.GotoAsync("/patient/prescriptions", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        var prescriptionCard = patientPage.Locator("article").Filter(new()
        {
            HasTextString = prescriptionNumber,
        });
        await Expect(prescriptionCard).ToBeVisibleAsync();
        await prescriptionCard.GetByRole(AriaRole.Button, new()
        {
            Name = "İlaçları ve Talimatları Gör",
            Exact = true,
        }).ClickAsync();
        await Expect(patientPage.GetByText("Eczaneden Alındı", new()
        {
            Exact = true
        })).ToBeVisibleAsync();
        await Expect(patientPage.GetByText("Ağızdan", new()
        {
            Exact = true
        })).ToBeVisibleAsync();
        await Expect(patientPage.GetByText("günde 1 kez", new()
        {
            Exact = false
        })).ToBeVisibleAsync();

        var viewportWidths = await patientPage.EvaluateAsync<int[]>(
            "() => [document.documentElement.scrollWidth, window.innerWidth]");
        Assert.True(
            viewportWidths[0] <= viewportWidths[1] + 1,
            $"Hasta reçete sayfası mobil görünümde taştı: {viewportWidths[0]} > {viewportWidths[1]}.");

        await application.AssertDispenseIntegrityAsync(encounterId);
        Assert.Empty(pageErrors);
    }

    private static async Task<IBrowserContext> CreateBrowserContextAsync(
        IBrowser browser,
        Uri baseAddress,
        int width,
        int height) =>
        await browser.NewContextAsync(new()
        {
            BaseURL = baseAddress.AbsoluteUri,
            IgnoreHTTPSErrors = true,
            Locale = "tr-TR",
            TimezoneId = "Europe/Istanbul",
            ViewportSize = new ViewportSize { Width = width, Height = height },
        });

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync("/account/login", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(email);
        await page.GetByLabel("Parola", new()
        {
            Exact = true
        }).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Giriş yap",
            Exact = true
        }).ClickAsync();
        await page.WaitForURLAsync(url => !url.EndsWith("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Phase5BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly X509Certificate2 _certificate;

        public Phase5BrowserWebApplicationFactory(string connectionString)
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
            return new Uri(addresses.Single(value => value.StartsWith("https://", StringComparison.Ordinal)));
        }

        public async Task<Guid> PrepareGateAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PatientsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PharmacyDbContext>().Database.MigrateAsync();

            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IOrganizationDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IMedicationCatalogDataSeeder>().SeedAsync();

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
                chiefComplaint: "DEMO Faz 5 tarayıcı reçete akışı",
                nowUtc,
                startImmediately: true);
            var clinicalDb = scope.ServiceProvider.GetRequiredService<ClinicalRecordsDbContext>();
            clinicalDb.Encounters.Add(encounter);
            await clinicalDb.SaveChangesAsync();
            return encounterId;
        }

        public async Task<string> GetPrescriptionNumberAsync(Guid encounterId)
        {
            await using var scope = Services.CreateAsyncScope();
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            return await pharmacyDb.Prescriptions
                .Where(prescription => prescription.EncounterId == encounterId)
                .Select(prescription => prescription.PrescriptionNumber)
                .SingleAsync();
        }

        public async Task AssertDispenseIntegrityAsync(Guid encounterId)
        {
            await using var scope = Services.CreateAsyncScope();
            var pharmacyDb = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            var prescription = await pharmacyDb.Prescriptions
                .Include(candidate => candidate.Items)
                .SingleAsync(candidate => candidate.EncounterId == encounterId);
            Assert.Equal(PrescriptionStatus.Dispensed, prescription.Status);
            Assert.All(prescription.Items, item => Assert.True(item.IsFullyDispensed));
            Assert.Single(await pharmacyDb.PrescriptionDispenseOperations
                .Where(operation => operation.PrescriptionId == prescription.Id)
                .ToListAsync());
            Assert.Single(await pharmacyDb.MedicationStockTransactions
                .Where(transaction => transaction.TransactionType == StockTransactionType.Dispense
                    && transaction.ReferenceId == prescription.PrescriptionNumber)
                .ToListAsync());

            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            var actions = await auditDb.AuditLogs
                .Where(log => log.TargetResourceId == prescription.Id.ToString())
                .Select(log => log.Action)
                .ToListAsync();
            Assert.Contains("Pharmacy.PrescriptionCreateDraft", actions);
            Assert.Contains("Pharmacy.PrescriptionSign", actions);
            Assert.Contains("Pharmacy.PrescriptionDispense", actions);
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
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-phase5-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-PHASE5-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-PHASE5-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-phase5-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string certificatePassword = "DEMO-PHASE5-E2E-CERTIFICATE";
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
                .WithDatabase($"hms_phase5_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_phase5_e2e")
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
