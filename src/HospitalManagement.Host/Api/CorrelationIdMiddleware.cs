namespace HospitalManagement.Host.Api;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const int MaximumCorrelationIdLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = GetIncomingCorrelationId(context) ?? Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ApiConstants.CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }

    private static string? GetIncomingCorrelationId(HttpContext context)
    {
        var values = context.Request.Headers[ApiConstants.CorrelationIdHeaderName];
        if (values.Count != 1)
        {
            return null;
        }

        var candidate = values[0];
        if (string.IsNullOrWhiteSpace(candidate)
            || candidate.Length > MaximumCorrelationIdLength
            || !candidate.All(IsSafeCorrelationIdCharacter))
        {
            return null;
        }

        return candidate;
    }

    private static bool IsSafeCorrelationIdCharacter(char character)
    {
        return char.IsAsciiLetterOrDigit(character)
            || character is '-' or '_' or '.';
    }
}
