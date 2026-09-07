using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure;

public static class IdentityAccessServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        services
            .AddOptions<IdentityAccessOptions>()
            .Bind(configuration.GetRequiredSection(IdentityAccessOptions.SectionName))
            .Validate(options => options.SessionMinutes is >= 5 and <= 480)
            .Validate(options => options.ActionCodeMinutes is >= 5 and <= 60)
            .Validate(options => options.LockoutMinutes is >= 5 and <= 1440)
            .Validate(options => options.MaxFailedAccessAttempts is >= 3 and <= 10)
            .Validate(options => options.SensitivePermitLimit is >= 5 and <= 100)
            .ValidateOnStart();

        services.AddDbContext<IdentityAccessDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            options
                .UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable(
                    IdentityAccessDbContext.MigrationHistoryTable,
                    IdentityAccessDbContext.Schema))
                .EnableDetailedErrors(false)
                .EnableSensitiveDataLogging(false);
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<IdentityAccessDbContext>()
            .AddSignInManager<HospitalSignInManager>()
            .AddDefaultTokenProviders();

        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<IdentityAccessOptions>>((identity, configured) =>
            {
                identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                    configured.Value.LockoutMinutes);
                identity.Lockout.MaxFailedAccessAttempts = configured.Value.MaxFailedAccessAttempts;
            });

        services.ConfigureApplicationCookie(cookie =>
        {
            cookie.Cookie.Name = IdentityAccessConstants.AuthCookieName;
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.IsEssential = true;
            cookie.Cookie.Path = "/";
            cookie.Cookie.SameSite = SameSiteMode.Strict;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            cookie.SlidingExpiration = false;
            cookie.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = RejectUnauthenticatedApiRedirectAsync,
                OnRedirectToAccessDenied = RejectForbiddenApiRedirectAsync,
            };
        });
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<IOptions<IdentityAccessOptions>>((cookie, configured) =>
            {
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(configured.Value.SessionMinutes);
            });
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme)
            .Configure(cookie =>
            {
                cookie.Cookie.Name = "__Host-HospitalManagement.2FA";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.IsEssential = true;
                cookie.Cookie.Path = "/";
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = IdentityAccessConstants.AntiforgeryHeaderName;
            options.Cookie.Name = "__Host-HospitalManagement.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Path = "/";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        services.AddAuthorization();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IIdentityLifecycleService, IdentityLifecycleService>();
        services.AddScoped<IIdentityDataSeeder, IdentityDataSeeder>();
        services.AddScoped<IIdentityContactLookup, IdentityContactLookup>();

        return services;
    }

    private static Task RejectUnauthenticatedApiRedirectAsync(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = context.Request.Path.StartsWithSegments("/api", StringComparison.Ordinal)
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private static Task RejectForbiddenApiRedirectAsync(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
