namespace HospitalManagement.Modules.Patients.Application;

public static class PatientMaskingHelper
{
    public static string? MaskNationalId(string? nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return null;
        }

        var trimmed = nationalId.Trim();
        if (trimmed.Length <= 4)
        {
            return "****";
        }

        var prefix = trimmed[..2];
        var suffix = trimmed[^2..];
        var asterisks = new string('*', trimmed.Length - 4);
        return $"{prefix}{asterisks}{suffix}";
    }

    public static string? MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        if (trimmed.Length <= 6)
        {
            return "******";
        }

        var prefix = trimmed[..4];
        var suffix = trimmed[^2..];
        var asterisks = new string('*', trimmed.Length - 6);
        return $"{prefix}{asterisks}{suffix}";
    }
}
