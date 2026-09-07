namespace HospitalManagement.Modules.Organization.Domain;

internal static class OrganizationDomainRules
{
    internal static Guid RequireId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return value;
    }

    internal static string NormalizeCode(
        string value,
        string parameterName,
        int maximumLength = 40)
    {
        var normalized = NormalizeRequiredText(value, parameterName, maximumLength)
            .ToUpperInvariant();

        if (!char.IsAsciiLetterOrDigit(normalized[0])
            || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException(
                "Code must contain only ASCII letters, digits, and hyphens, and must start with a letter or digit.",
                parameterName);
        }

        return normalized;
    }

    internal static string NormalizeRequiredText(
        string value,
        string parameterName,
        int maximumLength)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot be longer than {maximumLength} characters.");
        }

        return normalized;
    }

    internal static DateTime RequireUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must use UTC DateTimeKind.", parameterName);
        }

        return value;
    }
}
