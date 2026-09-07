using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Notifications;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.IntegrationTests.Infrastructure;
using HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;
using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Notifications.Infrastructure.Persistence;
using HospitalManagement.Modules.Organization.Infrastructure.Persistence;
using HospitalManagement.Modules.Patients.Infrastructure.Persistence;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalManagement.IntegrationTests;

public sealed class NotificationInfrastructureIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-KAPI")]
    public async Task MockProviderFailureDoesNotRollBackBookedAppointment()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: new InMemoryIdentityMessageSender(),
            notificationTransport: new AlwaysFailingNotificationTransport());

        await using (var scope = application.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<PatientsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<ISchedulingDataSeeder>().SeedAsync();
        }

        var patient = IdentityDataSeeder.DemoUsers.Single(user => user.RoleName == HospitalRoles.Patient);
        var doctor = IdentityDataSeeder.DemoUsers.First(user => user.RoleName == HospitalRoles.Doctor);
        var slotId = Guid.NewGuid();
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            schedulingDb.AppointmentSlots.Add(AppointmentSlot.Create(
                slotId,
                doctor.PersonId,
                Guid.Parse("30000000-0000-0000-0000-000000000003"),
                null,
                DateTime.UtcNow.AddDays(3),
                DateTime.UtcNow.AddDays(3).AddMinutes(20),
                DateTime.UtcNow));
            await schedulingDb.SaveChangesAsync();
        }

        using var client = CreateSecureClient(application);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, patient.Email, patient.Password)).StatusCode);

        var bookingResponse = await PostWithAntiforgeryAsync(
            client,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = patient.PersonId,
                ReasonForVisit = "DEMO entegrasyon dayanıklılık kontrolü",
            });

        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
        var booked = await bookingResponse.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(booked);

        await using var verificationScope = application.Services.CreateAsyncScope();
        var scheduling = verificationScope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var notifications = verificationScope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        Assert.True(await scheduling.Appointments.AnyAsync(appointment => appointment.Id == booked.Id));
        var failedOutbox = await notifications.OutboxEvents.SingleAsync(
            item => item.IdempotencyKey == $"appt-booked-{booked.Id}");
        Assert.Equal("Failed", failedOutbox.Status);
        Assert.Equal(3, failedOutbox.RetryCount);
        Assert.DoesNotContain("DEMO-TRANSPORT-EXCEPTION-CANARY", failedOutbox.Error, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G08")]
    public async Task RecipientManagesOwnExternalChannelPreferenceAndLocale()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: new InMemoryIdentityMessageSender());

        await using (var scope = application.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
        }

        using (var anonymousClient = CreateSecureClient(application))
        {
            var anonymousResponse = await anonymousClient.GetAsync("/api/v1/notifications/preferences");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
        }

        var patient = IdentityDataSeeder.DemoUsers.First(user => user.RoleName == HospitalRoles.Patient);
        using var client = CreateSecureClient(application);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, patient.Email, patient.Password)).StatusCode);

        var initialResponse = await client.GetAsync("/api/v1/notifications/preferences");
        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        using (var initialJson = JsonDocument.Parse(await initialResponse.Content.ReadAsStringAsync()))
        {
            Assert.True(initialJson.RootElement.GetProperty("emailEnabled").GetBoolean());
            Assert.False(initialJson.RootElement.GetProperty("smsEnabled").GetBoolean());
            Assert.Equal("tr-TR", initialJson.RootElement.GetProperty("locale").GetString());
        }

        var updateResponse = await PutWithAntiforgeryAsync(
            client,
            "/api/v1/notifications/preferences",
            new
            {
                EmailEnabled = false,
                SmsEnabled = true,
                Locale = "en-US"
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using (var updatedJson = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync()))
        {
            Assert.False(updatedJson.RootElement.GetProperty("emailEnabled").GetBoolean());
            Assert.True(updatedJson.RootElement.GetProperty("smsEnabled").GetBoolean());
            Assert.Equal("en-US", updatedJson.RootElement.GetProperty("locale").GetString());
        }

        var unsupportedLocale = await PutWithAntiforgeryAsync(
            client,
            "/api/v1/notifications/preferences",
            new
            {
                EmailEnabled = true,
                SmsEnabled = true,
                Locale = "fr-FR"
            });
        Assert.Equal(HttpStatusCode.BadRequest, unsupportedLocale.StatusCode);
    }

    private sealed class AlwaysFailingNotificationTransport : INotificationTransport
    {
        public Task CaptureAsync(
            NotificationProviderMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("DEMO-TRANSPORT-EXCEPTION-CANARY");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F10-G08")]
    public async Task OutboxUsesPreferenceLocaleTemplateAndSafeLocalMockCapture()
    {
        await using var database = await PostgreSqlTestDatabase.StartAsync();
        using var application = new ApiWebApplicationFactory(
            database.ConnectionString,
            identityMessageSender: new InMemoryIdentityMessageSender());

        await using (var scope = application.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<AuditPrivacyDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>().SeedAsync();
        }

        var patient = IdentityDataSeeder.DemoUsers.First(user => user.RoleName == HospitalRoles.Patient);
        using var client = CreateSecureClient(application);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, patient.Email, patient.Password)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await PutWithAntiforgeryAsync(
                client,
                "/api/v1/notifications/preferences",
                new
                {
                    EmailEnabled = true,
                    SmsEnabled = false,
                    Locale = "en-US"
                })).StatusCode);

        const string idempotencyKey = "DEMO-NOTIFY-SAFE-TEMPLATE-1";
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await service.EnqueueOutboxEventAsync(new PublishOutboxEventCommand(
                EventType: "Appointment.Booked",
                IdempotencyKey: idempotencyKey,
                RecipientPersonId: patient.PersonId,
                RecipientEmail: patient.Email,
                RecipientPhone: "+900000000000",
                Subject: "Tanı: DEMO-GIZLI-TANI",
                Message: "Test sonucu: DEMO-GIZLI-SONUC",
                TemplateKey: "Appointment.Booked",
                TemplateTokens: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["appointmentDate"] = "02.09.2026 10:30",
                    ["reference"] = "DEMO-APT-42",
                }));

            var processResult = await service.ProcessOutboxAsync();
            Assert.Equal(1, processResult.ProcessedCount);
            Assert.Equal(0, processResult.FailedCount);
        }

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var captureQuery = scope.ServiceProvider.GetRequiredService<ILocalNotificationCaptureQuery>();
            var captures = await captureQuery.GetByIdempotencyKeyAsync(idempotencyKey);

            var email = Assert.Single(captures);
            Assert.Equal("Email", email.Channel);
            Assert.Equal("en-US", email.Locale);
            Assert.Equal("Appointment.Booked", email.TemplateKey);
            Assert.Equal("MOCK-LOCAL-CAPTURE", email.Provider);
            Assert.Equal(1, email.AttemptCount);
            Assert.Contains("DEMO-APT-42", email.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-GIZLI", email.Subject, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-GIZLI", email.Body, StringComparison.Ordinal);
        }

        Assert.Equal(
            HttpStatusCode.OK,
            (await PutWithAntiforgeryAsync(
                client,
                "/api/v1/notifications/preferences",
                new
                {
                    EmailEnabled = false,
                    SmsEnabled = true,
                    Locale = "tr-TR"
                })).StatusCode);

        const string genericIdempotencyKey = "DEMO-NOTIFY-GENERIC-SAFE-1";
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await service.EnqueueOutboxEventAsync(new PublishOutboxEventCommand(
                EventType: "Diagnostic.ResultReady",
                IdempotencyKey: genericIdempotencyKey,
                RecipientPersonId: patient.PersonId,
                RecipientEmail: patient.Email,
                RecipientPhone: "+900000000000",
                Subject: "Tanı: DEMO-GIZLI-TANI",
                Message: "Test sonucu: DEMO-GIZLI-SONUC",
                TemplateKey: "Diagnostic.ResultReady",
                TemplateTokens: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["diagnosis"] = "DEMO-GIZLI-TANI",
                    ["testResult"] = "DEMO-GIZLI-SONUC",
                }));

            var processResult = await service.ProcessOutboxAsync();
            Assert.Equal(1, processResult.ProcessedCount);
            Assert.Equal(0, processResult.FailedCount);
        }

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var captureQuery = scope.ServiceProvider.GetRequiredService<ILocalNotificationCaptureQuery>();
            var captures = await captureQuery.GetByIdempotencyKeyAsync(genericIdempotencyKey);

            var sms = Assert.Single(captures);
            Assert.Equal("Sms", sms.Channel);
            Assert.Equal("tr-TR", sms.Locale);
            Assert.Equal("Notification.Generic", sms.TemplateKey);
            Assert.Equal("MOCK-LOCAL-CAPTURE", sms.Provider);
            Assert.Contains("güvenli portala", sms.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DEMO-GIZLI", sms.Subject, StringComparison.Ordinal);
            Assert.DoesNotContain("DEMO-GIZLI", sms.Body, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Roadmap", "F03-G07")]
    public async Task AppointmentEventsTriggerOutboxDeliveriesIdempotentlyWithoutSensitiveClinicalLeakage()
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

        // Seed demo identity & scheduling
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await identitySeeder.SeedAsync();
            var schedulingSeeder = scope.ServiceProvider.GetRequiredService<ISchedulingDataSeeder>();
            await schedulingSeeder.SeedAsync();
        }

        var patientDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Patient);
        var doctorDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.Doctor);
        var regDef = IdentityDataSeeder.DemoUsers.First(u => u.RoleName == HospitalRoles.RegistrationStaff);

        // Create a slot for booking
        var slotId = Guid.NewGuid();
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var schedulingDb = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
            var deptId = Guid.Parse("30000000-0000-0000-0000-000000000003");
            var slot = AppointmentSlot.Create(
                slotId,
                doctorDef.PersonId,
                deptId,
                null,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(2).AddMinutes(20),
                DateTime.UtcNow);
            schedulingDb.AppointmentSlots.Add(slot);
            await schedulingDb.SaveChangesAsync();
        }

        using var patientClient = CreateSecureClient(application);
        var loginResp = await LoginAsync(patientClient, patientDef.Email, patientDef.Password);
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);

        // 1. Hasta randevu alır
        var bookResp = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/scheduling/appointments/book",
            new BookAppointmentRequest
            {
                SlotId = slotId,
                PatientId = patientDef.PersonId,
                ReasonForVisit = "Kardiyoloji Kontrol",
            });

        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        var bookedAppt = await bookResp.Content.ReadFromJsonAsync<AppointmentDetailResponse>();
        Assert.NotNull(bookedAppt);

        // 2. Outbox ve Teslimat Tablolarını Doğrula
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var notifDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

            var outboxEvents = await notifDb.OutboxEvents
                .Where(e => e.RecipientPersonId == patientDef.PersonId)
                .ToListAsync();

            Assert.Single(outboxEvents);
            var outbox = outboxEvents[0];
            Assert.Equal("Appointment.Booked", outbox.EventType);
            Assert.Equal("Processed", outbox.Status);
            Assert.Contains(bookedAppt.Id.ToString(), outbox.IdempotencyKey);

            // Gizlilik doğrulaması: Mesaj hassas klinik teşhis içermemeli
            Assert.DoesNotContain("ICD", outbox.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Teşhis", outbox.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Randevu Referansı", outbox.Message, StringComparison.Ordinal);

            // In-app bildirim doğrulaması
            var inAppList = await notifDb.InAppNotifications
                .Where(n => n.RecipientPersonId == patientDef.PersonId)
                .ToListAsync();
            Assert.Single(inAppList);
            Assert.False(inAppList[0].IsRead);

            // Mock email teslimat doğrulaması
            var deliveries = await notifDb.MockDeliveries
                .Where(d => d.IdempotencyKey == outbox.IdempotencyKey)
                .ToListAsync();
            Assert.NotEmpty(deliveries);
            Assert.Contains(deliveries, d => d.Channel == "Email" && d.Recipient == "DEMO-patient@hospital.invalid");
        }

        // 3. Tekrar Outbox Tetikleme (Idempotency Testi - Çift bildirim oluşmamalı)
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var processResult = await notificationService.ProcessOutboxAsync();
            Assert.Equal(0, processResult.ProcessedCount);
            Assert.Equal(0, processResult.FailedCount);
        }

        var internalDispatchAttempt = await PostWithAntiforgeryAsync(
            patientClient,
            "/api/v1/notifications/process-outbox",
            new
            {
            });
        Assert.True(internalDispatchAttempt.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var notifDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var inAppCount = await notifDb.InAppNotifications
                .CountAsync(n => n.RecipientPersonId == patientDef.PersonId);
            Assert.Equal(1, inAppCount); // Çift bildirim oluşmadı
        }

        // 4. Hasta Bildirimlerimi Listele
        var myNotifsResp = await patientClient.GetAsync("/api/v1/notifications/my");
        Assert.Equal(HttpStatusCode.OK, myNotifsResp.StatusCode);
        var myNotifs = await myNotifsResp.Content.ReadFromJsonAsync<IReadOnlyList<NotificationDetailResponse>>();
        Assert.NotNull(myNotifs);
        Assert.Single(myNotifs);
        Assert.False(myNotifs[0].IsRead);

        // 5. Bildirimi Okundu Olarak İşaretle
        var markReadResp = await PostWithAntiforgeryAsync(
            patientClient,
            $"/api/v1/notifications/{myNotifs[0].Id}/read",
            new
            {
            });
        Assert.Equal(HttpStatusCode.OK, markReadResp.StatusCode);

        var myNotifsAfterRead = await patientClient.GetFromJsonAsync<IReadOnlyList<NotificationDetailResponse>>("/api/v1/notifications/my");
        Assert.NotNull(myNotifsAfterRead);
        Assert.True(myNotifsAfterRead[0].IsRead);
        Assert.NotNull(myNotifsAfterRead[0].ReadAtUtc);

        // 6. IDOR Yetki Testi: İkinci kullanıcı (doktor) hastanın bildirimini okundu yapamaz
        using var doctorClient = CreateSecureClient(application);
        await LoginAsync(doctorClient, doctorDef.Email, doctorDef.Password);
        var unauthorizedReadResp = await PostWithAntiforgeryAsync(
            doctorClient,
            $"/api/v1/notifications/{myNotifs[0].Id}/read",
            new
            {
            });
        Assert.Equal(HttpStatusCode.NotFound, unauthorizedReadResp.StatusCode);
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

    private static async Task<HttpResponseMessage> PutWithAntiforgeryAsync<TRequest>(
        HttpClient client,
        string requestUri,
        TRequest body)
    {
        var token = await client.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "/api/v1/identity/antiforgery");
        Assert.NotNull(token);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-HMS-CSRF", token.Token);
        return await client.SendAsync(request);
    }
}
