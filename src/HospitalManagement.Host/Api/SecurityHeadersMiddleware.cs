using HospitalManagement.BuildingBlocks.Security;

namespace HospitalManagement.Host.Api;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = SecurityHeaderDefaults.XContentTypeOptions;
            headers["X-Frame-Options"] = SecurityHeaderDefaults.XFrameOptions;
            headers["Referrer-Policy"] = SecurityHeaderDefaults.ReferrerPolicy;
            headers["Permissions-Policy"] = SecurityHeaderDefaults.PermissionsPolicy;
            headers["X-XSS-Protection"] = SecurityHeaderDefaults.XXssProtection;

            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers["Content-Security-Policy"] = SecurityHeaderDefaults.ContentSecurityPolicy;
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
