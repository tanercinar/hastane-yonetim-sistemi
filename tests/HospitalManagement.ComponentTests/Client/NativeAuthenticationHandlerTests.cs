using System.Net;
using HospitalManagement.UI.Client;

using Xunit;

namespace HospitalManagement.ComponentTests.Client;

public sealed class NativeAuthenticationHandlerTests
{
    private sealed class MockTokenService : INativeTokenRefreshService
    {
        public string? CurrentToken
        {
            get; set;
        }
        public string? RefreshedToken
        {
            get; set;
        }
        public bool InvalidateSessionCalled
        {
            get; private set;
        }

        public Task<string?> GetCurrentAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentToken);

        public Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RefreshedToken);

        public Task InvalidateSessionAsync(CancellationToken cancellationToken = default)
        {
            InvalidateSessionCalled = true;
            CurrentToken = null;
            return Task.CompletedTask;
        }
    }

    private sealed class MockHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handlerFunc = handlerFunc;
        public int CallCount
        {
            get; private set;
        }
        public HttpRequestMessage? LastRequest
        {
            get; private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(_handlerFunc(request));
        }
    }

    [Fact]
    public async Task HandlerShouldAttachBearerTokenWhenAvailable()
    {
        var tokenService = new MockTokenService { CurrentToken = "valid_test_token" };
        var mockHttp = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var authHandler = new NativeAuthenticationHandler(tokenService, mockHttp);
        using var client = new HttpClient(authHandler);

        var response = await client.GetAsync("https://hospital.example.com/api/v1/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(mockHttp.LastRequest?.Headers.Authorization);
        Assert.Equal("Bearer", mockHttp.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("valid_test_token", mockHttp.LastRequest?.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task HandlerShouldRefreshOn401AndRetryWithNewToken()
    {
        var tokenService = new MockTokenService
        {
            CurrentToken = "expired_token",
            RefreshedToken = "new_refreshed_token"
        };

        var mockHttp = new MockHttpHandler(req =>
        {
            if (req.Headers.Authorization?.Parameter == "expired_token")
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var authHandler = new NativeAuthenticationHandler(tokenService, mockHttp);
        using var client = new HttpClient(authHandler);

        var response = await client.GetAsync("https://hospital.example.com/api/v1/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, mockHttp.CallCount);
        Assert.Equal("new_refreshed_token", mockHttp.LastRequest?.Headers.Authorization?.Parameter);
        Assert.False(tokenService.InvalidateSessionCalled);
    }

    [Fact]
    public async Task HandlerShouldInvalidateSessionWhenRefreshFails()
    {
        var tokenService = new MockTokenService
        {
            CurrentToken = "expired_token",
            RefreshedToken = null // Refresh failed
        };

        var mockHttp = new MockHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var authHandler = new NativeAuthenticationHandler(tokenService, mockHttp);
        using var client = new HttpClient(authHandler);

        var response = await client.GetAsync("https://hospital.example.com/api/v1/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(tokenService.InvalidateSessionCalled);
    }
}
