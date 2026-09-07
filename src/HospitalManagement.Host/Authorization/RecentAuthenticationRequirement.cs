using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.Host.Authorization;

public sealed class RecentAuthenticationRequirement(TimeSpan maxAge) : IAuthorizationRequirement
{
    public TimeSpan MaxAge { get; } = maxAge;
}
