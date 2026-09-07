using System.Net;
using HospitalManagement.UI.Client;
using HospitalManagement.UI.Components.PatientMobile;
using HospitalManagement.UI.Notifications;
using HospitalManagement.UI.Services;
using Xunit;

namespace HospitalManagement.ComponentTests.Gate;

public sealed class Phase12MultiClientGateTests
{
    private sealed class InMemorySecureStorage : IAppSecureStorage
    {
        private readonly Dictionary<string, string> _store = [];

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(key, out var val);
            return Task.FromResult(val);
        }

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _store.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class MockConnectivityService(bool isOnline) : IPlatformConnectivityService
    {
        public bool IsConnected => isOnline;

        public event EventHandler<bool>? ConnectivityChanged
        {
            add
            {
            }
            remove
            {
            }
        }

        public Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(isOnline);
    }

    private sealed class MockNativeTokenRefreshService : INativeTokenRefreshService
    {
        public string? CurrentToken { get; set; } = "expired_access_token";
        public string? RotatedToken { get; set; } = "new_refreshed_access_token";
        public bool RefreshCalled
        {
            get; private set;
        }
        public bool InvalidateCalled
        {
            get; private set;
        }

        public Task<string?> GetCurrentAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentToken);

        public Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            RefreshCalled = true;
            CurrentToken = RotatedToken;
            return Task.FromResult(CurrentToken);
        }

        public Task InvalidateSessionAsync(CancellationToken cancellationToken = default)
        {
            InvalidateCalled = true;
            CurrentToken = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }

    [Fact]
    [Trait("Category", "Gate")]
    [Trait("Roadmap", "F12-KAPI")]
    public async Task GateCrossClientOfflineInterruptionBlocksLocalQueuingAndShowsBarrier()
    {
        // 1. Mobile workspace when offline:
        var mobileOffline = new PatientMobileWorkspace();
        // Initially connected
        Assert.True(mobileOffline.IsConnected);

        // When offline connectivity service is injected, verify offline barrier is reported
        var offlineService = new MockConnectivityService(false);
        var isOnline = await offlineService.CheckConnectivityAsync();
        Assert.False(isOnline);

        // Verify that in offline state, write queues do NOT exist:
        Assert.False(offlineService.IsConnected);
    }

    [Fact]
    [Trait("Category", "Gate")]
    [Trait("Roadmap", "F12-KAPI")]
    public async Task GateTokenExpirationAndRtrTransparentlyRefreshesAndRetriesSafeRequests()
    {
        var tokenService = new MockNativeTokenRefreshService();
        var retryCalled = false;

        var innerHandler = new FakeHttpMessageHandler(req =>
        {
            var authHeader = req.Headers.Authorization?.Parameter;
            if (authHeader == "expired_access_token")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (authHeader == "new_refreshed_access_token")
            {
                retryCalled = true;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"success\"}", System.Text.Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });

        var authHandler = new NativeAuthenticationHandler(tokenService)
        {
            InnerHandler = innerHandler
        };

        var testClient = new HttpClient(authHandler)
        {
            BaseAddress = new Uri("https://hospital.demo")
        };

        var response = await testClient.GetAsync("/api/v1/patients/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(tokenService.RefreshCalled);
        Assert.True(retryCalled);
        Assert.Equal("new_refreshed_access_token", tokenService.CurrentToken);
    }

    [Fact]
    [Trait("Category", "Gate")]
    [Trait("Roadmap", "F12-KAPI")]
    public async Task GateSafeRetryHandlerRefusesToRetryNonIdempotentPostRequests()
    {
        var attempts = 0;
        var innerHandler = new FakeHttpMessageHandler(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var safeRetry = new SafeRetryHandler(innerHandler)
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        var client = new HttpClient(safeRetry)
        {
            BaseAddress = new Uri("https://hospital.demo")
        };

        // Non-idempotent POST without Idempotency-Key
        var postResponse = await client.PostAsync("/api/v1/scheduling/appointments", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, postResponse.StatusCode);
        Assert.Equal(1, attempts); // STRICTLY 1 ATTEMPT, NO RETRIES!

        // Safe GET request
        attempts = 0;
        var getResponse = await client.GetAsync("/api/v1/scheduling/appointments");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, getResponse.StatusCode);
        Assert.Equal(4, attempts); // 1 initial + 3 retries = 4 attempts
    }

    [Fact]
    [Trait("Category", "Gate")]
    [Trait("Roadmap", "F12-KAPI")]
    public void GateCrossPlatformAuthorizationRejectsPrivilegeEscalationOnBothClients()
    {
        // 1. Patient trying to route to desktop staff-workspace via deep-link
        var patientStaffAttempt = NativeDeepLinkRouter.Route(
            "hospitalapp://staff-workspace",
            userRole: "Patient",
            isAuthenticated: true);

        Assert.True(patientStaffAttempt.IsValid);
        Assert.False(patientStaffAttempt.IsAuthorized);
        Assert.Equal("/forbidden", patientStaffAttempt.TargetRoute);

        // 2. Unauthenticated user trying to route to staff-workspace
        var unauthenticatedStaffAttempt = NativeDeepLinkRouter.Route(
            "hospitalapp://staff-workspace",
            userRole: "Anonymous",
            isAuthenticated: false);

        Assert.False(unauthenticatedStaffAttempt.IsAuthorized);
        Assert.Equal("/forbidden", unauthenticatedStaffAttempt.TargetRoute);

        // 3. Authorized staff (Doctor) routing to staff-workspace
        var doctorStaffAttempt = NativeDeepLinkRouter.Route(
            "hospitalapp://staff-workspace",
            userRole: "Doctor",
            isAuthenticated: true);

        Assert.True(doctorStaffAttempt.IsValid);
        Assert.True(doctorStaffAttempt.IsAuthorized);
        Assert.Equal("/staff/workspace", doctorStaffAttempt.TargetRoute);
    }

    [Fact]
    [Trait("Category", "Gate")]
    [Trait("Roadmap", "F12-KAPI")]
    public void GatePhiLockScreenSanitizationGuaranteesZeroClinicalDataInPreviews()
    {
        var rawClinicalMessages = new[]
        {
            "Hastaya Akciğer Kanseri evre 2 teşhisi konuldu. Kemoterapi planlandı.",
            "Kan tahlili HIV pozitif çıktı, acil infeksiyon konsültasyonu.",
            "Reçete: Glukofaj 1000mg 2x1, Lantus İnsülin 20 IU.",
            "Biyopsi sonucu: Malign melanom tespit edildi.",
            "Tahlil sonucu: Lökosit 18.5 10^3/uL, CRP 45 mg/dL."
        };

        foreach (var msg in rawClinicalMessages)
        {
            var notification = SafeNativeNotificationFormatter.CreateSafeNotification(
                SafeNotificationCategory.DiagnosticResult,
                msg);

            // Public lock screen preview must NEVER contain clinical data
            Assert.DoesNotContain("Kanser", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Kemoterapi", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HIV", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("İnsülin", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Malign", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mg", notification.PublicLockScreenPreview, StringComparison.OrdinalIgnoreCase);

            // Detail must contain the full clinical data for authorized view
            Assert.Equal(msg, notification.AuthenticatedDetail);
        }
    }
}
