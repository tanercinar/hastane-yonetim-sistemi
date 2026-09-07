using System.Net;
using System.Net.Http.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Notifications;
using HospitalManagement.Contracts.Patients;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class Phase3ProductGateIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-KAPI")]
    public async Task Phase3CompleteProductFlowPatientRegistrationDoubleBookingRaceConditionCheckInNotificationsAndSignalRPassesGate()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        var messages = new InMemoryIdentityMessageSender();

        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: messages);

        // 1. Run all migrations across all monolith schemas
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

        // 2. Seed demo identity & scheduling data
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
            var schedulingSeeder = scope.ServiceProvider.GetRequiredService<ISchedulingDataSeeder>();
            await schedulingSeeder.SeedAsync();
        }

        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);
        var docDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var pat1Def = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);

        // Create distinct second patient user
        var pat2PersonId = Guid.Parse("00000000-0000-0000-0000-000000000299");
        var pat2Email = "DEMO-patient2@hospital.invalid";
        var pat2Password = "DEMO-Patient2-Pass!1";

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user2 = ApplicationUser.CreatePatient(Guid.NewGuid(), pat2PersonId, pat2Email, DateTime.UtcNow);
            user2.ConfirmPatientEmail();
            await userManager.CreateAsync(user2, pat2Password);
            await userManager.AddToRoleAsync(user2, HospitalRoles.Patient);
        }

        // 3. Staff registers a new patient in Patients module (F03-G01, F03-G02)
        using var staffClient = CreateSecureClient(application);
        var staffLoginResp = await LoginAsync(staffClient, regDef.Email, regDef.Password);
        Assert.Equal(HttpStatusCode.OK, staffLoginResp.StatusCode);

        var regPatientResp = await PostWithAntiforgeryAsync(staffClient, "/api/v1/patients", new PatientRegistrationRequest
        {
            NationalIdSynthetic = "12345678901",
            FirstName = "Ali",
            LastName = "Yılmaz",
            DateOfBirth = new DateOnly(1985, 5, 20),
            Gender = "Male",
            Email = "DEMO-ali.yilmaz@hospital.invalid",
            PhoneNumber = "+905551112233",
        });
        Assert.Equal(HttpStatusCode.Created, regPatientResp.StatusCode);
        var createdPatient = await regPatientResp.Content.ReadFromJsonAsync<PatientDetailResponse>();
        Assert.NotNull(createdPatient);
        Assert.StartsWith("MRN-", createdPatient.MedicalRecordNumber, StringComparison.Ordinal);

        // 4. Retrieve seeded available doctor slot (F03-G03)
        Guid targetSlotId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var seededSlot = await schedulingDb.AppointmentSlots
                .FirstAsync(s => s.DoctorId == docDef.PersonId && s.Status == SlotStatus.Available);
            targetSlotId = seededSlot.Id;
        }

        // 5. Anonymous callers cannot enumerate or mutate appointment resources.
        using (var anonymousClient = CreateSecureClient(application))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var anonymousAvailability = await anonymousClient.GetAsync(
                $"/api/v1/scheduling/availability/by-doctor/{docDef.PersonId}?startDate={today:O}&endDate={today.AddDays(7):O}");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousAvailability.StatusCode);

            var anonymousBook = await PostWithAntiforgeryAsync(
                anonymousClient,
                "/api/v1/scheduling/appointments/book",
                new BookAppointmentRequest
                {
                    SlotId = targetSlotId,
                    PatientId = pat1Def.PersonId,
                    ReasonForVisit = "Yetkisiz deneme",
                });
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousBook.StatusCode);
        }

        using (var doctorClient = CreateSecureClient(application))
        {
            await LoginAsync(doctorClient, docDef.Email, docDef.Password);
            var crossDoctorSchedule = await doctorClient.GetAsync(
                $"/api/v1/scheduling/schedules/by-doctor/{Guid.Parse("00000000-0000-0000-0000-000000000777")}");
            Assert.Equal(HttpStatusCode.Forbidden, crossDoctorSchedule.StatusCode);
        }

        // 6. Connect SignalR Hub for Staff (F03-G08)
        var staffCsrf = await staffClient.GetFromJsonAsync<AntiforgeryTokenResponse>("/api/v1/identity/antiforgery");
        Assert.NotNull(staffCsrf);

        var cookieValues = new List<string>();
        if (staffLoginResp.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            cookieValues.AddRange(setCookies.Select(c => c.Split(';')[0]));
        }
        var cookieHeader = string.Join("; ", cookieValues);

        var staffHub = new HubConnectionBuilder()
            .WithUrl(new Uri(staffClient.BaseAddress!, "hubs/hospital"), options =>
            {
                options.HttpMessageHandlerFactory = _ => application.Server.CreateHandler();
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    options.Headers.Add("Cookie", cookieHeader);
                }
                options.Headers.Add("X-HMS-CSRF", staffCsrf.Token);
                options.Transports = HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents;
            })
            .WithAutomaticReconnect()
            .Build();

        var receivedSlotUpdates = new List<SlotRealtimeUpdate>();
        var receivedQueueUpdates = new List<QueueRealtimeUpdate>();

        staffHub.On<SlotRealtimeUpdate>("SlotStatusChanged", update =>
        {
            lock (receivedSlotUpdates)
            {
                receivedSlotUpdates.Add(update);
            }
        });

        staffHub.On<QueueRealtimeUpdate>("QueueUpdated", update =>
        {
            lock (receivedQueueUpdates)
            {
                receivedQueueUpdates.Add(update);
            }
        });

        await staffHub.StartAsync();
        Assert.Equal(HubConnectionState.Connected, staffHub.State);

        // 7. Double-booking race condition gate (F03-G04, F03-KAPI)
        // Two patients race to book the exact same slot concurrently
        using var pat1Client = CreateSecureClient(application);
        await LoginAsync(pat1Client, pat1Def.Email, pat1Def.Password);

        using var pat2Client = CreateSecureClient(application);
        await LoginAsync(pat2Client, pat2Email, pat2Password);

        Guid ownershipProbeSlotId;
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            ownershipProbeSlotId = await schedulingDb.AppointmentSlots
                .Where(s => s.Id != targetSlotId && s.Status == SlotStatus.Available)
                .Select(s => s.Id)
                .FirstAsync();
        }

        var crossPatientBook = await PostWithAntiforgeryAsync(
            pat1Client,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = ownershipProbeSlotId,
                PatientId = pat2PersonId,
                ReasonForVisit = "Başka hasta adına deneme",
            });
        Assert.Equal(HttpStatusCode.Forbidden, crossPatientBook.StatusCode);

        var bookTask1 = PostWithAntiforgeryAsync(pat1Client, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = targetSlotId,
            PatientId = pat1Def.PersonId,
            ReasonForVisit = "Kardiyoloji Kontrolü",
        });

        var bookTask2 = PostWithAntiforgeryAsync(pat2Client, "/api/v1/scheduling/appointments/book", new BookAppointmentRequest
        {
            SlotId = targetSlotId,
            PatientId = pat2PersonId,
            ReasonForVisit = "Genel Muayene",
        });

        var responses = await Task.WhenAll(bookTask1, bookTask2);

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        var winningResp = responses.First(r => r.StatusCode == HttpStatusCode.Created);
        var bookedAppt = await winningResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(bookedAppt);
        Assert.Equal("Confirmed", bookedAppt.Status);

        // Availability contract exposes only bookable slots and never another patient's identifier.
        var todayForAvailability = DateOnly.FromDateTime(DateTime.UtcNow);
        var safeAvailability = await pat1Client.GetFromJsonAsync<IReadOnlyList<DoctorAvailabilityDayResponse>>(
            $"/api/v1/scheduling/availability/by-doctor/{docDef.PersonId}?startDate={todayForAvailability:O}&endDate={todayForAvailability.AddDays(14):O}");
        Assert.NotNull(safeAvailability);
        Assert.All(safeAvailability.SelectMany(day => day.Slots), slot =>
        {
            Assert.Equal("Available", slot.Status);
            Assert.Null(slot.HeldByPersonId);
        });

        // 8. Check-In & Queue Generation by Staff (F03-G06)
        var checkInResp = await PostWithAntiforgeryAsync(staffClient, $"/api/v1/scheduling/appointments/{bookedAppt.Id}/check-in", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, checkInResp.StatusCode);
        var checkedInAppt = await checkInResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(checkedInAppt);
        Assert.Equal("CheckedIn", checkedInAppt.Status);
        Assert.True(checkedInAppt.QueueNumber > 0, "Queue number must be positive sequential integer.");

        // 9. Verify Outbox Notifications & Delivery (F03-G07)
        var winningPatientClient = bookedAppt.PatientId == pat1Def.PersonId ? pat1Client : pat2Client;
        var myNotifs = await winningPatientClient.GetFromJsonAsync<List<NotificationDetailResponse>>("/api/v1/notifications/my");
        Assert.NotNull(myNotifs);
        Assert.NotEmpty(myNotifs);

        var firstNotif = myNotifs.First();
        Assert.False(string.IsNullOrWhiteSpace(firstNotif.Title));

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var notificationDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var expectedRecipient = bookedAppt.PatientId == pat1Def.PersonId ? pat1Def.Email : pat2Email;
            var appointmentDeliveries = await notificationDb.MockDeliveries
                .Where(delivery => delivery.IdempotencyKey.Contains(bookedAppt.Id.ToString()))
                .ToListAsync();
            Assert.NotEmpty(appointmentDeliveries);
            Assert.All(appointmentDeliveries, delivery => Assert.Equal(expectedRecipient, delivery.Recipient));
        }

        // Mark notification as read
        var readResp = await PostWithAntiforgeryAsync(winningPatientClient, $"/api/v1/notifications/{firstNotif.Id}/read", new
        {
        });
        Assert.Equal(HttpStatusCode.OK, readResp.StatusCode);

        // 10. IDOR Negative Security Check: Losing patient cannot cancel winner's appointment and cannot view winner's appointments
        var losingPatientClient = bookedAppt.PatientId == pat1Def.PersonId ? pat2Client : pat1Client;
        var unauthorizedCancel = await PostWithAntiforgeryAsync(losingPatientClient, $"/api/v1/scheduling/appointments/{bookedAppt.Id}/cancel", new CancelAppointmentRequest
        {
            Reason = "Unauthorized attempt",
        });
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedCancel.StatusCode);

        var unauthorizedList = await losingPatientClient.GetAsync($"/api/v1/scheduling/appointments/by-patient/{bookedAppt.PatientId}");
        Assert.True(unauthorizedList.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);

        await staffHub.StopAsync();
        await staffHub.DisposeAsync();
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
