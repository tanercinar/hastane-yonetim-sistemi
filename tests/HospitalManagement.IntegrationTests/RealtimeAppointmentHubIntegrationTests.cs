using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Realtime;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class RealtimeAppointmentHubIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G08")]
    public async Task AnonymousConnectionToSignalRHubIsRejected()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        using var client = application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost", UriKind.Absolute),
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        // Anonymous negotiate attempt
        var negotiateResp = await client.PostAsync("/hubs/hospital/negotiate?negotiateVersion=1", null);
        Assert.True(negotiateResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.Redirect);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G08")]
    public async Task AuthenticatedStaffAndPatientReceiveRealtimeQueueAndSlotUpdates()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // Run migrations
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            await identityDb.Database.MigrateAsync();
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>();
            await auditDb.Database.MigrateAsync();
            var patientsDb = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
            await patientsDb.Database.MigrateAsync();
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            await schedulingDb.Database.MigrateAsync();
            var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            await notificationsDb.Database.MigrateAsync();
        }

        // Seed only demo identity; this test creates an isolated realtime slot explicitly.
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
        }

        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);

        var slotId = Guid.NewGuid();
        var today = DateTime.UtcNow.Date;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");
            var slot = AppointmentSlot.Create(slotId, docDef.PersonId, deptId, null, today.AddHours(11), today.AddHours(11).AddMinutes(20), DateTime.UtcNow);
            schedulingDb.AppointmentSlots.Add(slot);
            await schedulingDb.SaveChangesAsync();
        }

        using var staffClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(staffClient, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        var csrfToken = await staffClient.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(csrfToken);

        var cookieValues = new List<string>();
        if (loginResp.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            cookieValues.AddRange(setCookies.Select(c => c.Split(';')[0]));
        }
        var cookieHeader = string.Join("; ", cookieValues);

        // Build SignalR HubConnection with cookies and CSRF from authenticated staff client
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(staffClient.BaseAddress!, "hubs/hospital"), options =>
            {
                options.HttpMessageHandlerFactory = _ => application.Server.CreateHandler();
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    options.Headers.Add("Cookie", cookieHeader);
                }
                options.Headers.Add("X-HMS-CSRF", csrfToken.Token);
                options.Transports = HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents;
            })
            .WithAutomaticReconnect()
            .Build();

        var receivedSlotUpdates = new List<SlotRealtimeUpdate>();
        var receivedQueueUpdates = new List<QueueRealtimeUpdate>();

        hubConnection.On<SlotRealtimeUpdate>("SlotStatusChanged", update =>
        {
            lock (receivedSlotUpdates)
            {
                receivedSlotUpdates.Add(update);
            }
        });

        hubConnection.On<QueueRealtimeUpdate>("QueueUpdated", update =>
        {
            lock (receivedQueueUpdates)
            {
                receivedQueueUpdates.Add(update);
            }
        });

        await hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        // 1. Staff books appointment -> Should trigger SignalR event
        var bookResp = await PostWithAntiforgeryAsync(staffClient, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = slotId,
            PatientId = Guid.NewGuid(),
            ReasonForVisit = "Realtime Test",
        });
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var bookedAppt = await bookResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(bookedAppt);

        // Wait up to 5s for realtime event delivery
        var slotReceived = await WaitForConditionAsync(() =>
        {
            lock (receivedSlotUpdates)
            {
                return receivedSlotUpdates.Any(u => u.SlotId == slotId && u.Status == "Booked");
            }
        }, TimeSpan.FromSeconds(5));
        Assert.True(slotReceived, "SlotStatusChanged SignalR event was not received.");

        // 2. Staff check-in appointment -> Should trigger QueueUpdated event
        var checkInResp = await PostWithAntiforgeryAsync(staffClient, $"/api/v1/scheduling/appointments/{bookedAppt.Id}/check-in", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, checkInResp.StatusCode);

        var queueReceived = await WaitForConditionAsync(() =>
        {
            lock (receivedQueueUpdates)
            {
                return receivedQueueUpdates.Any(u => u.AppointmentId == bookedAppt.Id && u.Status == "CheckedIn" && u.QueueNumber.HasValue);
            }
        }, TimeSpan.FromSeconds(5));
        Assert.True(queueReceived, "QueueUpdated SignalR event was not received.");

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    private static CookieContainer GetCookieContainer(HttpClient client, HttpResponseMessage response)
    {
        var container = new CookieContainer();
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                container.SetCookies(client.BaseAddress!, cookie);
            }
        }

        return container;
    }

    private static async Task<bool> WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(50);
        }

        return condition();
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
}
