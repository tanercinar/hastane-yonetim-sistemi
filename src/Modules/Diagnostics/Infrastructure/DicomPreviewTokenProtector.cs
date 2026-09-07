using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using HospitalManagement.Modules.Diagnostics.Application;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class DicomPreviewTokenProtector : IDicomPreviewTokenProtector
{
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

    public string Protect(DicomPreviewGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(grant));
        using var hmac = new HMACSHA256(_key);
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return $"{payload}.{signature}";
    }

    public bool TryUnprotect(string token, out DicomPreviewGrant? grant)
    {
        grant = null;
        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            using var hmac = new HMACSHA256(_key);
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0]));
            var provided = Base64UrlDecode(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(expected, provided))
            {
                return false;
            }

            grant = JsonSerializer.Deserialize<DicomPreviewGrant>(Base64UrlDecode(parts[0]));
            return grant is not null
                && grant.StudyId != Guid.Empty
                && grant.AuthorizedPersonId != Guid.Empty
                && !string.IsNullOrWhiteSpace(grant.SopInstanceUid);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("Geçersiz base64url değeri."),
        };
        return Convert.FromBase64String(padded);
    }
}
