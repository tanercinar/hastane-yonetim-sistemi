using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Surgery;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Surgery;

public static class SurgeryEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSurgeryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var surgeryGroup = endpoints.MapGroup("/api/v1/surgery")
            .RequireAuthorization();

        surgeryGroup.MapGet("/operating-rooms", GetOperatingRoomsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .WithName("GetOperatingRooms")
            .Produces<List<OperatingRoomResponse>>();

        var bookingsGroup = surgeryGroup.MapGroup("/bookings");

        bookingsGroup.MapPost(string.Empty, CreateBookingAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateSurgeryBooking")
            .Produces<SurgeryBookingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        bookingsGroup.MapPost("/{id:guid}/reschedule", RescheduleBookingAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RescheduleSurgeryBooking")
            .Produces<SurgeryBookingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        bookingsGroup.MapPost("/{id:guid}/pre-op-checklist", RecordPreOpChecklistAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RecordSurgeryPreOpChecklist")
            .Produces<SurgeryBookingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        bookingsGroup.MapPost("/{id:guid}/cancel", CancelBookingAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CancelSurgeryBooking")
            .Produces<SurgeryBookingResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        bookingsGroup.MapGet("/{id:guid}", GetBookingByIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .WithName("GetSurgeryBookingById")
            .Produces<SurgeryBookingResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        bookingsGroup.MapGet(string.Empty, GetBookingsAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .WithName("GetSurgeryBookings")
            .Produces<List<SurgeryBookingResponse>>();

        var periopGroup = surgeryGroup.MapGroup("/perioperative-records");

        periopGroup.MapPost(string.Empty, SavePerioperativeRecordAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SavePerioperativeRecord")
            .Produces<PerioperativeRecordResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        periopGroup.MapGet("/by-booking/{bookingId:guid}", GetPerioperativeRecordByBookingIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .WithName("GetPerioperativeRecordByBookingId")
            .Produces<PerioperativeRecordResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        periopGroup.MapGet("/{id:guid}", GetPerioperativeRecordByIdAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .WithName("GetPerioperativeRecordById")
            .Produces<PerioperativeRecordResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        periopGroup.MapPost("/{id:guid}/sign", SignPerioperativeRecordAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("SignPerioperativeRecord")
            .Produces<PerioperativeRecordResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        periopGroup.MapPost("/{id:guid}/corrections", AddPerioperativeCorrectionAsync)
            .RequirePermission(HospitalPermissions.Inpatient.SurgerySchedule)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AddPerioperativeCorrection")
            .Produces<PerioperativeCorrectionResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetOperatingRoomsAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanUseSurgeryWorklistAsync(user, cancellationToken))
        {
            return Results.Forbid();
        }

        var list = await service.GetOperatingRoomsAsync(cancellationToken);
        return Results.Ok(list.Select(r => new OperatingRoomResponse(
            r.Id,
            r.RoomCode,
            r.RoomName,
            r.IsActive,
            r.Capacity,
            r.SpecialtyDepartmentId)).ToList());
    }

    private static async Task<IResult> CreateBookingAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] CreateSurgeryBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanCreateSurgeryBookingAsync(
                user,
                request.PatientId,
                request.DepartmentId,
                request.LeadSurgeonDoctorId,
                request.AnesthesiologistDoctorId,
                request.OperatingNurseStaffId,
                cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<SurgeryUrgency>(request.Urgency, true, out var urgency))
        {
            return InvalidEnumValue(nameof(request.Urgency), request.Urgency, Enum.GetNames<SurgeryUrgency>());
        }

        var dto = new CreateSurgeryBookingDto(
            request.PatientId,
            request.EncounterId,
            request.DepartmentId,
            request.DepartmentName,
            request.ProcedureName,
            request.ProcedureCode,
            urgency,
            request.OperatingRoomId,
            request.LeadSurgeonDoctorId,
            request.AnesthesiologistDoctorId,
            request.OperatingNurseStaffId,
            request.ScheduledStartTimeUtc,
            request.ScheduledEndTimeUtc,
            request.ClinicalNotes);

        var result = await service.CreateBookingAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, booking => Results.Created($"/api/v1/surgery/bookings/{booking.Id}", MapToResponse(booking)));
    }

    private static async Task<IResult> RescheduleBookingAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RescheduleSurgeryBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        var dto = new RescheduleSurgeryBookingDto(
            request.OperatingRoomId,
            request.ScheduledStartTimeUtc,
            request.ScheduledEndTimeUtc);

        var result = await service.RescheduleBookingAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, booking => Results.Ok(MapToResponse(booking)));
    }

    private static async Task<IResult> RecordPreOpChecklistAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] RecordPreOpChecklistRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        var dto = new RecordPreOpChecklistDto(
            request.ConsentSigned,
            request.AnesthesiaClearance,
            request.NpoConfirmed,
            request.BloodProductsReserved,
            request.SiteMarked,
            request.AllergyChecked,
            request.Notes);

        var result = await service.RecordPreOpChecklistAsync(id, dto, staffId, cancellationToken);
        return ToHttpResult(result, booking => Results.Ok(MapToResponse(booking)));
    }

    private static async Task<IResult> CancelBookingAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] CancelSurgeryBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);
        var result = await service.CancelBookingAsync(id, request.Reason, staffId, cancellationToken);
        return ToHttpResult(result, booking => Results.Ok(MapToResponse(booking)));
    }

    private static async Task<IResult> GetBookingByIdAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var booking = await service.GetBookingByIdAsync(id, cancellationToken);
        return booking is null ? Results.NotFound() : Results.Ok(MapToResponse(booking));
    }

    private static async Task<IResult> GetBookingsAsync(
        ISurgeryPlanningService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromQuery] Guid? operatingRoomId,
        [FromQuery] Guid? leadSurgeonId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        SurgeryBookingStatus? parsedStatus = Enum.TryParse<SurgeryBookingStatus>(status, true, out var st) ? st : null;

        var list = await service.GetBookingsAsync(
            operatingRoomId,
            leadSurgeonId,
            fromDate,
            toDate,
            parsedStatus,
            cancellationToken);

        var accessible = new List<SurgeryBookingResponse>();
        foreach (var booking in list)
        {
            if (await accessControl.CanAccessSurgeryBookingAsync(user, booking, cancellationToken))
            {
                accessible.Add(MapToResponse(booking));
            }
        }

        return Results.Ok(accessible);
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

    private static async Task<IResult> SavePerioperativeRecordAsync(
        IPerioperativeRecordService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        [FromBody] SavePerioperativeRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, request.SurgeryBookingId, cancellationToken))
        {
            return Results.Forbid();
        }

        var staffId = ExtractActorId(user);

        if (!Enum.TryParse<AnesthesiaType>(request.AnesthesiaType, true, out var anesthesiaType))
        {
            return InvalidEnumValue(nameof(request.AnesthesiaType), request.AnesthesiaType, Enum.GetNames<AnesthesiaType>());
        }

        if (!Enum.TryParse<PostOpDisposition>(request.PostOpDisposition, true, out var postOpDisp))
        {
            return InvalidEnumValue(nameof(request.PostOpDisposition), request.PostOpDisposition, Enum.GetNames<PostOpDisposition>());
        }

        var dto = new SavePerioperativeRecordDto(
            request.SurgeryBookingId,
            request.RoomEntryTimeUtc,
            request.AnesthesiaStartTimeUtc,
            request.IncisionTimeUtc,
            request.ClosureTimeUtc,
            request.AnesthesiaEndTimeUtc,
            request.RoomExitTimeUtc,
            anesthesiaType,
            request.AnesthesiaNotes,
            request.IntraoperativeFindings,
            request.IntraoperativeComplications,
            request.EstimatedBloodLossMl,
            request.SpecimensCollected,
            request.CountsConfirmed,
            postOpDisp,
            request.PostOpInstructions);

        var result = await service.SaveRecordAsync(dto, staffId, cancellationToken);
        return ToHttpResult(result, rec => Results.Ok(MapToRecordResponse(rec)));
    }

    private static async Task<IResult> GetPerioperativeRecordByBookingIdAsync(
        IPerioperativeRecordService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessSurgeryBookingAsync(user, bookingId, cancellationToken))
        {
            return Results.Forbid();
        }

        var rec = await service.GetByBookingIdAsync(bookingId, cancellationToken);
        return rec is null ? Results.NotFound() : Results.Ok(MapToRecordResponse(rec));
    }

    private static async Task<IResult> GetPerioperativeRecordByIdAsync(
        IPerioperativeRecordService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPerioperativeRecordAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var rec = await service.GetByIdAsync(id, cancellationToken);
        return rec is null ? Results.NotFound() : Results.Ok(MapToRecordResponse(rec));
    }

    private static async Task<IResult> SignPerioperativeRecordAsync(
        IPerioperativeRecordService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPerioperativeRecordAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);
        var result = await service.SignRecordAsync(id, doctorId, cancellationToken);
        return ToHttpResult(result, rec => Results.Ok(MapToRecordResponse(rec)));
    }

    private static async Task<IResult> AddPerioperativeCorrectionAsync(
        IPerioperativeRecordService service,
        Phase8ClinicalAccessControl accessControl,
        ClaimsPrincipal user,
        Guid id,
        [FromBody] AddPerioperativeCorrectionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await accessControl.CanAccessPerioperativeRecordAsync(user, id, cancellationToken))
        {
            return Results.Forbid();
        }

        var doctorId = ExtractActorId(user);
        var dto = new AddPerioperativeCorrectionDto(request.ReasonForCorrection, request.CorrectionNote);
        var result = await service.AddCorrectionAsync(id, dto, doctorId, cancellationToken);
        return ToHttpResult(result, corr => Results.Ok(new PerioperativeCorrectionResponse(
            corr.Id,
            corr.PerioperativeRecordId,
            corr.CorrectedByDoctorId,
            corr.CorrectedAtUtc,
            corr.ReasonForCorrection,
            corr.CorrectionNote)));
    }

    private static PerioperativeRecordResponse MapToRecordResponse(PerioperativeRecordDto r) =>
        new(
            r.Id,
            r.SurgeryBookingId,
            r.PatientId,
            r.OperatingRoomId,
            r.RoomEntryTimeUtc,
            r.AnesthesiaStartTimeUtc,
            r.IncisionTimeUtc,
            r.ClosureTimeUtc,
            r.AnesthesiaEndTimeUtc,
            r.RoomExitTimeUtc,
            r.AnesthesiaType.ToString(),
            r.AnesthesiaNotes,
            r.IntraoperativeFindings,
            r.IntraoperativeComplications,
            r.EstimatedBloodLossMl,
            r.SpecimensCollected,
            r.CountsConfirmed,
            r.PostOpDisposition.ToString(),
            r.PostOpInstructions,
            r.IsSigned,
            r.SignedByDoctorId,
            r.SignedAtUtc,
            r.Corrections.Select(c => new PerioperativeCorrectionResponse(
                c.Id,
                c.PerioperativeRecordId,
                c.CorrectedByDoctorId,
                c.CorrectedAtUtc,
                c.ReasonForCorrection,
                c.CorrectionNote)).ToList(),
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.Version);

    private static SurgeryBookingResponse MapToResponse(SurgeryBookingDto b) =>
        new(
            b.Id,
            b.BookingProtocolNumber,
            b.PatientId,
            b.EncounterId,
            b.DepartmentId,
            b.DepartmentName,
            b.ProcedureName,
            b.ProcedureCode,
            b.Urgency.ToString(),
            b.OperatingRoomId,
            b.LeadSurgeonDoctorId,
            b.AnesthesiologistDoctorId,
            b.OperatingNurseStaffId,
            b.ScheduledStartTimeUtc,
            b.ScheduledEndTimeUtc,
            b.Status.ToString(),
            b.PreOpChecklist is null ? null : new PreOpChecklistResponse(
                b.PreOpChecklist.ConsentSigned,
                b.PreOpChecklist.AnesthesiaClearance,
                b.PreOpChecklist.NpoConfirmed,
                b.PreOpChecklist.BloodProductsReserved,
                b.PreOpChecklist.SiteMarked,
                b.PreOpChecklist.AllergyChecked,
                b.PreOpChecklist.IsFullyCleared,
                b.PreOpChecklist.CompletedByStaffId,
                b.PreOpChecklist.CompletedAtUtc,
                b.PreOpChecklist.Notes),
            b.ClinicalNotes,
            b.CancellationReason,
            b.CreatedAtUtc,
            b.UpdatedAtUtc,
            b.Version);

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
