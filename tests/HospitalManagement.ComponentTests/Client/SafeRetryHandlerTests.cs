using System.Net;
using HospitalManagement.UI.Client;

using Xunit;

namespace HospitalManagement.ComponentTests.Client;

public sealed class SafeRetryHandlerTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> handlerFunc)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _handlerFunc = handlerFunc;
        public int CallCount
        {
            get; private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_handlerFunc(request, CallCount));
        }
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    public async Task SafeRetryHandlerShouldRetryGetOn503ServiceUnavailable()
    {
        var mockHandler = new MockHttpMessageHandler((req, count) =>
        {
            if (count == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var retryHandler = new SafeRetryHandler(mockHandler)
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(5)
        };

        using var client = new HttpClient(retryHandler);
        var response = await client.GetAsync("https://hospital.example.com/api/v1/patients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, mockHandler.CallCount);
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    public async Task SafeRetryHandlerShouldNotRetryNonIdempotentPostOn503()
    {
        var mockHandler = new MockHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var retryHandler = new SafeRetryHandler(mockHandler)
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(5)
        };

        using var client = new HttpClient(retryHandler);
        var response = await client.PostAsync("https://hospital.example.com/api/v1/appointments", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, mockHandler.CallCount); // Strict rule: no retry on unsafe POST!
    }

    [Fact]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    public async Task SafeRetryHandlerShouldRetryPostWhenIdempotencyKeyHeaderIsPresent()
    {
        var mockHandler = new MockHttpMessageHandler((req, count) =>
        {
            if (count == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return new HttpResponseMessage(HttpStatusCode.Created);
        });

        var retryHandler = new SafeRetryHandler(mockHandler)
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(5)
        };

        using var client = new HttpClient(retryHandler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://hospital.example.com/api/v1/appointments")
        {
            Content = new StringContent("{}")
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, mockHandler.CallCount);
    }

    [Theory]
    [Trait("Category", "Component")]
    [Trait("Roadmap", "F13-G09")]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task SafeRetryHandlerShouldNotRetryClientErrors(HttpStatusCode statusCode)
    {
        var mockHandler = new MockHttpMessageHandler((_, _) => new HttpResponseMessage(statusCode));

        var retryHandler = new SafeRetryHandler(mockHandler)
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(5)
        };

        using var client = new HttpClient(retryHandler);
        var response = await client.GetAsync("https://hospital.example.com/api/v1/patients");

        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(1, mockHandler.CallCount);
    }
}
