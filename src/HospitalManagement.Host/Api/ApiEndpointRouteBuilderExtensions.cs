using HospitalManagement.Contracts.Platform;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Configuration;
using HospitalManagement.Host.Health;
using HospitalManagement.Host.Observability;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Host.Api;

public static class ApiEndpointRouteBuilderExtensions
{
    private const string TestingEnvironmentName = "Testing";

    public static WebApplication MapApiFoundationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionOne = app.MapGroup(ApiConstants.VersionOnePrefix)
            .WithGroupName(ApiConstants.Version)
            .WithTags("Platform")
            .RequireRateLimiting(ApiConstants.RateLimitPolicyName);

        versionOne.MapGet("/platform/status", GetApiStatus)
            .WithName("GetApiStatus")
            .WithSummary("API sürümünü ve güvenli çalışma modunu döndürür.")
            .Produces<ApiStatusResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(
                StatusCodes.Status429TooManyRequests,
                ApiConstants.ProblemContentType);

        if (app.Environment.IsEnvironment(TestingEnvironmentName))
        {
            versionOne.MapPost("/platform/validation-probe", ValidateRequestContract)
                .ExcludeFromDescription();
            versionOne.MapGet("/platform/failure-probe", ThrowFailureProbe)
                .ExcludeFromDescription();
            versionOne.MapPost("/platform/telemetry-probe/{resourceId}", TelemetryProbe)
                .ExcludeFromDescription();
            versionOne.MapGet("/platform/auth-probes/organization-manage", () => TypedResults.Ok("org-manage-ok"))
                .RequirePermission(HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.Organization.Manage)
                .ExcludeFromDescription();
            versionOne.MapGet("/platform/auth-probes/profile-view", () => TypedResults.Ok("profile-view-ok"))
                .RequirePermission(HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.Identity.ProfileViewOwn)
                .ExcludeFromDescription();
            versionOne.MapGet("/platform/auth-probes/unknown-permission", () => TypedResults.Ok("unknown-ok"))
                .RequirePermission("nonexistent.custom.permission")
                .ExcludeFromDescription();
            versionOne.MapGet("/platform/auth-probes/clinical-note-sign", () => TypedResults.Ok("clinical-sign-ok"))
                .RequirePermission(HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.ClinicalRecords.ClinicalNoteSign)
                .ExcludeFromDescription();

            versionOne.MapGet("/platform/resource-probes/patient-record/{patientId:guid}", async (
                Guid patientId,
                HttpContext context,
                Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService) =>
            {
                var resource = HospitalManagement.BuildingBlocks.Authorization.ResourceScopedData.ForPatient(patientId);
                var requirement = new HospitalManagement.Host.Authorization.ResourceScopeAuthorizationRequirement(
                    HospitalManagement.BuildingBlocks.Authorization.ResourceScope.Own,
                    HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.Patient.ViewOwn);

                var result = await authorizationService.AuthorizeAsync(context.User, resource, [requirement]);
                return result.Succeeded
                    ? Results.Ok(new
                    {
                        PatientId = patientId,
                        Message = "patient-record-ok"
                    })
                    : Results.Forbid();
            }).ExcludeFromDescription();

            versionOne.MapGet("/platform/resource-probes/clinical-encounter/{patientId:guid}", async (
                Guid patientId,
                HttpContext context,
                Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService) =>
            {
                var resource = HospitalManagement.BuildingBlocks.Authorization.ResourceScopedData.ForClinicalEncounter(patientId);
                var requirement = new HospitalManagement.Host.Authorization.ResourceScopeAuthorizationRequirement(
                    HospitalManagement.BuildingBlocks.Authorization.ResourceScope.CareTeam,
                    HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.ClinicalRecords.EncounterView);

                var result = await authorizationService.AuthorizeAsync(context.User, resource, [requirement]);
                return result.Succeeded
                    ? Results.Ok(new
                    {
                        PatientId = patientId,
                        Message = "clinical-encounter-ok"
                    })
                    : Results.Forbid();
            }).ExcludeFromDescription();

            versionOne.MapPost("/platform/resource-probes/department-record/{departmentId:guid}", async (
                Guid departmentId,
                HttpContext context,
                Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService) =>
            {
                var resource = HospitalManagement.BuildingBlocks.Authorization.ResourceScopedData.ForDepartment(departmentId);
                var requirement = new HospitalManagement.Host.Authorization.ResourceScopeAuthorizationRequirement(
                    HospitalManagement.BuildingBlocks.Authorization.ResourceScope.Department,
                    HospitalManagement.BuildingBlocks.Authorization.HospitalPermissions.ClinicalRecords.ObservationRecordVital);

                var result = await authorizationService.AuthorizeAsync(context.User, resource, [requirement]);
                return result.Succeeded
                    ? Results.Ok(new
                    {
                        DepartmentId = departmentId,
                        Message = "department-record-ok"
                    })
                    : Results.Forbid();
            }).ExcludeFromDescription();

            versionOne.MapPost("/platform/resource-probes/care-relationships", (
                EstablishCareRelationshipRequest request,
                HospitalManagement.Host.Authorization.CareRelationshipRegistry registry) =>
            {
                registry.EstablishCareRelationship(request.ClinicianPersonId, request.PatientPersonId);
                return TypedResults.Ok("established");
            }).ExcludeFromDescription();

            versionOne.MapPost("/platform/sensitive-probes/sensitive-operation", async (
                HttpContext context,
                Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService) =>
            {
                var requirement = new HospitalManagement.Host.Authorization.RecentAuthenticationRequirement(
                    TimeSpan.FromMinutes(5));
                var result = await authorizationService.AuthorizeAsync(context.User, null, [requirement]);
                return result.Succeeded
                    ? Results.Ok("sensitive-operation-ok")
                    : Results.Forbid();
            }).ExcludeFromDescription();
        }

        if (app.Environment.IsDevelopment()
            || app.Environment.IsEnvironment(TestingEnvironmentName))
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponseWriter.WriteAsync,
        })
            .WithName("Liveness")
            .DisableRateLimiting()
            .ExcludeFromDescription();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ApiConstants.ReadinessTag),
            ResponseWriter = HealthResponseWriter.WriteAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
        })
            .WithName("Readiness")
            .DisableRateLimiting()
            .ExcludeFromDescription();

        return app;
    }

    private static Ok<ApiStatusResponse> GetApiStatus(
        HttpContext httpContext,
        IOptions<RuntimeOptions> runtimeOptions)
    {
        return TypedResults.Ok(new ApiStatusResponse(
            "HospitalManagement.Api",
            ApiConstants.Version,
            runtimeOptions.Value.DataMode,
            httpContext.TraceIdentifier));
    }

    private static Ok<ValidationProbeResponse> ValidateRequestContract(
        ValidationProbeRequest request)
    {
        return TypedResults.Ok(new ValidationProbeResponse(
            Accepted: true,
            ClientName: request.ClientName!));
    }

    private static IResult ThrowFailureProbe()
    {
        throw new InvalidOperationException(
            "DEMO API failure probe must never be returned to a client.");
    }

    private static NoContent TelemetryProbe(
        string resourceId,
        ValidationProbeRequest request,
        ILogger<RequestTelemetryMiddleware> logger)
    {
        _ = resourceId;
        ObservabilityLog.LogRedactionProbe(logger, request.ClientName!);
        return TypedResults.NoContent();
    }

    private sealed record ValidationProbeResponse(bool Accepted, string ClientName);

    public sealed record EstablishCareRelationshipRequest(Guid ClinicianPersonId, Guid PatientPersonId);
}

