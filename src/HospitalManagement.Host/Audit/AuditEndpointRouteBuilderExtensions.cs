using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Host.Authorization;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Audit;

public static class AuditEndpointRouteBuilderExtensions
{
    public static WebApplication MapAuditEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/audit")
            .WithGroupName("v1")
            .WithTags("Audit");

        group.MapGet("/logs", QueryAuditLogsAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.AuditTechnicalView)
            .WithName("QueryAuditLogs")
            .Produces<IReadOnlyList<AuditEvent>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/integrity-check/{id:guid}", VerifyAuditIntegrityAsync)
            .RequirePermission(HospitalPermissions.ReportingAndAudit.AuditTechnicalView)
            .WithName("VerifyAuditIntegrity")
            .Produces<AuditIntegrityResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> QueryAuditLogsAsync(
        [FromQuery] Guid? actorUserId,
        [FromQuery] Guid? actorPersonId,
        [FromQuery] string? action,
        [FromQuery] string? targetResourceType,
        [FromQuery] string? targetResourceId,
        [FromQuery] AuditOutcome? outcome,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        IAuditLogReader reader,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilter(
            actorUserId,
            actorPersonId,
            action,
            targetResourceType,
            targetResourceId,
            outcome,
            fromUtc,
            toUtc,
            page ?? 1,
            pageSize ?? 50);

        var logs = await reader.QueryLogsAsync(filter, cancellationToken);
        return Results.Ok(logs);
    }

    private static async Task<IResult> VerifyAuditIntegrityAsync(
        Guid id,
        IAuditLogReader reader,
        CancellationToken cancellationToken)
    {
        var isTamperFree = await reader.VerifyTamperIntegrityAsync(id, cancellationToken);
        return Results.Ok(new AuditIntegrityResponse(id, isTamperFree));
    }
}

public sealed record AuditIntegrityResponse(Guid Id, bool IsTamperFree);

