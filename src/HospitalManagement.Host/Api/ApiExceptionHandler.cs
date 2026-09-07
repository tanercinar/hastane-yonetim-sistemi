using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace HospitalManagement.Host.Api;

public sealed partial class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [LoggerMessage(
        EventId = 2200,
        Level = LogLevel.Error,
        Message = "API istegi beklenmeyen bir hatayla sonlandi. ExceptionType={ExceptionType} CorrelationId={CorrelationId}")]
    private static partial void LogUnhandledApiException(
        ILogger logger,
        string exceptionType,
        string correlationId);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var originalRequestPath = httpContext.Features
            .Get<IExceptionHandlerPathFeature>()
            ?.Path;

        if (!ApiRequestClassifier.IsApiPath(originalRequestPath ?? httpContext.Request.Path))
        {
            return false;
        }

        var isConcurrencyConflict = exception is DbUpdateConcurrencyException
            || exception is PostgresException postgresException
                && IsConcurrencySqlState(postgresException.SqlState)
            || exception is DbUpdateException
            {
                InnerException: PostgresException innerPostgresException,
            } && IsConcurrencySqlState(innerPostgresException.SqlState);

        if (!isConcurrencyConflict)
        {
            LogUnhandledApiException(
                _logger,
                exception.GetType().FullName ?? exception.GetType().Name,
                httpContext.TraceIdentifier);
        }

        await ApiProblemResponseWriter.WriteAsync(
            httpContext,
            new ProblemDetails
            {
                Status = isConcurrencyConflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status500InternalServerError,
                Title = isConcurrencyConflict
                    ? "Eşzamanlı İşlem Çakışması"
                    : null,
            },
            cancellationToken);
        return true;
    }

    private static bool IsConcurrencySqlState(string sqlState) =>
        sqlState is PostgresErrorCodes.UniqueViolation
            or PostgresErrorCodes.ExclusionViolation
            or PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected;
}
