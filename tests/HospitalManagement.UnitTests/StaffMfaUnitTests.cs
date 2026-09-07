using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalManagement.UnitTests;

public sealed class StaffMfaUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task MfaSetupEnableAndRecoveryCodeRedemptionWorksAcrossScopes()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityAccessDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication();
        services.AddHttpContextAccessor();

        var sp = services.BuildServiceProvider();

        Guid userId;
        string[]? codes;

        using (var scope = sp.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = ApplicationUser.CreateInvitedStaff(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEMO-test@hospital.invalid",
                DateTime.UtcNow);
            user.AcceptStaffInvitation();

            var createResult = await userManager.CreateAsync(user, "DEMO-Test-Pass!1");
            Assert.True(createResult.Succeeded);
            userId = user.Id;

            // Generate recovery codes in scope 1
            codes = (await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))?.ToArray();
            Assert.NotNull(codes);
            Assert.Equal(8, codes.Length);
        }

        // Check tokens in DbContext directly
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
            var tokens = await db.UserTokens.Where(t => t.UserId == userId).ToListAsync();
            Assert.NotEmpty(tokens);
        }

        // Redeem in scope 2
        using (var scope = sp.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);

            var redeemResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, codes[0]);
            Assert.True(redeemResult.Succeeded, $"Redemption failed: {string.Join(',', redeemResult.Errors.Select(e => e.Description))}");
        }
    }
}

