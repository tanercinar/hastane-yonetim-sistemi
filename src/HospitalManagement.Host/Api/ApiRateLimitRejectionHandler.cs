using System.Globalization;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HospitalManagement.Host.Api;

public static class ApiRateLimitRejectionHandler
{
    public static async ValueTask HandleAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        await ApiProblemResponseWriter.WriteAsync(
            context.HttpContext,
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
            },
            cancellationToken);
    }
}
