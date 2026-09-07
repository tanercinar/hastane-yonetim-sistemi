using System.Globalization;
using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Identity;

public sealed class HospitalUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    TimeProvider? timeProvider = null)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, optionsAccessor)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(
            HospitalClaimTypes.PersonId,
            user.PersonId.ToString("D", CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim(
            HospitalClaimTypes.AccountKind,
            user.AccountKind.ToString()));
        identity.AddClaim(new Claim(
            HospitalClaimTypes.AuthTime,
            _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim(
            HospitalClaimTypes.Amr,
            user.TwoFactorEnabled ? "mfa" : "pwd"));
        identity.AddClaim(new Claim(
            HospitalClaimTypes.Permission,
            HospitalPermissions.Identity.ProfileViewOwn));
        identity.AddClaim(new Claim(
            HospitalClaimTypes.Permission,
            HospitalPermissions.Identity.ProfileEditOwn));

        var roles = await UserManager.GetRolesAsync(user);
        var permissions = RolePermissionDefaults.GetPermissionsForRoles(roles);

        foreach (var permission in permissions)
        {
            if (!identity.HasClaim(HospitalClaimTypes.Permission, permission))
            {
                identity.AddClaim(new Claim(HospitalClaimTypes.Permission, permission));
            }
        }

        return identity;
    }
}

