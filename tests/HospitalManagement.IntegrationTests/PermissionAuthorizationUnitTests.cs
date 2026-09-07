using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Host.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HospitalManagement.IntegrationTests;

public sealed class PermissionAuthorizationUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task PolicyProviderReturnsValidPolicyForKnownPermission()
    {
        var options = Options.Create(new AuthorizationOptions());
        var provider = new HospitalAuthorizationPolicyProvider(options);

        var policy = await provider.GetPolicyAsync(HospitalPermissions.Identity.ProfileViewOwn);

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionAuthorizationRequirement>());
        Assert.Equal(HospitalPermissions.Identity.ProfileViewOwn, requirement.Permission);
        Assert.True(requirement.IsKnown);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task PolicyProviderReturnsAlwaysDenyPolicyForUnknownPermission()
    {
        var options = Options.Create(new AuthorizationOptions());
        var provider = new HospitalAuthorizationPolicyProvider(options);

        var policy = await provider.GetPolicyAsync("nonexistent.custom.permission");

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionAuthorizationRequirement>());
        Assert.Equal("nonexistent.custom.permission", requirement.Permission);
        Assert.False(requirement.IsKnown);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task HandlerSucceedsWhenUserHasMatchingPermissionClaim()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionAuthorizationRequirement(HospitalPermissions.Identity.ProfileViewOwn, isKnown: true);

        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.ProfileViewOwn));
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task HandlerFailsWhenUserLacksMatchingPermissionClaim()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionAuthorizationRequirement(HospitalPermissions.Identity.RoleAssign, isKnown: true);

        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.ProfileViewOwn));
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task HandlerFailsWhenUserIsUnauthenticated()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionAuthorizationRequirement(HospitalPermissions.Identity.ProfileViewOwn, isKnown: true);

        var identity = new ClaimsIdentity(); // Unauthenticated
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G03")]
    public async Task HandlerFailsWhenRequirementIsUnknownEvenIfUserHasClaim()
    {
        var handler = new PermissionAuthorizationHandler();
        var requirement = new PermissionAuthorizationRequirement("nonexistent.permission", isKnown: false);

        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(HospitalClaimTypes.Permission, "nonexistent.permission"));
        var principal = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

