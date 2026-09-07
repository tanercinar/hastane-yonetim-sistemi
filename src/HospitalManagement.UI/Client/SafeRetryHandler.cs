using System.Net;

namespace HospitalManagement.UI.Client;

/// <summary>
/// HTTP message handler providing resilient retries strictly for safe and idempotent requests.
/// Protects against duplicate mutations by refusing to retry non-idempotent writes (e.g. POST without Idempotency-Key).
/// </summary>
public sealed class SafeRetryHandler : DelegatingHandler
{
    private static readonly HashSet<HttpMethod> IdempotentMethods = new()
    {
        HttpMethod.Get,
        HttpMethod.Head,
        HttpMethod.Options,
        HttpMethod.Trace,
        HttpMethod.Put,
        HttpMethod.Delete
    };

    private static readonly HashSet<HttpStatusCode> TransientStatusCodes = new()
    {
        HttpStatusCode.ServiceUnavailable, // 503
        HttpStatusCode.GatewayTimeout,     // 504
        HttpStatusCode.RequestTimeout      // 408
    };

    public int MaxRetries { get; set; } = 2;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    public SafeRetryHandler()
    {
    }

    public SafeRetryHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var isIdempotent = IsRequestSafeOrIdempotent(request);
        var attempts = 0;
        var maxAttempts = isIdempotent ? MaxRetries + 1 : 1;

        while (true)
        {
            attempts++;
            try
            {
                var response = await base.SendAsync(request, cancellationToken);

                if (attempts < maxAttempts &&
                    isIdempotent &&
                    TransientStatusCodes.Contains(response.StatusCode))
                {
                    response.Dispose();
                    await Task.Delay(InitialDelay * attempts, cancellationToken);
                    continue;
                }

                return response;
            }
            catch (HttpRequestException) when (attempts < maxAttempts && isIdempotent)
            {
                await Task.Delay(InitialDelay * attempts, cancellationToken);
            }
        }
    }

    public static bool IsRequestSafeOrIdempotent(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (IdempotentMethods.Contains(request.Method))
        {
            return true;
        }

        // Custom idempotency headers allow safe retries of state-changing operations
        if (request.Headers.Contains("Idempotency-Key") ||
            request.Headers.Contains("X-Idempotency-Key"))
        {
            return true;
        }

        return false;
    }
}
