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

public static class IcuEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIcuEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var icuGroup = endpoints.MapGroup("/api/v1/icu")
            .RequireAuthorization();

        icuGroup.MapGet("/beds", GetIcuBedsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetIcuBeds")
            .Produces<List<IcuBedResponse>>();

        var admissionsGroup = icuGroup.MapGroup("/admissions");

        admissionsGroup.MapPost(string.Empty, AdmitToIcuAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AdmitToIcu")
            .Produces<IcuAdmissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/care-plan", UpdateIcuCarePlanAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdateIcuCarePlan")
            .Produces<IcuAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/discharge-or-transfer", DischargeOrTransferAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("DischargeOrTransferFromIcu")
            .Produces<IcuAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapGet("/active", GetActiveAdmissionsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetActiveIcuAdmissions")
            .Produces<List<IcuAdmissionResponse>>();

        admissionsGroup.MapGet("/{id:guid}", GetIcuAdmissionByIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetIcuAdmissionById")
            .Produces<IcuAdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Flowsheet endpoints
        admissionsGroup.MapPost("/{id:guid}/flowsheet", AddFlowsheetEntryAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AddIcuFlowsheetEntry")
            .Produces<IcuFlowsheetEntryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admissionsGroup.MapGet("/{id:guid}/flowsheet", GetFlowsheetEntriesAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetIcuFlowsheetEntries")
            .Produces<List<IcuFlowsheetEntryResponse>>();

        admissionsGroup.MapGet("/{id:guid}/flowsheet/fluid-balance", GetFluidBalanceSummaryAsync)
            .RequirePermission(HospitalPermissions.Inpatient.CriticalCareRecord)
            .WithName("GetIcuFluidBalanceSummary")
            .Produces<IcuFluidBalanceSummaryResponse>();

        return endpoints;
    }

    private static async Task<IResult> GetIcuBedsAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseIcuWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        var list = await service.GetIcuBedsAsync(cancellationToken);
        return Results.Ok(list.Select(b => new IcuBedResponse(
            b.Id,
            b.BedCode,
            b.BedName,
            b.UnitName,
            b.IsActive,
            b.IsOccupied,
            b.CurrentAdmissionId,
            b.CurrentPatientProtocolNumber)).ToList());
    }

    private static async Task<IResult> AdmitToIcuAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateIcuAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanCreateIcuAdmissionAsync(
                user,
                request.PatientId,
                request.AttendingDoctorId,
                request.PrimaryNurseId,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<IcuAcuityLevel>(request.AcuityLevel, true, out var acuity))
        {
            return InvalidEnumValue(nameof(request.AcuityLevel), request.AcuityLevel, Enum.GetNames<IcuAcuityLevel>());
        }

        if (!Enum.TryParse<IcuVentilationMode>(request.VentilationMode, true, out var ventilation))
        {
            return InvalidEnumValue(nameof(request.VentilationMode), request.VentilationMode, Enum.GetNames<IcuVentilationMode>());
        }

        var dto = new CreateIcuAdmissionDto(
            request.InpatientStayId,
            request.PatientId,
            request.EncounterId,
            request.IcuBedId,
            request.AttendingDoctorId,
            request.PrimaryNurseId,
            request.AdmissionReason,
            acuity,
            request.MonitoringFrequencyMinutes,
            ventilation,
            request.CarePlanNotes);

        var result = await service.AdmitToIcuAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, adm => Results.Created($"/api/v1/icu/admissions/{adm.Id}", MapToResponse(adm)));
    }

    private static async Task<IResult> UpdateIcuCarePlanAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateIcuCarePlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<IcuAcuityLevel>(request.AcuityLevel, true, out var acuity))
        {
            return InvalidEnumValue(nameof(request.AcuityLevel), request.AcuityLevel, Enum.GetNames<IcuAcuityLevel>());
        }

        if (!Enum.TryParse<IcuVentilationMode>(request.VentilationMode, true, out var ventilation))
        {
            return InvalidEnumValue(nameof(request.VentilationMode), request.VentilationMode, Enum.GetNames<IcuVentilationMode>());
        }

        var dto = new UpdateIcuCarePlanDto(
            acuity,
            request.MonitoringFrequencyMinutes,
            ventilation,
            request.PrimaryNurseId,
            request.CarePlanNotes);

        var result = await service.UpdateCarePlanAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, adm => Results.Ok(MapToResponse(adm)));
    }

    private static async Task<IResult> DischargeOrTransferAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] IcuDischargeOrTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<IcuAdmissionStatus>(request.DestinationStatus, true, out var destStatus)
            || destStatus == IcuAdmissionStatus.Active)
        {
            return InvalidEnumValue(
                nameof(request.DestinationStatus),
                request.DestinationStatus,
                [nameof(IcuAdmissionStatus.TransferredToWard), nameof(IcuAdmissionStatus.Discharged), nameof(IcuAdmissionStatus.Deceased)]);
        }

        var dto = new IcuDischargeOrTransferDto(destStatus, request.DischargeNotes);

        var result = await service.DischargeOrTransferAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, adm => Results.Ok(MapToResponse(adm)));
    }

    private static async Task<IResult> GetActiveAdmissionsAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var list = await service.GetActiveAdmissionsAsync(cancellationToken);
        var accessible = new List<IcuAdmissionResponse>();
        foreach (var admission in list)
        {
            if (await accessControl.CanAccessIcuAdmissionAsync(user, admission, cancellationToken))
            {
                accessible.Add(MapToResponse(admission));
            }
        }

        return Results.Ok(accessible);
    }

    private static async Task<IResult> GetIcuAdmissionByIdAsync(
        IIcuAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var adm = await service.GetByIdAsync(id, cancellationToken);
        return adm is null ? Results.NotFound() : Results.Ok(MapToResponse(adm));
    }

    private static async Task<IResult> AddFlowsheetEntryAsync(
        IIcuFlowsheetService flowsheetService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CreateIcuFlowsheetEntryRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<IcuVentilationMode>(request.VentilationMode, true, out var ventilation))
        {
            return InvalidEnumValue(nameof(request.VentilationMode), request.VentilationMode, Enum.GetNames<IcuVentilationMode>());
        }

        var dto = new CreateIcuFlowsheetEntryDto(
            id,
            request.RecordedAtUtc,
            request.HeartRateBpm,
            request.SystolicBpMmHg,
            request.DiastolicBpMmHg,
            request.RespiratoryRateBpm,
            request.OxygenSaturationPct,
            request.BodyTemperatureCelsius,
            request.GlasgowComaScale,
            request.RichmondAgitationSedationScale,
            ventilation,
            request.FractionOfInspiredOxygenPct,
            request.PositiveEndExpiratoryPressure,
            request.TidalVolumeMl,
            request.PeakInspiratoryPressure,
            request.IvFluidIntakeMl,
            request.EnteralNutritionIntakeMl,
            request.UrineOutputMl,
            request.DrainOutputMl,
            request.ClinicalNotes);

        var result = await flowsheetService.AddFlowsheetEntryAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, entry => Results.Created($"/api/v1/icu/admissions/{id}/flowsheet/{entry.Id}", MapToFlowsheetResponse(entry)));
    }

    private static async Task<IResult> GetFlowsheetEntriesAsync(
        IIcuFlowsheetService flowsheetService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var list = await flowsheetService.GetFlowsheetEntriesAsync(id, fromUtc, toUtc, cancellationToken);
        return Results.Ok(list.Select(MapToFlowsheetResponse).ToList());
    }

    private static async Task<IResult> GetFluidBalanceSummaryAsync(
        IIcuFlowsheetService flowsheetService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessIcuAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var summary = await flowsheetService.GetFluidBalanceSummaryAsync(id, fromUtc, toUtc, cancellationToken);
        return Results.Ok(new IcuFluidBalanceSummaryResponse(
            summary.IcuAdmissionId,
            summary.FromUtc,
            summary.ToUtc,
            summary.TotalIvIntakeMl,
            summary.TotalEnteralIntakeMl,
            summary.TotalIntakeMl,
            summary.TotalUrineOutputMl,
            summary.TotalDrainOutputMl,
            summary.TotalOutputMl,
            summary.NetBalanceMl,
            summary.EntryCount));
    }

    private static IcuFlowsheetEntryResponse MapToFlowsheetResponse(IcuFlowsheetEntryDto e) =>
        new(
            e.Id,
            e.IcuAdmissionId,
            e.RecordedAtUtc,
            e.RecordedByStaffId,
            e.HeartRateBpm,
            e.SystolicBpMmHg,
            e.DiastolicBpMmHg,
            e.MeanArterialPressureMmHg,
            e.RespiratoryRateBpm,
            e.OxygenSaturationPct,
            e.BodyTemperatureCelsius,
            e.GlasgowComaScale,
            e.RichmondAgitationSedationScale,
            e.VentilationMode.ToString(),
            e.FractionOfInspiredOxygenPct,
            e.PositiveEndExpiratoryPressure,
            e.TidalVolumeMl,
            e.PeakInspiratoryPressure,
            e.IvFluidIntakeMl,
            e.EnteralNutritionIntakeMl,
            e.UrineOutputMl,
            e.DrainOutputMl,
            e.TotalIntakeMl,
            e.TotalOutputMl,
            e.NetFluidBalanceMl,
            e.ClinicalNotes,
            e.CreatedAtUtc);

    private static IcuAdmissionResponse MapToResponse(IcuAdmissionDto a) =>
        new(
            a.Id,
            a.AdmissionProtocolNumber,
            a.InpatientStayId,
            a.PatientId,
            a.EncounterId,
            a.IcuBedId,
            a.IcuBedCode,
            a.AttendingDoctorId,
            a.PrimaryNurseId,
            a.AdmissionReason,
            a.AcuityLevel.ToString(),
            a.MonitoringFrequencyMinutes,
            a.VentilationMode.ToString(),
            a.Status.ToString(),
            a.CarePlanNotes,
            a.AdmittedAtUtc,
            a.DischargedAtUtc,
            a.DischargeNotes,
            a.CreatedAtUtc,
            a.UpdatedAtUtc,
            a.Version);

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
