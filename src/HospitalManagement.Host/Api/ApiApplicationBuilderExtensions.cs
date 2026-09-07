using HospitalManagement.Host.Observability;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Api;

public static class ApiApplicationBuilderExtensions
{
    public static WebApplication UseApiFoundation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseRouting();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestTelemetryMiddleware>();
        app.UseExceptionHandler("/error", createScopeForErrors: true);
        app.UseWhen(
            ApiRequestClassifier.IsApiRequest,
            apiBranch => apiBranch.UseStatusCodePages(async statusCodeContext =>
            {
                await ApiProblemResponseWriter.WriteAsync(
                    statusCodeContext.HttpContext,
                    new ProblemDetails
                    {
                        Status = statusCodeContext.HttpContext.Response.StatusCode,
                    },
                    statusCodeContext.HttpContext.RequestAborted);
            }));
        app.UseWhen(
            context => !ApiRequestClassifier.IsApiRequest(context),
            uiBranch => uiBranch.UseStatusCodePagesWithReExecute(
                "/not-found",
                createScopeForStatusCodePages: true));
        app.UseRateLimiter();

        return app;
    }
}
