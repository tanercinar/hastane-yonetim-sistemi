using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Application;
using HospitalManagement.Modules.Patients.Infrastructure;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

using Testcontainers.PostgreSql;

using static Microsoft.Playwright.Assertions;

namespace HospitalManagement.EndToEndTests;

public sealed partial class Phase3ProductGateEndToEndTests
{
    private const string SecondPatientEmail = "DEMO-patient2@hospital.invalid";
    private const string SecondPatientPassword = "DEMO-Patient2-Pass!1";

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F03-KAPI")]
    public async Task TwoPatientsRaceForOneSlotAndWinnerCanBeCheckedIn()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        using var application = new Phase3BrowserWebApplicationFactory(database.ConnectionString);
        var baseAddress = application.StartAndGetBaseAddress();
        var gateData = await application.PreparePhase3GateAsync();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });

        var patientDefinition = IdentityDataSeeder.DemoUsers.Single(user => user.RoleName == HospitalRoles.Patient);
        var receptionistDefinition = IdentityDataSeeder.DemoUsers.Single(user => user.RoleName == HospitalRoles.RegistrationStaff);
        var pageErrors = new ConcurrentQueue<string>();

        await using var firstPatientContext = await CreateBrowserContextAsync(
            browser,
            baseAddress,
            width: 1280,
            height: 900);
        await using var secondPatientContext = await CreateBrowserContextAsync(
            browser,
            baseAddress,
            width: 390,
            height: 844);

        var firstPatientPage = await firstPatientContext.NewPageAsync();
        var secondPatientPage = await secondPatientContext.NewPageAsync();
        firstPatientPage.PageError += (_, error) => pageErrors.Enqueue(error);
        secondPatientPage.PageError += (_, error) => pageErrors.Enqueue(error);

        await LoginAsync(firstPatientPage, patientDefinition.Email, patientDefinition.Password);
        await LoginAsync(secondPatientPage, SecondPatientEmail, SecondPatientPassword);
        await SelectAppointmentSlotAsync(firstPatientPage, gateData.SlotDate, gateData.SlotTime, "E2E yarış hastası 1");
        await SelectAppointmentSlotAsync(secondPatientPage, gateData.SlotDate, gateData.SlotTime, "E2E yarış hastası 2");

        var bookingResponses = await Task.WhenAll(
            SubmitAppointmentAsync(firstPatientPage),
            SubmitAppointmentAsync(secondPatientPage));
        Assert.Equal(
            [(int)HttpStatusCode.Created, (int)HttpStatusCode.Conflict],
            bookingResponses.Order().ToArray());

        var firstWon = bookingResponses[0] == (int)HttpStatusCode.Created;
        var winningPage = firstWon ? firstPatientPage : secondPatientPage;
        await Expect(winningPage.GetByRole(AriaRole.Heading, new()
        {
            Name = "Randevunuz Başarıyla Oluşturuldu",
            Exact = true,
        })).ToBeVisibleAsync();

        var losingPage = firstWon ? secondPatientPage : firstPatientPage;
        var winnerReason = firstWon ? "E2E yarış hastası 1" : "E2E yarış hastası 2";
        await Expect(losingPage.GetByRole(AriaRole.Alert)).ToContainTextAsync("başka bir kullanıcı tarafından alınmış olabilir");

        var mobilePage = secondPatientPage;
        var mobileWidths = await mobilePage.EvaluateAsync<int[]>(
            "() => [document.documentElement.scrollWidth, window.innerWidth]");
        var overflowingElements = await mobilePage.EvaluateAsync<string[]>(
            "() => Array.from(document.querySelectorAll('*'))"
            + ".filter(element => element.getBoundingClientRect().right > window.innerWidth + 1)"
            + ".slice(0, 8)"
            + ".map(element => `${element.tagName.toLowerCase()}.${element.className}`)");
        Assert.True(
            mobileWidths[0] <= mobileWidths[1] + 1,
            $"Mobile document width {mobileWidths[0]} exceeded viewport {mobileWidths[1]}. "
            + $"Overflowing elements: {string.Join(", ", overflowingElements)}");

        await using var receptionistContext = await CreateBrowserContextAsync(
            browser,
            baseAddress,
            width: 1280,
            height: 900);
        var receptionistPage = await receptionistContext.NewPageAsync();
        receptionistPage.PageError += (_, error) => pageErrors.Enqueue(error);

        await LoginAsync(receptionistPage, receptionistDefinition.Email, receptionistDefinition.Password);
        await receptionistPage.GotoAsync("/staff/queue", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await receptionistPage.GetByLabel("Tarih", new()
        {
            Exact = true
        }).FillAsync(
            gateData.SlotDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var winningRow = receptionistPage.GetByRole(AriaRole.Row).Filter(new()
        {
            HasTextString = winnerReason,
        });
        await Expect(winningRow).ToBeVisibleAsync();
        await winningRow.GetByRole(AriaRole.Button, new()
        {
            Name = "Giriş Yap",
            Exact = true
        }).ClickAsync();
        await Expect(receptionistPage.GetByRole(AriaRole.Status)).ToContainTextAsync("Hasta girişi yapıldı. Sıra Numarası:");
        await Expect(winningRow).ToContainTextAsync("Giriş Yapıldı");

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
            ViewportSize = new ViewportSize
            {
                Width = width,
                Height = height,
            },
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

    private static async Task SelectAppointmentSlotAsync(
        IPage page,
        DateOnly slotDate,
        string slotTime,
        string reason)
    {
        await page.GotoAsync("/patient/appointments/book", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.GetByLabel("Tarih", new()
        {
            Exact = true
        }).FillAsync(
            slotDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        var slotButton = page.GetByRole(AriaRole.Button, new()
        {
            Name = slotTime,
            Exact = true
        }).First;
        await Expect(slotButton).ToBeVisibleAsync();
        await slotButton.ClickAsync();
        await page.GetByLabel("Geliş Şikayeti / Ön Bilgi (Opsiyonel)", new()
        {
            Exact = true
        }).FillAsync(reason);
    }

    private static async Task<int> SubmitAppointmentAsync(IPage page)
    {
        var response = await page.RunAndWaitForResponseAsync(
            () => page.GetByRole(AriaRole.Button, new()
            {
                Name = "Randevuyu Onayla",
                Exact = true,
            }).ClickAsync(),
            response => response.Request.Method == "POST"
                && response.Url.EndsWith("/api/v1/scheduling/appointments/book", StringComparison.Ordinal));

        return response.Status;
    }

    private sealed class Phase3BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly X509Certificate2 _certificate;

        public Phase3BrowserWebApplicationFactory(string connectionString)
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

        public async Task<Phase3GateData> PreparePhase3GateAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PatientsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<IPatientDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<ISchedulingDataSeeder>().SeedAsync();

            var secondPatient = PatientDataSeeder.DemoPatients.Single(patient => patient.Email == SecondPatientEmail);
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var secondUser = ApplicationUser.CreatePatient(
                Guid.Parse("00000000-0000-0000-0000-000000000092"),
                secondPatient.PersonId,
                SecondPatientEmail,
                DateTime.UtcNow);
            secondUser.ConfirmPatientEmail();
            var createResult = await userManager.CreateAsync(secondUser, SecondPatientPassword);
            Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Code)));
            var roleResult = await userManager.AddToRoleAsync(secondUser, HospitalRoles.Patient);
            Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Code)));

            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var targetSlot = await schedulingDb.AppointmentSlots
                .AsNoTracking()
                .Where(slot => slot.Status == SlotStatus.Available)
                .OrderBy(slot => slot.StartUtc)
                .FirstAsync();

            return new Phase3GateData(
                DateOnly.FromDateTime(targetSlot.StartUtc),
                TimeZoneInfo.ConvertTimeFromUtc(targetSlot.StartUtc, FindIstanbulTimeZone())
                    .ToString("HH:mm", CultureInfo.InvariantCulture));
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
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-phase3-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-PHASE3-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-PHASE3-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-phase3-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string certificatePassword = "DEMO-PHASE3-E2E-CERTIFICATE";
            using var privateKey = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=localhost",
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new("1.3.6.1.5.5.7.3.1") },
                critical: false));
            using var generated = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddHours(1));
            return X509CertificateLoader.LoadPkcs12(
                generated.Export(X509ContentType.Pkcs12, certificatePassword),
                certificatePassword,
                X509KeyStorageFlags.Exportable);
        }
    }

    private sealed record Phase3GateData(DateOnly SlotDate, string SlotTime);

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
                .WithDatabase($"hms_phase3_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_phase3_e2e")
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

    private static TimeZoneInfo FindIstanbulTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    [GeneratedRegex("/account$")]
    private static partial Regex AccountUrlPattern();
}
