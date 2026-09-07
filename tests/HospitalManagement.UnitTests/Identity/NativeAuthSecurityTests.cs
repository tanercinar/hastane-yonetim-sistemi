using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;

using Xunit;

namespace HospitalManagement.UnitTests.Identity;

public sealed class NativeAuthSecurityTests
{
    [Fact]
    public void GenerateCodeVerifierShouldProduceValidVerifier()
    {
        var verifier = PkceSecurityHelper.GenerateCodeVerifier();

        Assert.NotNull(verifier);
        Assert.Equal(PkceSecurityHelper.DefaultVerifierLength, verifier.Length);
        Assert.True(PkceSecurityHelper.IsValidVerifierFormat(verifier));
    }

    [Theory]
    [InlineData(43)]
    [InlineData(64)]
    [InlineData(128)]
    public void GenerateCodeVerifierCustomLengthShouldProduceCorrectLength(int length)
    {
        var verifier = PkceSecurityHelper.GenerateCodeVerifier(length);

        Assert.Equal(length, verifier.Length);
        Assert.True(PkceSecurityHelper.IsValidVerifierFormat(verifier));
    }

    [Theory]
    [InlineData(42)]
    [InlineData(129)]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateCodeVerifierInvalidLengthShouldThrow(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PkceSecurityHelper.GenerateCodeVerifier(length));
    }

    [Fact]
    public void PkceRfc7636AppendixBTestVectorShouldMatchExpectedChallenge()
    {
        // Test vector from RFC 7636 Appendix B
        const string rfcVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string expectedChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        var actualChallenge = PkceSecurityHelper.GenerateCodeChallenge(rfcVerifier);

        Assert.Equal(expectedChallenge, actualChallenge);

        var isValid = PkceSecurityHelper.ValidateCodeVerifier(rfcVerifier, expectedChallenge, "S256");
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateCodeVerifierWithMismatchedChallengeShouldReturnFalse()
    {
        var verifier = PkceSecurityHelper.GenerateCodeVerifier();
        var challenge = PkceSecurityHelper.GenerateCodeChallenge(verifier);

        var differentVerifier = PkceSecurityHelper.GenerateCodeVerifier();

        var isValid = PkceSecurityHelper.ValidateCodeVerifier(differentVerifier, challenge, "S256");
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCodeVerifierShouldRejectPlainMethod()
    {
        var verifier = PkceSecurityHelper.GenerateCodeVerifier();
        var challenge = PkceSecurityHelper.GenerateCodeChallenge(verifier);

        var isValid = PkceSecurityHelper.ValidateCodeVerifier(verifier, challenge, "plain");
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null, "challenge")]
    [InlineData("verifier", null)]
    [InlineData("", "challenge")]
    [InlineData("verifier", "")]
    public void ValidateCodeVerifierWithNullOrEmptyShouldReturnFalse(string? verifier, string? challenge)
    {
        var isValid = PkceSecurityHelper.ValidateCodeVerifier(verifier, challenge, "S256");
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("http://127.0.0.1:5000/callback")]
    [InlineData("http://127.0.0.1:8080/auth-callback")]
    [InlineData("http://localhost:5001/callback")]
    [InlineData("http://[::1]:5002/callback")]
    [InlineData("hospitalmanagement://auth-callback")]
    [InlineData("hospitalmanagement://oauth-callback")]
    [InlineData("https://hospital.example.com/auth-callback")]
    public void IsValidNativeCallbackUriShouldAcceptAuthorizedUris(string validUri)
    {
        var isValid = NativeCallbackUriValidator.IsValidNativeCallbackUri(validUri);
        Assert.True(isValid);
    }

    [Theory]
    [InlineData("//attacker.com/callback")]
    [InlineData("http://attacker.com/callback")]
    [InlineData("https://evil.com/auth-callback")]
    [InlineData("http://user:pass@127.0.0.1:5000/callback")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<html>")]
    [InlineData("evilscheme://auth-callback")]
    [InlineData("hospitalmanagement://unknown-host")]
    [InlineData("https://hospital.example.com/unauthorized-endpoint")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidNativeCallbackUriShouldRejectUnauthorizedUris(string? invalidUri)
    {
        var isValid = NativeCallbackUriValidator.IsValidNativeCallbackUri(invalidUri);
        Assert.False(isValid);
    }

    [Fact]
    public void NativeAuthContractsShouldInitializeWithDefaultSecureValues()
    {
        var authReq = new NativeAuthorizeRequest();
        Assert.Equal("code", authReq.ResponseType);
        Assert.Equal("S256", authReq.CodeChallengeMethod);

        var tokenReq = new NativeTokenRequest();
        Assert.Equal("authorization_code", tokenReq.GrantType);

        var tokenResp = new NativeTokenResponse();
        Assert.Equal("Bearer", tokenResp.TokenType);
        Assert.Equal(900, tokenResp.ExpiresIn);

        var refreshReq = new NativeTokenRefreshRequest();
        Assert.Equal("refresh_token", refreshReq.GrantType);

        var revokeReq = new NativeTokenRevocationRequest();
        Assert.Equal("refresh_token", revokeReq.TokenTypeHint);
    }
}
