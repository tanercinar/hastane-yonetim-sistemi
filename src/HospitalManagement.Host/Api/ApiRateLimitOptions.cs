namespace HospitalManagement.Host.Api;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "HospitalManagement:Api:RateLimit";

    public int PermitLimit
    {
        get;
        set;
    } = 120;

    public int WindowSeconds
    {
        get;
        set;
    } = 60;
}
