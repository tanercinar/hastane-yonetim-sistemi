using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using HospitalManagement.Host.Authorization;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;
using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;

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

public sealed class Phase9ProductGateEndToEndTests
{
    private static readonly Guid PatientId =
        Guid.Parse("00000000-0000-0000-0000-000000000201");
    private static readonly Guid PatientPersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000109");
    private static readonly Guid DoctorPersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid NursePersonId =
        Guid.Parse("00000000-0000-0000-0000-000000000103");
    private static readonly Guid NewbornPatientId =
        Guid.Parse("90000000-0000-0000-0000-000000000201");

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F09-KAPI")]
    public async Task SpecialtyRolesReviewPublishedVerticalsAndUnauthorizedUserSeesForbiddenStateInChromium()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase9BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var gate = await application.PrepareGateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true
        });
        var pageErrors = new ConcurrentQueue<string>();
        var browserMessages = new ConcurrentQueue<string>();
        var requestedUrls = new ConcurrentQueue<string>();

        await using (var doctorContext = await CreateBrowserContextAsync(browser, baseAddress, 1440, 1000))
        {
            var page = await CreateObservedPageAsync(doctorContext, pageErrors, browserMessages, requestedUrls);
            await LoginAsync(page, "DEMO-doctor@hospital.invalid", "DEMO-Doc-Pass!1");

            await page.GotoAsync("/specialty/pregnancy", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Gebelik ve Antenatal Takip Panosu"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.PregnancyProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();

            await page.GotoAsync("/specialty/deliveries", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Doğum ve Yenidoğan Kayıt Panosu"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.DeliveryProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            await Expect(page.GetByRole(AriaRole.Cell, new()
            {
                Name = "#1",
                Exact = true
            })).ToBeVisibleAsync();

            await page.GotoAsync("/specialty/dental", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Diş Hekimliği ve Odontogram Panosu"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.DentalProcedureProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            var tooth = page.GetByRole(AriaRole.Button, new()
            {
                Name = "Diş 16 : Sound",
                Exact = true
            });
            await tooth.FocusAsync();
            await Expect(tooth).ToBeFocusedAsync();
            var doctorHomeHealthPayload = await FetchTextAsync(
                page,
                "/api/v1/specialty/home-health/visits/active");
            Assert.DoesNotContain("DEMO-SENSITIVE-CANARY-ADDRESS", doctorHomeHealthPayload, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-SENSITIVE-CANARY-PHONE", doctorHomeHealthPayload, StringComparison.Ordinal);
            await AssertNoHorizontalOverflowAsync(page, 1440);
        }

        await using (var nurseContext = await CreateBrowserContextAsync(browser, baseAddress, 1024, 900))
        {
            var page = await CreateObservedPageAsync(nurseContext, pageErrors, browserMessages, requestedUrls);
            await LoginAsync(page, "DEMO-nurse@hospital.invalid", "DEMO-Nurse-Pass!1");
            await page.GotoAsync("/specialty/home-health", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Evde Sağlık Planlama ve Ziyaret Yönetimi"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.HomeHealthProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            var nurseHomeHealthPayload = await FetchTextAsync(
                page,
                "/api/v1/specialty/home-health/visits/active");
            Assert.Contains("DEMO-SENSITIVE-CANARY-ADDRESS", nurseHomeHealthPayload, StringComparison.Ordinal);
            Assert.Contains("DEMO-SENSITIVE-CANARY-PHONE", nurseHomeHealthPayload, StringComparison.Ordinal);
            await AssertNoHorizontalOverflowAsync(page, 1024);
        }

        await using (var managerContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900))
        {
            var page = await CreateObservedPageAsync(managerContext, pageErrors, browserMessages, requestedUrls);
            await LoginAsync(page, "DEMO-manager@hospital.invalid", "DEMO-Manager-Pass!1");
            await page.GotoAsync("/specialty/reports", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Uzmanlık Operasyonel Göstergeleri ve Raporlar"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText("Toplam Doğum Kaydı", new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            Assert.DoesNotContain("DEMO-SENSITIVE-CANARY", await page.Locator("body").InnerTextAsync(), StringComparison.Ordinal);
        }

        await using (var patientContext = await CreateBrowserContextAsync(browser, baseAddress, 390, 844))
        {
            var page = await CreateObservedPageAsync(patientContext, pageErrors, browserMessages, requestedUrls);
            await LoginAsync(page, "DEMO-patient@hospital.invalid", "DEMO-Patient-Pass!1");
            await page.GotoAsync("/patient/specialty-records", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByRole(AriaRole.Heading, new()
            {
                Name = "Uzmanlık Kayıtlarım"
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.PregnancyProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.DeliveryProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            await Expect(page.GetByText("DEMO Tamamlanmış Dolgu", new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            await Expect(page.GetByText(gate.HomeHealthProtocol, new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            var patientBody = await page.Locator("body").InnerTextAsync();
            Assert.DoesNotContain("DEMO-SENSITIVE-CANARY", patientBody, StringComparison.Ordinal);
            Assert.DoesNotContain(NewbornPatientId.ToString("D"), patientBody, StringComparison.OrdinalIgnoreCase);
            await AssertNoHorizontalOverflowAsync(page, 390);
        }

        await using (var adminContext = await CreateBrowserContextAsync(browser, baseAddress, 1280, 900))
        {
            var page = await CreateObservedPageAsync(adminContext, pageErrors, browserMessages, requestedUrls);
            await LoginAsync(page, "DEMO-admin@hospital.invalid", "DEMO-Admin-Pass!1");
            await page.GotoAsync("/patient/specialty-records", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });
            await Expect(page.GetByText("Bu alana erişiminiz yok", new()
            {
                Exact = true
            })).ToBeVisibleAsync();
            await Expect(page.GetByText("Yayınlanmış uzmanlık kaydı bulunamadı", new()
            {
                Exact = true
            })).ToHaveCountAsync(0);
        }

        Assert.Empty(pageErrors);
        Assert.DoesNotContain(browserMessages, message => message.Contains("DEMO-SENSITIVE-CANARY", StringComparison.Ordinal));
        Assert.DoesNotContain(requestedUrls, url => url.Contains("DEMO-SENSITIVE-CANARY", StringComparison.Ordinal));
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

    private static async Task<IPage> CreateObservedPageAsync(
        IBrowserContext context,
        ConcurrentQueue<string> pageErrors,
        ConcurrentQueue<string> browserMessages,
        ConcurrentQueue<string> requestedUrls)
    {
        var page = await context.NewPageAsync();
        page.PageError += (_, error) => pageErrors.Enqueue(error);
        page.Console += (_, message) => browserMessages.Enqueue(message.Text);
        page.Request += (_, request) => requestedUrls.Enqueue(request.Url);
        return page;
    }

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
        await page.WaitForURLAsync(url => !url.EndsWith("/account/login", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith("/login", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task AssertNoHorizontalOverflowAsync(IPage page, int viewportWidth)
    {
        var scrollWidth = await page.EvaluateAsync<int>("document.documentElement.scrollWidth");
        Assert.True(scrollWidth <= viewportWidth, $"Sayfa yatay taştı: {scrollWidth}px > {viewportWidth}px.");
    }

    private static Task<string> FetchTextAsync(IPage page, string path) =>
        page.EvaluateAsync<string>(
            "async path => { const response = await fetch(path); return await response.text(); }",
            path);

    private sealed record Phase9GateData(
        string PregnancyProtocol,
        string DeliveryProtocol,
        string DentalProcedureProtocol,
        string HomeHealthProtocol);

    private sealed class Phase9BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly X509Certificate2 _certificate;
        private Uri? _baseAddress;

        public Phase9BrowserWebApplicationFactory(string connectionString)
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
            _baseAddress = new Uri(addresses.Single(address =>
                address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));
            return _baseAddress;
        }

        public async Task<Phase9GateData> PrepareGateAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await services.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await services.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await services.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
            await services.GetRequiredService<PatientsDbContext>().Database.MigrateAsync();
            await services.GetRequiredService<SpecialtyCareDbContext>().Database.MigrateAsync();

            await services.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
            await services.GetRequiredService<IOrganizationDataSeeder>().SeedAsync();
            await services.GetRequiredService<IPatientDataSeeder>().SeedAsync();
            services.GetRequiredService<CareRelationshipRegistry>()
                .EstablishCareRelationship(DoctorPersonId, PatientPersonId);

            var nowUtc = DateTime.UtcNow;
            var episode = PregnancyEpisode.Create(
                Guid.Parse("91000000-0000-0000-0000-000000000001"),
                PatientId,
                Guid.Parse("91000000-0000-0000-0000-000000000011"),
                gravida: 2,
                para: 1,
                abortus: 0,
                livingChildren: 1,
                lastMenstrualPeriodUtc: nowUtc.AddDays(-140),
                customEstimatedDeliveryDateUtc: null,
                bloodGroupAndRh: "A+",
                riskCategory: PregnancyRiskCategory.LowRisk,
                riskFactorsNotes: "DEMO-SENSITIVE-CANARY-RISK",
                assignedDoctorId: DoctorPersonId,
                assignedMidwifeId: NursePersonId,
                nowUtc);

            var delivery = DeliveryRecord.Create(
                Guid.Parse("91000000-0000-0000-0000-000000000002"),
                pregnancyEpisodeId: null,
                PatientId,
                encounterId: null,
                DeliveryMode.SpontaneousVaginal,
                nowUtc.AddDays(-14),
                gestationalAgeWeeks: 39,
                gestationalAgeDays: 2,
                PerinealTearDegree.None,
                estimatedBloodLossMl: 250,
                DoctorPersonId,
                NursePersonId,
                pediatricianDoctorId: null,
                maternalComplicationsNotes: "DEMO-SENSITIVE-CANARY-DELIVERY",
                deliverySummaryNotes: "DEMO tarayıcı kapısı doğum özeti",
                nowUtc);
            delivery.AddNewborn(
                Guid.Parse("91000000-0000-0000-0000-000000000003"),
                NewbornPatientId,
                birthOrder: 1,
                nowUtc.AddDays(-14),
                NewbornGender.Female,
                birthWeightGrams: 3200,
                birthLengthCm: 50,
                headCircumferenceCm: 35,
                apgarScore1Min: 9,
                apgarScore5Min: 10,
                apgarScore10Min: null,
                ResuscitationIntervention.None,
                cordBloodPh: "7.35",
                complicationsNotes: "DEMO-SENSITIVE-CANARY-NEWBORN",
                nowUtc);

            var examination = DentalExaminationRecord.Create(
                Guid.Parse("91000000-0000-0000-0000-000000000004"),
                PatientId,
                encounterId: null,
                DoctorPersonId,
                nowUtc.AddDays(-5),
                chiefComplaint: "DEMO kontrol",
                diagnosisNotes: "DEMO-SENSITIVE-CANARY-DENTAL",
                treatmentPlanSummary: "DEMO dolgu planı",
                nowUtc);
            var procedure = DentalProcedure.Plan(
                Guid.Parse("91000000-0000-0000-0000-000000000005"),
                PatientId,
                encounterId: null,
                toothNumber: 16,
                ToothSurface.Occlusal,
                procedureCode: "DEMO-FILL",
                procedureName: "DEMO Tamamlanmış Dolgu",
                estimatedCost: 750,
                DoctorPersonId,
                scheduledDateUtc: nowUtc.AddDays(-4),
                clinicalNotes: "DEMO-SENSITIVE-CANARY-PROCEDURE",
                nowUtc);
            procedure.Complete(nowUtc.AddDays(-3), "DEMO tamamlandı", nowUtc.AddDays(-3));

            var homeVisit = HomeHealthVisit.Request(
                Guid.Parse("91000000-0000-0000-0000-000000000006"),
                PatientId,
                HomeCareServiceType.GeneralNursing,
                HomeVisitPriority.Routine,
                city: "DEMO İstanbul",
                district: "DEMO Kadıköy",
                addressDetail: "DEMO yalnız görevli adres — DEMO-SENSITIVE-CANARY-ADDRESS",
                contactPhone: "DEMO-SENSITIVE-CANARY-PHONE",
                requestedByStaffId: DoctorPersonId,
                initialNotes: "DEMO-SENSITIVE-CANARY-HOME",
                nowUtc);
            homeVisit.AssignTeam(NursePersonId, nowUtc.AddDays(1), nowUtc);

            var specialtyDb = services.GetRequiredService<SpecialtyCareDbContext>();
            specialtyDb.AddRange(episode, delivery, examination, procedure, homeVisit);
            await specialtyDb.SaveChangesAsync();

            return new Phase9GateData(
                episode.EpisodeProtocolNumber,
                delivery.DeliveryProtocolNumber,
                procedure.ProcedureProtocolNumber,
                homeVisit.ProtocolNumber);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseStaticWebAssets();
            builder.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(_certificate));
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
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-phase9-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-PHASE9-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-PHASE9-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-phase9-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string password = "DEMO-PHASE9-E2E-CERTIFICATE";
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
                generated.Export(X509ContentType.Pkcs12, password),
                password,
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
                .WithDatabase($"hms_phase9_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_phase9_e2e")
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
