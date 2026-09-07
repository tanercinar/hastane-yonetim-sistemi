using HospitalManagement.Modules.IdentityAccess.Infrastructure;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Identity;

public sealed class IdentityAntiforgeryEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "İstek doğrulanamadı.",
                Detail = $"Geçerli bir {IdentityAccessConstants.AntiforgeryHeaderName} değeri gereklidir.",
            });
        }

        return await next(context);
    }
}
