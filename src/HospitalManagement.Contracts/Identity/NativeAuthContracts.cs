using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Contracts.Identity;

/// <summary>
/// OAuth 2.0 / OIDC Authorization Request parameters for native public clients (RFC 6749, RFC 7636).
/// </summary>
public sealed class NativeAuthorizeRequest
{
    [Required]
    public string ResponseType { get; set; } = "code";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;

    [Required]
    public string Scope { get; set; } = "openid profile offline_access";

    [Required]
    public string State { get; set; } = string.Empty;

    [Required]
    public string CodeChallenge { get; set; } = string.Empty;

    [Required]
    public string CodeChallengeMethod { get; set; } = "S256";

    public string? Nonce
    {
        get; set;
    }
}

/// <summary>
/// OAuth 2.0 Token Request using authorization_code grant with PKCE verifier (RFC 6749, RFC 7636).
/// </summary>
public sealed class NativeTokenRequest
{
    [Required]
    public string GrantType { get; set; } = "authorization_code";

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string CodeVerifier { get; set; } = string.Empty;
}

/// <summary>
/// OAuth 2.0 Token Response returning Bearer access token and rotating refresh token.
/// </summary>
public sealed class NativeTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresIn { get; set; } = 900; // 15 minutes default

    public string? RefreshToken
    {
        get; set;
    }

    public string? Scope
    {
        get; set;
    }

    public string? IdToken
    {
        get; set;
    }
}

/// <summary>
/// OAuth 2.0 Token Refresh Request using refresh_token grant (with Refresh Token Rotation).
/// </summary>
public sealed class NativeTokenRefreshRequest
{
    [Required]
    public string GrantType { get; set; } = "refresh_token";

    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? Scope
    {
        get; set;
    }
}

/// <summary>
/// RFC 7009 Token Revocation Request for logout or session invalidation.
/// </summary>
public sealed class NativeTokenRevocationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    public string? TokenTypeHint { get; set; } = "refresh_token";

    [Required]
    public string ClientId { get; set; } = string.Empty;
}
