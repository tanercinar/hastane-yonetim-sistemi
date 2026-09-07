using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalManagement.UnitTests;

public sealed class AdminUserManagementUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G08")]
    public async Task DisablingLastSystemAdministratorIsRejected()
    {
        var services = BuildServiceProvider(out var auditPublisher);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityLifecycleService>();

        var adminUser = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-admin1@hospital.invalid",
            DateTime.UtcNow);
        adminUser.AcceptStaffInvitation();
        await userManager.CreateAsync(adminUser, "DEMO-Admin-Pass!1");
        await userManager.AddToRoleAsync(adminUser, HospitalRoles.SystemAdministrator);

        var adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Email, adminUser.Email!),
            new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.RoleAssign),
            new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.UserDisable),
        ], "TestAuth"));

        // Tek admin kendini devre dışı bırakmayı deniyor
        var result = await service.UpdateUserStatusAsync(
            adminPrincipal,
            new UpdateUserStatusCommand(adminUser.Id, isEnabled: false));

        Assert.Equal(IdentityLifecycleStatus.ValidationFailed, result.Status);
        Assert.NotNull(result.Errors);
        Assert.True(result.Errors.ContainsKey("user"));
        Assert.Contains("Son sistem yöneticisi", result.Errors["user"][0]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G08")]
    public async Task RemovingAdminRoleFromLastSystemAdministratorIsRejected()
    {
        var services = BuildServiceProvider(out var auditPublisher);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityLifecycleService>();

        var adminUser = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-admin1@hospital.invalid",
            DateTime.UtcNow);
        adminUser.AcceptStaffInvitation();
        await userManager.CreateAsync(adminUser, "DEMO-Admin-Pass!1");
        await userManager.AddToRoleAsync(adminUser, HospitalRoles.SystemAdministrator);

        var adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Email, adminUser.Email!),
            new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.RoleAssign),
        ], "TestAuth"));

        // Admin rolünü kaldırmayı deniyor (yalnızca Doctor rolü vermek istiyor)
        var result = await service.UpdateUserRolesAsync(
            adminPrincipal,
            new UpdateUserRolesCommand(adminUser.Id, [HospitalRoles.Doctor]));

        Assert.Equal(IdentityLifecycleStatus.ValidationFailed, result.Status);
        Assert.NotNull(result.Errors);
        Assert.True(result.Errors.ContainsKey("roles"));
        Assert.Contains("Son sistem yöneticisi", result.Errors["roles"][0]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F02-G08")]
    public async Task UpdatingNonAdminRolesAndStatusSucceedsAndGeneratesAuditLogs()
    {
        var services = BuildServiceProvider(out var auditPublisher);
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityLifecycleService>();

        var adminUser = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-admin@hospital.invalid",
            DateTime.UtcNow);
        adminUser.AcceptStaffInvitation();
        await userManager.CreateAsync(adminUser, "DEMO-Admin-Pass!1");
        await userManager.AddToRoleAsync(adminUser, HospitalRoles.SystemAdministrator);

        var doctorUser = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-doctor@hospital.invalid",
            DateTime.UtcNow);
        doctorUser.AcceptStaffInvitation();
        await userManager.CreateAsync(doctorUser, "DEMO-Doc-Pass!1");
        await userManager.AddToRoleAsync(doctorUser, HospitalRoles.Doctor);

        var adminPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
            new Claim(ClaimTypes.Email, adminUser.Email!),
            new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.RoleAssign),
            new Claim(HospitalClaimTypes.Permission, HospitalPermissions.Identity.UserDisable),
        ], "TestAuth"));

        // 1. Doktorun rolünü Doctor + Nurse olarak güncelle
        var roleResult = await service.UpdateUserRolesAsync(
            adminPrincipal,
            new UpdateUserRolesCommand(doctorUser.Id, [HospitalRoles.Doctor, HospitalRoles.Nurse]));
        Assert.Equal(IdentityLifecycleStatus.Succeeded, roleResult.Status);

        var roles = await userManager.GetRolesAsync(doctorUser);
        Assert.Contains(HospitalRoles.Doctor, roles);
        Assert.Contains(HospitalRoles.Nurse, roles);

        // 2. Doktoru devre dışı bırak
        var disableResult = await service.UpdateUserStatusAsync(
            adminPrincipal,
            new UpdateUserStatusCommand(doctorUser.Id, isEnabled: false));
        Assert.Equal(IdentityLifecycleStatus.Succeeded, disableResult.Status);

        var updatedDoc = await userManager.FindByIdAsync(doctorUser.Id.ToString());
        Assert.False(updatedDoc!.IsEnabled);

        // 3. Denetim loglarının oluşturulduğunu kontrol et
        Assert.Contains(auditPublisher.Events, e => e.Action == AuditAction.RoleAssign && e.TargetResourceId == doctorUser.Id.ToString());
        Assert.Contains(auditPublisher.Events, e => e.Action == AuditAction.UserDisable && e.TargetResourceId == doctorUser.Id.ToString());
    }

    private static ServiceProvider BuildServiceProvider(out RecordingAuditPublisher auditPublisher)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddHttpContextAccessor();
        services.AddAuthentication();

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
        .AddSignInManager<HospitalSignInManager>()
        .AddDefaultTokenProviders();

        auditPublisher = new RecordingAuditPublisher();
        services.AddSingleton<IAuditEventPublisher>(auditPublisher);
        services.AddSingleton(auditPublisher);

        services.AddSingleton<IIdentityMessageSender, InMemoryTestMessageSender>();
        services.Configure<IdentityAccessOptions>(options =>
        {
            options.SessionMinutes = 60;
            options.ActionCodeMinutes = 15;
            options.LockoutMinutes = 15;
            options.MaxFailedAccessAttempts = 5;
            options.SensitivePermitLimit = 10;
        });

        services.AddScoped<IIdentityLifecycleService, IdentityLifecycleService>();

        var sp = services.BuildServiceProvider();

        // Rolleri tohumla
        using var scope = sp.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in HospitalRoles.All)
        {
            roleManager.CreateAsync(new IdentityRole<Guid>(role.Code)).GetAwaiter().GetResult();
        }

        return sp;
    }

    private sealed class RecordingAuditPublisher : IAuditEventPublisher
    {
        public List<AuditEvent> Events { get; } = [];

        public Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTestMessageSender : IIdentityMessageSender
    {
        public Task SendAsync(IdentityMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
