using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Surgery;

public static class ClinicalHandoffEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapClinicalHandoffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/clinical-handoffs")
            .RequireAuthorization();

        group.MapPost(string.Empty, InitiateHandoffAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("InitiateClinicalHandoff")
            .Produces<ClinicalHandoffResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/accept", AcceptHandoffAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AcceptClinicalHandoff")
            .Produces<ClinicalHandoffResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/reject", RejectHandoffAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RejectClinicalHandoff")
            .Produces<ClinicalHandoffResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/cancel", CancelHandoffAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelClinicalHandoff")
            .Produces<ClinicalHandoffResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/pending", GetPendingHandoffsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetPendingClinicalHandoffs")
            .Produces<List<ClinicalHandoffResponse>>();

        group.MapGet("/patient/{patientId:guid}", GetHandoffsByPatientIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetClinicalHandoffsByPatientId")
            .Produces<List<ClinicalHandoffResponse>>();

        group.MapGet("/{id:guid}", GetHandoffByIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetClinicalHandoffById")
            .Produces<ClinicalHandoffResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> InitiateHandoffAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] InitiateClinicalHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanInitiateClinicalHandoffAsync(user, request.PatientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<ClinicalAreaType>(request.SourceArea, true, out var source))
        {
            return InvalidEnumValue(nameof(request.SourceArea), request.SourceArea, Enum.GetNames<ClinicalAreaType>());
        }

        if (!Enum.TryParse<ClinicalAreaType>(request.DestinationArea, true, out var dest))
        {
            return InvalidEnumValue(nameof(request.DestinationArea), request.DestinationArea, Enum.GetNames<ClinicalAreaType>());
        }

        var dto = new InitiateClinicalHandoffDto(
            request.PatientId,
            request.InpatientStayId,
            request.EncounterId,
            source,
            request.SourceLocationDetails,
            dest,
            request.DestinationLocationDetails,
            request.Situation,
            request.Background,
            request.Assessment,
            request.Recommendation,
            request.CriticalAlerts);

        var result = await service.InitiateHandoffAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, h => Results.Created($"/api/v1/clinical-handoffs/{h.Id}", MapToResponse(h)));
    }

    private static async Task<IResult> AcceptHandoffAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AcceptClinicalHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessClinicalHandoffAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.AcceptHandoffAsync(id, staffId, request.AcceptanceNote, cancellationToken);
        return ToHttpResult(result, h => Results.Ok(MapToResponse(h)));
    }

    private static async Task<IResult> RejectHandoffAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RejectClinicalHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessClinicalHandoffAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.RejectHandoffAsync(id, staffId, request.RejectionReason, cancellationToken);
        return ToHttpResult(result, h => Results.Ok(MapToResponse(h)));
    }

    private static async Task<IResult> CancelHandoffAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CancelClinicalHandoffRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessClinicalHandoffAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.CancelHandoffAsync(id, staffId, request.CancelReason, cancellationToken);
        return ToHttpResult(result, h => Results.Ok(MapToResponse(h)));
    }

    private static async Task<IResult> GetPendingHandoffsAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromQuery] string? destinationArea,
        CancellationToken cancellationToken)
    {
        ClinicalAreaType? dest = null;
        if (!string.IsNullOrWhiteSpace(destinationArea) && Enum.TryParse<ClinicalAreaType>(destinationArea, true, out var d))
        {
            dest = d;
        }

        var list = await service.GetPendingHandoffsAsync(dest, cancellationToken);
        var accessible = new List<ClinicalHandoffResponse>();
        foreach (var handoff in list)
        {
            if (await accessControl.CanAccessClinicalHandoffAsync(user, handoff, cancellationToken))
            {
                accessible.Add(MapToResponse(handoff));
            }
        }

        return Results.Ok(accessible);
    }

    private static async Task<IResult> GetHandoffsByPatientIdAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanInitiateClinicalHandoffAsync(user, patientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var list = await service.GetHandoffsByPatientIdAsync(patientId, cancellationToken);
        return Results.Ok(list.Select(MapToResponse).ToList());
    }

    private static async Task<IResult> GetHandoffByIdAsync(
        IClinicalHandoffService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessClinicalHandoffAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var handoff = await service.GetByIdAsync(id, cancellationToken);
        return handoff is null ? Results.NotFound() : Results.Ok(MapToResponse(handoff));
    }

    private static ClinicalHandoffResponse MapToResponse(ClinicalHandoffDto h) =>
        new(
            h.Id,
            h.HandoffProtocolNumber,
            h.PatientId,
            h.InpatientStayId,
            h.EncounterId,
            h.SourceArea.ToString(),
            h.SourceLocationDetails,
            h.DestinationArea.ToString(),
            h.DestinationLocationDetails,
            h.HandingOverStaffId,
            h.ReceivingStaffId,
            h.Situation,
            h.Background,
            h.Assessment,
            h.Recommendation,
            h.CriticalAlerts,
            h.Status.ToString(),
            h.StatusReason,
            h.HandedOverAtUtc,
            h.AcceptedAtUtc,
            h.CreatedAtUtc,
            h.UpdatedAtUtc,
            h.Version);

    private static Guid ExtractActorId(ClaimsPrincipal actor)
    {
        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var personId) && personId != Guid.Empty)
        {
            return personId;
        }

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }

    private static IResult ToHttpResult<T>(SurgeryOperationResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            SurgeryOperationStatus.Success => onSuccess(result.Value!),
            SurgeryOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage),
            SurgeryOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage),
            SurgeryOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors?.Count > 0
                    ? result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value)
                    : new Dictionary<string, string[]> { ["General"] = [result.ErrorMessage ?? "Doğrulama hatası."] }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static IResult InvalidEnumValue(string fieldName, string? value, IReadOnlyList<string> allowedValues) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [fieldName] = [$"'{value}' geçerli değildir. İzin verilen değerler: {string.Join(", ", allowedValues)}."],
        });
}
