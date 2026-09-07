using System.Security.Cryptography;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public static class AttachmentSecurityValidator
{
    public const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15 MB

    private static readonly char[] ExtraDangerousChars = ['/', '\\', '\0', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".dcm",
        ".dicom",
    };

    public static string SanitizeFileName(string? rawFileName)
    {
        if (string.IsNullOrWhiteSpace(rawFileName))
        {
            return "attachment.bin";
        }

        // Strip path traversal attempts and extract plain filename (cross-platform normalization)
        var normalized = rawFileName.Trim().Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);

        // Remove dangerous characters (null bytes, quotes, path separators)
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat(ExtraDangerousChars)
            .ToHashSet();

        var sanitizedChars = fileName.Where(c => !invalidChars.Contains(c)).ToArray();
        var sanitized = new string(sanitizedChars);

        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.StartsWith('.'))
        {
            sanitized = "attachment_" + Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(fileName);
        }

        if (sanitized.Length > 180)
        {
            var extension = Path.GetExtension(sanitized);
            var baseNameLength = Math.Max(1, 180 - extension.Length);
            sanitized = sanitized[..baseNameLength] + extension;
        }

        return sanitized;
    }

    public static (bool IsValid, string? ErrorMessage, string? ContentType) ValidateAttachment(
        string fileName,
        string declaredContentType,
        byte[] fileBytes)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        if (fileBytes.Length == 0)
        {
            return (false, "Yüklenen dosya boş olamaz.", null);
        }

        if (fileBytes.Length > MaxFileSizeBytes)
        {
            return (false, $"Dosya boyutu maksimum izin verilen 15 MB sınırını aşıyor ({fileBytes.Length} bayt).", null);
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return (false, $"Geçersiz dosya uzantısı ({ext}). Yalnızca PDF, JPG, PNG, WEBP ve DICOM dosyaları yüklenebilir.", null);
        }

        // Executable / malicious file signature check (MZ for DOS/PE EXE, DLL)
        if (fileBytes.Length >= 2 && fileBytes[0] == 0x4D && fileBytes[1] == 0x5A)
        {
            return (false, "Çalıştırılabilir ikili (EXE/DLL) dosyaların yüklenmesi güvenlik gerekçesiyle yasaktır.", null);
        }

        string detectedContentType;

        // Magic byte verification according to extension
        switch (ext)
        {
            case ".pdf":
                if (fileBytes.Length < 4 ||
                    fileBytes[0] != 0x25 || fileBytes[1] != 0x50 || fileBytes[2] != 0x44 || fileBytes[3] != 0x46)
                {
                    return (false, "Dosya içeriği geçerli bir PDF formatında değil (MIME / imza sahteciliği).", null);
                }
                detectedContentType = "application/pdf";
                break;

            case ".jpg":
            case ".jpeg":
                if (fileBytes.Length < 3 ||
                    fileBytes[0] != 0xFF || fileBytes[1] != 0xD8 || fileBytes[2] != 0xFF)
                {
                    return (false, "Dosya içeriği geçerli bir JPEG formatında değil.", null);
                }
                detectedContentType = "image/jpeg";
                break;

            case ".png":
                if (fileBytes.Length < 8 ||
                    fileBytes[0] != 0x89 || fileBytes[1] != 0x50 || fileBytes[2] != 0x4E || fileBytes[3] != 0x47 ||
                    fileBytes[4] != 0x0D || fileBytes[5] != 0x0A || fileBytes[6] != 0x1A || fileBytes[7] != 0x0A)
                {
                    return (false, "Dosya içeriği geçerli bir PNG formatında değil.", null);
                }
                detectedContentType = "image/png";
                break;

            case ".webp":
                if (fileBytes.Length < 12 ||
                    fileBytes[0] != 0x52 || fileBytes[1] != 0x49 || fileBytes[2] != 0x46 || fileBytes[3] != 0x46 ||
                    fileBytes[8] != 0x57 || fileBytes[9] != 0x45 || fileBytes[10] != 0x42 || fileBytes[11] != 0x50)
                {
                    return (false, "Dosya içeriği geçerli bir WebP formatında değil.", null);
                }
                detectedContentType = "image/webp";
                break;

            case ".dcm":
            case ".dicom":
                if (fileBytes.Length < 132
                    || fileBytes[128] != 0x44
                    || fileBytes[129] != 0x49
                    || fileBytes[130] != 0x43
                    || fileBytes[131] != 0x4D)
                {
                    return (false, "Dosya içeriği geçerli bir DICOM Part 10 imzası taşımıyor.", null);
                }
                detectedContentType = "application/dicom";
                break;

            default:
                return (false, "Desteklenmeyen dosya türü.", null);
        }

        var normalizedDeclaredType = declaredContentType
            .Split(';', 2, StringSplitOptions.TrimEntries)[0]
            .ToLowerInvariant();
        if (normalizedDeclaredType == "image/jpg")
        {
            normalizedDeclaredType = "image/jpeg";
        }

        if (!string.Equals(normalizedDeclaredType, detectedContentType, StringComparison.Ordinal))
        {
            return (
                false,
                "Bildirilen içerik türü dosyanın doğrulanan biçimiyle eşleşmiyor.",
                null);
        }

        return (true, null, detectedContentType);
    }

    public static string ComputeSha256Checksum(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
