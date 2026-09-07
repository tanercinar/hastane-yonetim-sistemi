using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.UnitTests;

public sealed class DemoDataAndSecurityUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G07")]
    public async Task IdentityDataSeederIsDeterministicAndIdempotent()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<IdentityAccessDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IIdentityDataSeeder, IdentityDataSeeder>();

        var sp = services.BuildServiceProvider();

        // 1. İlk çalıştırma
        using (var scope = sp.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await seeder.SeedAsync();
        }

        // 2. İkinci çalıştırma (Idempotency doğrulaması - hata vermemeli)
        using (var scope = sp.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await seeder.SeedAsync();
        }

        // 3. Sonuçları doğrula
        using (var scope = sp.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var db = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();

            var users = await db.Users.ToListAsync();
            Assert.Equal(IdentityDataSeeder.DemoUsers.Count, users.Count);

            foreach (var demoDef in IdentityDataSeeder.DemoUsers)
            {
                // DEMO-* prefix ve .invalid domain kontrolü
                Assert.StartsWith("DEMO-", demoDef.Email, StringComparison.Ordinal);
                Assert.EndsWith(".invalid", demoDef.Email, StringComparison.Ordinal);

                var user = await userManager.FindByEmailAsync(demoDef.Email);
                Assert.NotNull(user);
                Assert.True(user.EmailConfirmed);
                Assert.True(user.IsEnabled);

                var inRole = await userManager.IsInRoleAsync(user, demoDef.RoleName);
                Assert.True(inRole, $"Kullanıcı '{demoDef.Email}' rolü '{demoDef.RoleName}' ile eşleşmedi.");
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G07")]
    public void AllDefinedDemoUsersFollowStrictSyntheticConventions()
    {
        foreach (var def in IdentityDataSeeder.DemoUsers)
        {
            Assert.StartsWith("DEMO-", def.Email, StringComparison.Ordinal);
            Assert.True(def.Email.EndsWith(".invalid", StringComparison.Ordinal) || def.Email.EndsWith(".test", StringComparison.Ordinal));
            Assert.StartsWith("DEMO-", def.Password, StringComparison.Ordinal);
            Assert.NotEqual(Guid.Empty, def.UserId);
            Assert.NotEqual(Guid.Empty, def.PersonId);
            Assert.False(string.IsNullOrWhiteSpace(def.RoleName));
        }
    }
}

