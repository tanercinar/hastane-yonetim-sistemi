using System.Security.Cryptography;
using System.Text;

namespace HospitalManagement.BuildingBlocks.Authorization;

/// <summary>
/// Implements RFC 7636 (Proof Key for Code Exchange by OAuth Public Clients).
/// Provides secure code_verifier generation, S256 code_challenge derivation, and timing-safe validation.
/// </summary>
public static class PkceSecurityHelper
{
    public const string S256Method = "S256";
    public const int MinVerifierLength = 43;
    public const int MaxVerifierLength = 128;
    public const int DefaultVerifierLength = 64;

    private const string UnreservedCharacters =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";

    /// <summary>
    /// Generates a cryptographically secure random code_verifier string according to RFC 7636 Section 4.1.
    /// </summary>
    /// <param name="length">Length of verifier (must be between 43 and 128 characters, default 64).</param>
    /// <returns>URL-safe code_verifier string.</returns>
    public static string GenerateCodeVerifier(int length = DefaultVerifierLength)
    {
        if (length is < MinVerifierLength or > MaxVerifierLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                $"PKCE code_verifier length must be between {MinVerifierLength} and {MaxVerifierLength} characters.");
        }

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = UnreservedCharacters[bytes[i] % UnreservedCharacters.Length];
        }

        return new string(chars);
    }

    /// <summary>
    /// Computes the S256 code_challenge from a code_verifier according to RFC 7636 Section 4.2.
    /// code_challenge = BASE64URL-ENCODE(SHA256(ASCII(code_verifier)))
    /// </summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);

        if (!IsValidVerifierFormat(codeVerifier))
        {
            throw new ArgumentException(
                $"PKCE code_verifier must contain only unreserved characters [A-Z, a-z, 0-9, '-', '.', '_', '~'] and have length between {MinVerifierLength} and {MaxVerifierLength}.",
                nameof(codeVerifier));
        }

        var asciiBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hashBytes = SHA256.HashData(asciiBytes);

        return Base64UrlEncode(hashBytes);
    }

    /// <summary>
    /// Validates a received code_verifier against the recorded code_challenge using timing-safe comparison.
    /// Rejects plain method; enforces S256.
    /// </summary>
    public static bool ValidateCodeVerifier(
        string? codeVerifier,
        string? codeChallenge,
        string? codeChallengeMethod = S256Method)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) ||
            string.IsNullOrWhiteSpace(codeChallenge) ||
            !string.Equals(codeChallengeMethod, S256Method, StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsValidVerifierFormat(codeVerifier))
        {
            return false;
        }

        var expectedChallenge = GenerateCodeChallenge(codeVerifier);

        var expectedBytes = Encoding.UTF8.GetBytes(expectedChallenge);
        var actualBytes = Encoding.UTF8.GetBytes(codeChallenge);

        if (expectedBytes.Length != actualBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <summary>
    /// Checks whether the string adheres to RFC 7636 unreserved character set and length constraints.
    /// </summary>
    public static bool IsValidVerifierFormat(string codeVerifier)
    {
        if (string.IsNullOrEmpty(codeVerifier) ||
            codeVerifier.Length < MinVerifierLength ||
            codeVerifier.Length > MaxVerifierLength)
        {
            return false;
        }

        foreach (var c in codeVerifier)
        {
            var isUnreserved = (c >= 'A' && c <= 'Z') ||
                               (c >= 'a' && c <= 'z') ||
                               (c >= '0' && c <= '9') ||
                               c == '-' || c == '.' || c == '_' || c == '~';

            if (!isUnreserved)
            {
                return false;
            }
        }

        return true;
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
