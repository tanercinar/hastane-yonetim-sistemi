using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Api;

public static class ApiProblemResponseWriter
{
    public static async Task WriteAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(problemDetails);

        ApiProblemDetailsDefaults.Apply(httpContext, problemDetails);
        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
        });

        if (written)
        {
            return;
        }

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: ApiConstants.ProblemContentType,
            cancellationToken: cancellationToken);
    }
}
