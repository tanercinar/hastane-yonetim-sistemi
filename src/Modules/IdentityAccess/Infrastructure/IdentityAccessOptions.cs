namespace HospitalManagement.Modules.IdentityAccess.Infrastructure;

public sealed class IdentityAccessOptions
{
    public const string SectionName = "HospitalManagement:Identity";

    public int SessionMinutes { get; set; } = 60;

    public int ActionCodeMinutes { get; set; } = 30;

    public int LockoutMinutes { get; set; } = 15;

    public int MaxFailedAccessAttempts { get; set; } = 5;

    public int SensitivePermitLimit { get; set; } = 10;
}

public static class IdentityAccessConstants
{
    public const string AuthCookieName = "__Host-HospitalManagement.Auth";
    public const string AntiforgeryHeaderName = "X-HMS-CSRF";
    public const string RateLimitPolicyName = "identity-sensitive";
}
