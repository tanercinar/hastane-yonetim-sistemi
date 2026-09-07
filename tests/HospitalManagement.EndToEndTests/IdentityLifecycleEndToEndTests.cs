using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

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

using Xunit.Abstractions;

using static Microsoft.Playwright.Assertions;

namespace HospitalManagement.EndToEndTests;

public sealed partial class IdentityLifecycleEndToEndTests
{
    private const string PatientEmail = "DEMO-browser-patient@hospital.invalid";
    private const string InitialPassword = "DEMO-Browser-Password!1";
    private const string ReplacementPassword = "DEMO-Browser-Replacement!2";
    private readonly ITestOutputHelper _output;

    public IdentityLifecycleEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F02-G02")]
    public async Task PatientCanRegisterConfirmLoginResetPasswordAndLoginAgainInChromium()
    {
        await using var database = await BrowserPostgreSqlDatabase.StartAsync();
        var messages = new BrowserIdentityMessageSender();
        using var application = new IdentityBrowserWebApplicationFactory(
            database.ConnectionString,
            messages);
        var baseAddress = application.StartAndGetBaseAddress();
        await application.MigrateIdentityAsync();
        using (var probeHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        using (var probeClient = new HttpClient(probeHandler) { BaseAddress = baseAddress })
        using (var probeResponse = await probeClient.GetAsync("/account/register"))
        {
            Assert.Equal(HttpStatusCode.OK, probeResponse.StatusCode);
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        await using var browserContext = await browser.NewContextAsync(new()
        {
            BaseURL = baseAddress.AbsoluteUri,
            IgnoreHTTPSErrors = true,
            Locale = "tr-TR",
            ViewportSize = new ViewportSize
            {
                Width = 390,
                Height = 844,
            },
        });
        var page = await browserContext.NewPageAsync();
        var pageErrors = new List<string>();
        page.PageError += (_, error) =>
        {
            pageErrors.Add(error);
            _output.WriteLine($"Browser page error: {error}");
        };
        page.Console += (_, message) =>
        {
            if (message.Type is "error")
            {
                _output.WriteLine($"Browser console error: {message.Text}");
            }
        };
        page.RequestFailed += (_, request) =>
            _output.WriteLine($"Browser request failed: {request.Method} {request.Url} {request.Failure}");

        var registrationPage = await page.GotoAsync("/account/register", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        Assert.NotNull(registrationPage);
        Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)registrationPage.Status);

        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(PatientEmail);
        await page.GetByLabel("Parola", new()
        {
            Exact = true
        }).FillAsync(InitialPassword);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Hesap oluştur",
            Exact = true
        }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("işlem kodu MOCK teslim kanalına");
        Assert.Equal(string.Empty, await page.GetByLabel("Parola", new()
        {
            Exact = true
        }).InputValueAsync());

        var confirmationCode = messages.Latest(
            IdentityMessageKind.PatientEmailConfirmation,
            PatientEmail).ActionCode;
        await page.GetByRole(AriaRole.Link, new()
        {
            Name = "Doğrulama kodunu gir",
            Exact = true
        }).ClickAsync();
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(PatientEmail);
        await page.GetByLabel("Doğrulama kodu", new()
        {
            Exact = true
        }).FillAsync(confirmationCode);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Hesabı doğrula",
            Exact = true
        }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("İşlem tamamlandı.");
        Assert.Equal(string.Empty, await page.GetByLabel("Doğrulama kodu", new()
        {
            Exact = true
        }).InputValueAsync());

        await page.GetByRole(AriaRole.Link, new()
        {
            Name = "Giriş yap",
            Exact = true
        }).ClickAsync();
        await LoginAsync(page, InitialPassword);
        await page.GotoAsync("/account", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Expect(page).ToHaveURLAsync(AccountUrlPattern());
        await Expect(page.Locator("main").GetByText(PatientEmail, new()
        {
            Exact = true
        })).ToBeVisibleAsync();
        await Expect(page.GetByText("Patient", new()
        {
            Exact = true
        })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Çıkış yap",
            Exact = true
        }).ClickAsync();
        await Expect(page).ToHaveURLAsync(LoginUrlPattern());
        await page.GotoAsync("/account/forgot-password", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(PatientEmail);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Kod iste",
            Exact = true
        }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync("işlem kodu MOCK teslim kanalına");

        var resetCode = messages.Latest(IdentityMessageKind.PasswordReset, PatientEmail).ActionCode;
        await page.GetByRole(AriaRole.Link, new()
        {
            Name = "Sıfırlama kodunu gir",
            Exact = true
        }).ClickAsync();
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(PatientEmail);
        await page.GetByLabel("Sıfırlama kodu", new()
        {
            Exact = true
        }).FillAsync(resetCode);
        await page.GetByLabel("Yeni parola", new()
        {
            Exact = true
        }).FillAsync(ReplacementPassword);
        await page.GetByRole(AriaRole.Button, new()
        {
            Name = "Parolayı değiştir",
            Exact = true
        }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status)).ToHaveTextAsync("İşlem tamamlandı.");
        Assert.Equal(string.Empty, await page.GetByLabel("Sıfırlama kodu", new()
        {
            Exact = true
        }).InputValueAsync());
        Assert.Equal(string.Empty, await page.GetByLabel("Yeni parola", new()
        {
            Exact = true
        }).InputValueAsync());

        await page.GetByRole(AriaRole.Link, new()
        {
            Name = "Giriş yap",
            Exact = true
        }).ClickAsync();
        await LoginAsync(page, ReplacementPassword);
        await page.GotoAsync("/account", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await Expect(page).ToHaveURLAsync(AccountUrlPattern());
        await Expect(page.Locator("main").GetByText(PatientEmail, new()
        {
            Exact = true
        })).ToBeVisibleAsync();
        Assert.DoesNotContain(
            await page.EvaluateAsync<string[]>("() => Object.keys(localStorage)"),
            IsIdentityStorageKey);
        Assert.DoesNotContain(
            await page.EvaluateAsync<string[]>("() => Object.keys(sessionStorage)"),
            IsIdentityStorageKey);
        Assert.Empty(pageErrors);
    }

    private static bool IsIdentityStorageKey(string key) =>
        key.Contains("auth", StringComparison.OrdinalIgnoreCase)
        || key.Contains("identity", StringComparison.OrdinalIgnoreCase)
        || key.Contains("token", StringComparison.OrdinalIgnoreCase);

    private static async Task LoginAsync(IPage page, string password)
    {
        await page.GetByLabel("DEMO e-posta", new()
        {
            Exact = true
        }).FillAsync(PatientEmail);
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

    [GeneratedRegex("/account$")]
    private static partial Regex AccountUrlPattern();

    [GeneratedRegex("/account/login$")]
    private static partial Regex LoginUrlPattern();

    private sealed class IdentityBrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly BrowserIdentityMessageSender _messages;
        private readonly X509Certificate2 _certificate;

        public IdentityBrowserWebApplicationFactory(
            string connectionString,
            BrowserIdentityMessageSender messages)
        {
            _connectionString = connectionString;
            _messages = messages;
            _certificate = CreateTestCertificate();
            UseKestrel();
        }

        public Uri StartAndGetBaseAddress()
        {
            StartServer();
            var server = Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
                ?? throw new InvalidOperationException("Kestrel did not expose a listening address.");
            var address = addresses.Single(value => value.StartsWith("https://", StringComparison.Ordinal));
            return new Uri(address, UriKind.Absolute);
        }

        public async Task MigrateIdentityAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseStaticWebAssets();
            builder.ConfigureKestrel(options =>
            {
                options.Listen(
                    IPAddress.Loopback,
                    0,
                    listenOptions => listenOptions.UseHttps(_certificate));
            });
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(CreateSettings(_connectionString));
            });
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.RemoveAll<IIdentityMessageSender>();
                services.AddSingleton<IIdentityMessageSender>(_messages);
            });
        }

        private static Dictionary<string, string?> CreateSettings(string databaseConnectionString) =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["HospitalManagement:Runtime:DataMode"] = "DEMO",
                ["HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled"] = "false",
                ["HospitalManagement:Api:RateLimit:PermitLimit"] = "120",
                ["HospitalManagement:Api:RateLimit:WindowSeconds"] = "60",
                ["HospitalManagement:Identity:SensitivePermitLimit"] = "100",
                ["ConnectionStrings:HospitalDatabase"] = databaseConnectionString,
                ["HospitalManagement:ObjectStorage:Endpoint"] = "http://127.0.0.1:9000",
                ["HospitalManagement:ObjectStorage:UseTls"] = "false",
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-identity-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-IDENTITY-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-IDENTITY-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-identity-e2e@hospital.invalid",
            };

        private static X509Certificate2 CreateTestCertificate()
        {
            const string CertificatePassword = "DEMO-E2E-CERTIFICATE";
            using var privateKey = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=localhost",
                privateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature,
                critical: true));
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("localhost");
            names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new("1.3.6.1.5.5.7.3.1") },
                critical: false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(
                request.PublicKey,
                critical: false));
            using var generated = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddHours(1));
            return X509CertificateLoader.LoadPkcs12(
                generated.Export(X509ContentType.Pkcs12, CertificatePassword),
                CertificatePassword,
                X509KeyStorageFlags.Exportable);
        }
    }

    private sealed class BrowserIdentityMessageSender : IIdentityMessageSender
    {
        private readonly ConcurrentQueue<IdentityMessage> _messages = new();

        public Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _messages.Enqueue(message);
            return Task.CompletedTask;
        }

        public IdentityMessage Latest(IdentityMessageKind kind, string recipient) =>
            _messages.Last(message =>
                message.Kind == kind
                && string.Equals(message.RecipientAddress, recipient, StringComparison.OrdinalIgnoreCase));
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
                .WithDatabase($"hms_e2e_{Guid.NewGuid():N}")
                .WithUsername("hms_identity_e2e")
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
