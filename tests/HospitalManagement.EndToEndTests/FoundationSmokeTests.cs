using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace HospitalManagement.EndToEndTests;

public sealed partial class FoundationSmokeTests
{
    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Roadmap", "F01-G10")]
    public async Task UserCanOpenDashboardAndReviewInterfaceStatesInChromium()
    {
        using var application = new BrowserWebApplicationFactory();
        var baseAddress = application.StartAndGetBaseAddress();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
        await using var browserContext = await browser.NewContextAsync(new()
        {
            BaseURL = baseAddress.AbsoluteUri,
            Locale = "tr-TR",
            ViewportSize = new ViewportSize
            {
                Width = 390,
                Height = 844,
            },
        });
        var page = await browserContext.NewPageAsync();
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);

        var response = await page.GotoAsync("/", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, (HttpStatusCode)response.Status);
        await Expect(page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Hastane operasyonları, tek ve anlaşılır bir görünümde.",
            Exact = true,
        })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new()
        {
            Name = "Durum bileşenlerini incele",
            Exact = true,
        }).ClickAsync();

        await Expect(page).ToHaveURLAsync(InterfaceStatesUrlPattern());
        await Expect(page.GetByRole(AriaRole.Heading, new()
        {
            Name = "Her veri durumu için aynı, erişilebilir dil.",
            Exact = true,
        })).ToBeVisibleAsync();
        Assert.Equal(3, await page.Locator("[role='status']").CountAsync());
        Assert.Equal(1, await page.Locator("[role='alert']").CountAsync());
        Assert.Empty(pageErrors);
    }

    [GeneratedRegex("/ui-states$")]
    private static partial Regex InterfaceStatesUrlPattern();

    private sealed class BrowserWebApplicationFactory : WebApplicationFactory<Program>
    {
        public BrowserWebApplicationFactory()
        {
            UseKestrel(0);
        }

        public Uri StartAndGetBaseAddress()
        {
            StartServer();
            var server = Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
                ?? throw new InvalidOperationException("Kestrel did not expose a listening address.");
            var address = addresses.Single(value => value.StartsWith("http://", StringComparison.Ordinal));
            return new Uri(address, UriKind.Absolute);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseStaticWebAssets();
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(CreateSettings());
            });
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }

        private static Dictionary<string, string?> CreateSettings()
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["HospitalManagement:Runtime:DataMode"] = "DEMO",
                ["HospitalManagement:Runtime:EmbeddedArtificialIntelligenceEnabled"] = "false",
                ["HospitalManagement:Api:RateLimit:PermitLimit"] = "120",
                ["HospitalManagement:Api:RateLimit:WindowSeconds"] = "60",
                ["ConnectionStrings:HospitalDatabase"] = "Host=127.0.0.1;Port=5432;Database=demo_e2e;Username=demo_e2e;Password=DEMO-E2E-DATABASE",
                ["HospitalManagement:ObjectStorage:Endpoint"] = "http://127.0.0.1:9000",
                ["HospitalManagement:ObjectStorage:UseTls"] = "false",
                ["HospitalManagement:ObjectStorage:BucketName"] = "demo-e2e-documents",
                ["HospitalManagement:ObjectStorage:AccessKey"] = "DEMO-E2E-ACCESS",
                ["HospitalManagement:ObjectStorage:SecretKey"] = "DEMO-E2E-OBJECT-SECRET",
                ["HospitalManagement:Email:Mode"] = "MOCK",
                ["HospitalManagement:Email:Host"] = "127.0.0.1",
                ["HospitalManagement:Email:Port"] = "1025",
                ["HospitalManagement:Email:SenderAddress"] = "DEMO-e2e@hospital.invalid",
            };
        }
    }
}
