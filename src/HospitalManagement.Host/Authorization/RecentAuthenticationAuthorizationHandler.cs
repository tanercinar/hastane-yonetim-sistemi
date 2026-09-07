using System.Globalization;

using HospitalManagement.BuildingBlocks.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace HospitalManagement.Host.Authorization;

public sealed class RecentAuthenticationAuthorizationHandler(TimeProvider timeProvider)
    : AuthorizationHandler<RecentAuthenticationRequirement>
{
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RecentAuthenticationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var authTimeClaim = context.User.FindFirst(HospitalClaimTypes.AuthTime)?.Value;
        if (string.IsNullOrWhiteSpace(authTimeClaim)
            || !long.TryParse(authTimeClaim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var authTimeSeconds))
        {
            return Task.CompletedTask;
        }

        var currentSeconds = _timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var elapsedSeconds = currentSeconds - authTimeSeconds;

        if (elapsedSeconds >= 0 && elapsedSeconds <= requirement.MaxAge.TotalSeconds)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

