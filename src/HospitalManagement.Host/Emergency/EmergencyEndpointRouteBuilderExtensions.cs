using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Emergency;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Emergency.Application;
using HospitalManagement.Modules.Emergency.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Emergency;

public static class EmergencyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEmergencyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var emergencyGroup = endpoints.MapGroup("/api/v1/emergency")
            .RequireAuthorization();

        var admissionsGroup = emergencyGroup.MapGroup("/admissions");

        admissionsGroup.MapPost(string.Empty, CreateAdmissionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateEmergencyAdmission")
            .Produces<EmergencyAdmissionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/triage", RecordTriageAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordEmergencyTriage")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/assign-doctor", AssignDoctorAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AssignEmergencyDoctor")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/status", UpdateStatusAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("UpdateEmergencyAdmissionStatus")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapPost("/{id:guid}/disposition", RecordDispositionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordEmergencyDisposition")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admissionsGroup.MapGet("/{id:guid}", GetAdmissionByIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyAdmissionById")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admissionsGroup.MapGet("/active/by-patient/{patientId:guid}", GetActiveAdmissionByPatientIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetActiveEmergencyAdmissionByPatientId")
            .Produces<EmergencyAdmissionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        admissionsGroup.MapGet(string.Empty, GetAdmissionsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyAdmissions")
            .Produces<List<EmergencyAdmissionSummaryResponse>>();

        var boardGroup = emergencyGroup.MapGroup("/board");

        boardGroup.MapGet("/summary", GetBoardSummaryAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyBoardSummary")
            .Produces<EmergencyBoardSummaryResponse>();

        boardGroup.MapGet("/worklist", GetBoardWorklistAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyBoardWorklist")
            .Produces<List<EmergencyBoardWorklistItemResponse>>();

        var ordersGroup = emergencyGroup.MapGroup("/orders");

        ordersGroup.MapPost(string.Empty, CreateOrderAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateEmergencyOrder")
            .Produces<EmergencyOrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        ordersGroup.MapPost("/{id:guid}/complete", CompleteOrderAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CompleteEmergencyOrder")
            .Produces<EmergencyOrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        ordersGroup.MapPost("/{id:guid}/cancel", CancelOrderAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelEmergencyOrder")
            .Produces<EmergencyOrderResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        ordersGroup.MapGet("/by-admission/{admissionId:guid}", GetOrdersByAdmissionIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyOrdersByAdmissionId")
            .Produces<List<EmergencyOrderResponse>>();

        var consultationsGroup = emergencyGroup.MapGroup("/consultations");

        consultationsGroup.MapPost(string.Empty, RequestConsultationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RequestEmergencyConsultation")
            .Produces<EmergencyConsultationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        consultationsGroup.MapPost("/{id:guid}/accept", AcceptConsultationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AcceptEmergencyConsultation")
            .Produces<EmergencyConsultationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        consultationsGroup.MapPost("/{id:guid}/respond", RespondConsultationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RespondEmergencyConsultation")
            .Produces<EmergencyConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        consultationsGroup.MapPost("/{id:guid}/cancel", CancelConsultationAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelEmergencyConsultation")
            .Produces<EmergencyConsultationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        consultationsGroup.MapGet("/by-admission/{admissionId:guid}", GetConsultationsByAdmissionIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.EmergencyTriageRecord)
            .WithName("GetEmergencyConsultationsByAdmissionId")
            .Produces<List<EmergencyConsultationResponse>>();

        return endpoints;
    }

    private static async Task<IResult> GetBoardSummaryAsync(
        IEmergencyTrackingBoardService boardService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseEmergencyWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        var summary = await boardService.GetBoardSummaryAsync(cancellationToken);
        return Results.Ok(new EmergencyBoardSummaryResponse(
            summary.TotalActiveAdmissions,
            summary.WaitingTriageCount,
            summary.TriagedWaitingDoctorCount,
            summary.InEvaluationCount,
            summary.InObservationCount,
            summary.AdmittedPendingTransferCount,
            summary.TodayDischargedCount,
            summary.Red1Count,
            summary.Red2Count,
            summary.YellowCount,
            summary.GreenCount,
            summary.BlackCount,
            summary.AverageWaitMinutesTriage,
            summary.AverageWaitMinutesDoctor,
            summary.AverageLengthOfStayMinutes));
    }

    private static async Task<IResult> GetBoardWorklistAsync(
        IEmergencyTrackingBoardService boardService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromQuery] string? status,
        [FromQuery] string? triageLevel,
        [FromQuery] string? zone,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseEmergencyWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        EmergencyAdmissionStatus? parsedStatus = Enum.TryParse<EmergencyAdmissionStatus>(status, true, out var st) ? st : null;
        TriageLevel? parsedTriage = Enum.TryParse<TriageLevel>(triageLevel, true, out var tr) ? tr : null;

        var items = await boardService.GetBoardWorklistAsync(parsedStatus, parsedTriage, zone, cancellationToken);
        var responses = items.Select(i => new EmergencyBoardWorklistItemResponse(
            i.AdmissionId,
            i.EmergencyProtocolNumber,
            i.PatientId,
            i.ArrivalType.ToString(),
            i.ChiefComplaint,
            i.Status.ToString(),
            i.TriageLevel?.ToString(),
            i.TriageCategoryReason,
            i.AdmittedAtUtc,
            i.TriagedAtUtc,
            i.AssignedDoctorId,
            i.AssignedBedOrZone,
            i.WaitingMinutes,
            i.DoctorWaitingMinutes)).ToList();

        return Results.Ok(responses);
    }

    private static async Task<IResult> CreateAdmissionAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateEmergencyAdmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseEmergencyWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<EmergencyArrivalType>(request.ArrivalType, true, out var arrivalType))
        {
            return InvalidEnumValue(nameof(request.ArrivalType), request.ArrivalType, Enum.GetNames<EmergencyArrivalType>());
        }

        var dto = new CreateEmergencyAdmissionDto
        {
            PatientId = request.PatientId,
            ArrivalType = arrivalType,
            ChiefComplaint = request.ChiefComplaint,
            AdmissionNotes = request.AdmissionNotes,
        };

        var result = await service.CreateAdmissionAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, admission => Results.Created($"/api/v1/emergency/admissions/{admission.Id}", MapToResponse(admission)));
    }

    private static async Task<IResult> RecordTriageAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RecordTriageRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<TriageLevel>(request.TriageLevel, true, out var triageLevel))
        {
            return InvalidEnumValue(nameof(request.TriageLevel), request.TriageLevel, Enum.GetNames<TriageLevel>());
        }

        var dto = new RecordTriageDto
        {
            TriageLevel = triageLevel,
            TriageCategoryReason = request.TriageCategoryReason,
            SystolicBp = request.SystolicBp,
            DiastolicBp = request.DiastolicBp,
            HeartRate = request.HeartRate,
            BodyTemperatureCelsius = request.BodyTemperatureCelsius,
            RespiratoryRate = request.RespiratoryRate,
            OxygenSaturationPercent = request.OxygenSaturationPercent,
            PainScale = request.PainScale,
            Consciousness = request.Consciousness,
            ClinicalNotes = request.ClinicalNotes,
        };

        var result = await service.RecordTriageAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToResponse(admission)));
    }

    private static async Task<IResult> AssignDoctorAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AssignEmergencyDoctorRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        var dto = new AssignDoctorDto
        {
            DoctorId = request.DoctorId,
            BedOrZone = request.BedOrZone,
        };

        var result = await service.AssignDoctorAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToResponse(admission)));
    }

    private static async Task<IResult> UpdateStatusAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] UpdateEmergencyAdmissionStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<EmergencyAdmissionStatus>(request.Status, true, out var status))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Status"] = ["Geçersiz acil başvuru durumu."] });
        }

        var dto = new UpdateEmergencyStatusDto
        {
            Status = status,
            Notes = request.Notes,
        };

        var result = await service.UpdateStatusAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToResponse(admission)));
    }

    private static async Task<IResult> RecordDispositionAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RecordEmergencyDispositionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);

        if (!Enum.TryParse<EmergencyDispositionType>(request.DispositionType, true, out var dispType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["DispositionType"] = ["Geçersiz disposition türü."] });
        }

        var dto = new RecordEmergencyDispositionDto(
            dispType,
            request.TargetWardOrIcuId,
            request.TargetDepartmentName,
            request.DispositionSummaryNotes,
            request.FollowUpInstructions);

        var result = await encounterService.RecordDispositionAsync(id, dto, doctorId, cancellationToken);
        return ToHttpResult(result, admission => Results.Ok(MapToResponse(admission)));
    }

    private static async Task<IResult> CreateOrderAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateEmergencyOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, request.AdmissionId, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);

        if (!Enum.TryParse<EmergencyOrderType>(request.OrderType, true, out var orderType))
        {
            return InvalidEnumValue(nameof(request.OrderType), request.OrderType, Enum.GetNames<EmergencyOrderType>());
        }

        if (!Enum.TryParse<EmergencyOrderPriority>(request.Priority, true, out var priority))
        {
            return InvalidEnumValue(nameof(request.Priority), request.Priority, Enum.GetNames<EmergencyOrderPriority>());
        }

        var dto = new CreateEmergencyOrderDto(
            request.AdmissionId,
            orderType,
            request.OrderCatalogCode,
            request.OrderCatalogName,
            priority,
            request.ClinicalInstructions);

        var result = await encounterService.CreateOrderAsync(dto, doctorId, cancellationToken);
        return ToHttpResult(result, order => Results.Created($"/api/v1/emergency/orders/{order.Id}", MapToOrderResponse(order)));
    }

    private static async Task<IResult> CompleteOrderAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CompleteEmergencyOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyOrderAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await encounterService.CompleteOrderAsync(id, request.ResultSummary, staffId, cancellationToken);
        return ToHttpResult(result, order => Results.Ok(MapToOrderResponse(order)));
    }

    private static async Task<IResult> CancelOrderAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CancelEmergencyOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyOrderAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await encounterService.CancelOrderAsync(id, request.Reason, staffId, cancellationToken);
        return ToHttpResult(result, order => Results.Ok(MapToOrderResponse(order)));
    }

    private static async Task<IResult> GetOrdersByAdmissionIdAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, admissionId, cancellationToken))
        {
            return Results.Forbid();
        }

        var orders = await encounterService.GetOrdersByAdmissionIdAsync(admissionId, cancellationToken);
        return Results.Ok(orders.Select(MapToOrderResponse).ToList());
    }

    private static async Task<IResult> RequestConsultationAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] RequestEmergencyConsultationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, request.AdmissionId, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);

        if (!Enum.TryParse<EmergencyConsultationUrgency>(request.Urgency, true, out var urgency))
        {
            return InvalidEnumValue(nameof(request.Urgency), request.Urgency, Enum.GetNames<EmergencyConsultationUrgency>());
        }

        var dto = new RequestEmergencyConsultationDto(
            request.AdmissionId,
            request.DepartmentId,
            request.DepartmentName,
            urgency,
            request.ClinicalReason);

        var result = await encounterService.RequestConsultationAsync(dto, doctorId, cancellationToken);
        return ToHttpResult(result, consultation => Results.Created($"/api/v1/emergency/consultations/{consultation.Id}", MapToConsultationResponse(consultation)));
    }

    private static async Task<IResult> AcceptConsultationAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyConsultationAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);
        var result = await encounterService.AcceptConsultationAsync(id, doctorId, cancellationToken);
        return ToHttpResult(result, consultation => Results.Ok(MapToConsultationResponse(consultation)));
    }

    private static async Task<IResult> RespondConsultationAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RespondEmergencyConsultationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyConsultationAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);
        var result = await encounterService.RespondConsultationAsync(id, request.ResponseNotes, doctorId, cancellationToken);
        return ToHttpResult(result, consultation => Results.Ok(MapToConsultationResponse(consultation)));
    }

    private static async Task<IResult> CancelConsultationAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CancelEmergencyConsultationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyConsultationAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);
        var result = await encounterService.CancelConsultationAsync(id, request.Reason, doctorId, cancellationToken);
        return ToHttpResult(result, consultation => Results.Ok(MapToConsultationResponse(consultation)));
    }

    private static async Task<IResult> GetConsultationsByAdmissionIdAsync(
        IEmergencyEncounterService encounterService,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid admissionId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, admissionId, cancellationToken))
        {
            return Results.Forbid();
        }

        var consultations = await encounterService.GetConsultationsByAdmissionIdAsync(admissionId, cancellationToken);
        return Results.Ok(consultations.Select(MapToConsultationResponse).ToList());
    }

    private static async Task<IResult> GetAdmissionByIdAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyAdmissionAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var admission = await service.GetAdmissionByIdAsync(id, cancellationToken);
        return admission is null ? Results.NotFound() : Results.Ok(MapToResponse(admission));
    }

    private static async Task<IResult> GetActiveAdmissionByPatientIdAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessEmergencyPatientAsync(user, patientId, cancellationToken))
        {
            return Results.Forbid();
        }

        var admission = await service.GetActiveAdmissionByPatientIdAsync(patientId, cancellationToken);
        return admission is null ? Results.NotFound() : Results.Ok(MapToResponse(admission));
    }

    private static async Task<IResult> GetAdmissionsAsync(
        IEmergencyAdmissionService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromQuery] string? status,
        [FromQuery] string? triageLevel,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? assignedDoctorId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseEmergencyWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        EmergencyAdmissionStatus? parsedStatus = Enum.TryParse<EmergencyAdmissionStatus>(status, true, out var st) ? st : null;
        TriageLevel? parsedTriage = Enum.TryParse<TriageLevel>(triageLevel, true, out var tr) ? tr : null;

        var list = await service.GetAdmissionsAsync(
            parsedStatus,
            parsedTriage,
            patientId,
            assignedDoctorId,
            fromDate,
            toDate,
            cancellationToken);

        return Results.Ok(list.Select(MapToSummaryResponse).ToList());
    }

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

    private static IResult InvalidEnumValue(string fieldName, string? value, IReadOnlyList<string> allowedValues) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [fieldName] = [$"'{value}' geçerli değildir. İzin verilen değerler: {string.Join(", ", allowedValues)}."],
        });

    private static EmergencyAdmissionResponse MapToResponse(EmergencyAdmissionDto d) =>
        new(
            d.Id,
            d.EmergencyProtocolNumber,
            d.PatientId,
            d.ArrivalType.ToString(),
            d.ChiefComplaint,
            d.AdmissionNotes,
            d.Status.ToString(),
            d.AdmittedAtUtc,
            d.AdmittingStaffId,
            d.Triage is null ? null : new EmergencyTriageResponse(
                d.Triage.TriageLevel.ToString(),
                d.Triage.TriageCategoryReason,
                d.Triage.TriagedAtUtc,
                d.Triage.TriageNurseId,
                d.Triage.EducationalClassificationAssisted,
                d.Triage.SystolicBp,
                d.Triage.DiastolicBp,
                d.Triage.HeartRate,
                d.Triage.BodyTemperatureCelsius,
                d.Triage.RespiratoryRate,
                d.Triage.OxygenSaturationPercent,
                d.Triage.PainScale,
                d.Triage.Consciousness,
                d.Triage.ClinicalNotes),
            d.AssignedDoctorId,
            d.AssignedBedOrZone,
            d.CompletedAtUtc,
            d.DischargeOrDispositionNotes,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Version);

    private static EmergencyOrderResponse MapToOrderResponse(EmergencyOrderDto o) =>
        new(
            o.Id,
            o.AdmissionId,
            o.OrderType.ToString(),
            o.OrderCatalogCode,
            o.OrderCatalogName,
            o.Priority.ToString(),
            o.OrderedByDoctorId,
            o.OrderedAtUtc,
            o.Status.ToString(),
            o.ClinicalInstructions,
            o.ResultSummary,
            o.CompletedAtUtc,
            o.CancellationReason,
            o.Version);

    private static EmergencyConsultationResponse MapToConsultationResponse(EmergencyConsultationDto c) =>
        new(
            c.Id,
            c.AdmissionId,
            c.DepartmentId,
            c.DepartmentName,
            c.RequestedByDoctorId,
            c.RequestedAtUtc,
            c.Urgency.ToString(),
            c.ClinicalReason,
            c.Status.ToString(),
            c.ConsultantDoctorId,
            c.ConsultationResponseNotes,
            c.RespondedAtUtc,
            c.CancellationReason,
            c.Version);

    private static EmergencyAdmissionSummaryResponse MapToSummaryResponse(EmergencyAdmissionSummaryDto s) =>
        new(
            s.Id,
            s.EmergencyProtocolNumber,
            s.PatientId,
            s.ArrivalType.ToString(),
            s.ChiefComplaint,
            s.Status.ToString(),
            s.TriageLevel?.ToString(),
            s.AdmittedAtUtc,
            s.AssignedDoctorId,
            s.AssignedBedOrZone);

    private static IResult ToHttpResult<T>(EmergencyOperationResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            EmergencyOperationStatus.Success => onSuccess(result.Value!),
            EmergencyOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt Bulunamadı",
                detail: result.ErrorMessage),
            EmergencyOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma / Geçersiz Durum",
                detail: result.ErrorMessage),
            EmergencyOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.ValidationErrors?.Count > 0
                    ? result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value)
                    : new Dictionary<string, string[]> { ["General"] = [result.ErrorMessage ?? "Doğrulama hatası."] }),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
