using System.Net;
using System.Net.Http.Headers;

namespace HospitalManagement.UI.Client;

/// <summary>
/// HTTP delegating handler that attaches OAuth 2.0 Bearer access tokens to outbound API requests
/// and transparently triggers Refresh Token Rotation (RTR) on 401 Unauthorized responses.
/// </summary>
public sealed class NativeAuthenticationHandler(INativeTokenRefreshService tokenService) : DelegatingHandler
{
    private readonly INativeTokenRefreshService _tokenService = tokenService;

    public NativeAuthenticationHandler(
        INativeTokenRefreshService tokenService,
        HttpMessageHandler innerHandler)
        : this(tokenService)
    {
        InnerHandler = innerHandler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetCurrentAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshedToken = await _tokenService.RefreshAccessTokenAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(refreshedToken))
            {
                response.Dispose();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
                return await base.SendAsync(request, cancellationToken);
            }

            await _tokenService.InvalidateSessionAsync(cancellationToken);
        }

        return response;
    }
}
