using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Scheduling;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Host.Realtime;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.Notifications.Application;
using HospitalManagement.Modules.Scheduling.Application;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Scheduling;

public static class SchedulingEndpointRouteBuilderExtensions
{
    public static WebApplication MapSchedulingEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/scheduling")
            .WithTags("Scheduling");

        // Doctor Schedule & Slots
        group.MapPost("/schedules", CreateDoctorScheduleAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.ScheduleManage)
            .WithName("CreateDoctorSchedule")
            .Produces<DoctorScheduleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/schedules/by-doctor/{doctorId:guid}", GetDoctorSchedulesAsync)
            .RequirePermission(HospitalPermissions.Appointment.ScheduleManage)
            .WithName("GetDoctorSchedules")
            .Produces<IReadOnlyList<DoctorScheduleResponse>>();

        group.MapPost("/leave-blocks", CreateDoctorLeaveBlockAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.ScheduleManage)
            .WithName("CreateDoctorLeaveBlock")
            .Produces<DoctorLeaveBlockResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/slots/generate", GenerateDoctorSlotsAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.ScheduleManage)
            .WithName("GenerateDoctorSlots")
            .Produces<int>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/availability/by-doctor/{doctorId:guid}", GetDoctorAvailabilityAsync)
            .RequireAnyPermission(
                HospitalPermissions.Appointment.BookOwn,
                HospitalPermissions.Appointment.Manage,
                HospitalPermissions.Appointment.ScheduleManage)
            .WithName("GetDoctorAvailability")
            .Produces<IReadOnlyList<DoctorAvailabilityDayResponse>>();

        // Appointments Lifecycle
        group.MapPost("/appointments/book", BookAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.Appointment.BookOwn,
                HospitalPermissions.Appointment.Manage)
            .WithName("BookAppointment")
            .Produces<AppointmentDetailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/appointments/{appointmentId:guid}/cancel", CancelAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAnyPermission(
                HospitalPermissions.Appointment.ManageOwn,
                HospitalPermissions.Appointment.Manage)
            .WithName("CancelAppointment")
            .Produces<AppointmentDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/appointments/{appointmentId:guid}/check-in", CheckInAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.CheckIn)
            .WithName("CheckInAppointment")
            .Produces<AppointmentDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/appointments/{appointmentId:guid}/complete", CompleteAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.Manage)
            .WithName("CompleteAppointment")
            .Produces<AppointmentDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/appointments/{appointmentId:guid}/no-show", MarkNoShowAppointmentAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Appointment.Manage)
            .WithName("MarkNoShowAppointment")
            .Produces<AppointmentDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/appointments/{appointmentId:guid}", GetAppointmentByIdAsync)
            .RequireAnyPermission(
                HospitalPermissions.Appointment.ViewOwn,
                HospitalPermissions.Appointment.Manage)
            .WithName("GetAppointmentById")
            .Produces<AppointmentDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/appointments/by-patient/{patientId:guid}", GetPatientAppointmentsAsync)
            .RequireAnyPermission(
                HospitalPermissions.Appointment.ViewOwn,
                HospitalPermissions.Appointment.Manage)
            .WithName("GetPatientAppointments")
            .Produces<IReadOnlyList<AppointmentDetailResponse>>();

        group.MapGet("/appointments/daily", GetDailyAppointmentsAsync)
            .RequirePermission(HospitalPermissions.Appointment.CheckIn)
            .WithName("GetDailyAppointments")
            .Produces<IReadOnlyList<AppointmentDetailResponse>>();

        return app;
    }

    private static async Task<IResult> CreateDoctorScheduleAsync(
        [FromBody] CreateDoctorScheduleRequest request,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DayOfWeek>(request.DayOfWeek, true, out var dayOfWeek)
            || !Enum.IsDefined(dayOfWeek))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dayOfWeek"] = ["Geçerli bir haftanın günü belirtilmelidir."],
            });
        }

        var command = new CreateDoctorScheduleCommand(
            request.DoctorId,
            request.DepartmentId,
            dayOfWeek,
            request.StartTime,
            request.EndTime,
            request.SlotDurationMinutes,
            request.Breaks?.Select(b => new BreakItemDto(b.StartTime, b.EndTime, b.Reason)).ToList());

        var result = await service.CreateDoctorScheduleAsync(context.User, command, cancellationToken);
        return MapResult(result, dto => MapSchedule(dto), isCreate: true);
    }

    private static async Task<IResult> GetDoctorSchedulesAsync(
        Guid doctorId,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetDoctorSchedulesAsync(context.User, doctorId, cancellationToken);
        return MapResult(result, list => list.Select(MapSchedule).ToList());
    }

    private static async Task<IResult> CreateDoctorLeaveBlockAsync(
        [FromBody] CreateDoctorLeaveBlockRequest request,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var command = new CreateDoctorLeaveBlockCommand(
            request.DoctorId,
            request.StartUtc,
            request.EndUtc,
            request.Reason);

        var result = await service.CreateDoctorLeaveBlockAsync(context.User, command, cancellationToken);
        return MapResult(result, dto => new DoctorLeaveBlockResponse(
            dto.Id,
            dto.DoctorId,
            dto.StartUtc,
            dto.EndUtc,
            dto.Reason,
            dto.IsActive), isCreate: true);
    }

    private static async Task<IResult> GenerateDoctorSlotsAsync(
        [FromBody] GenerateSlotsRequest request,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var command = new GenerateDoctorSlotsCommand(
            request.DoctorId,
            request.StartDate,
            request.EndDate,
            request.TimeZoneId);

        var result = await service.GenerateDoctorSlotsAsync(context.User, command, cancellationToken);
        return MapResult(result, count => count);
    }

    private static async Task<IResult> GetDoctorAvailabilityAsync(
        Guid doctorId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] string? timeZoneId,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var end = endDate ?? start.AddDays(7);
        var tz = string.IsNullOrWhiteSpace(timeZoneId) ? "Europe/Istanbul" : timeZoneId;

        var query = new GetDoctorAvailabilityQuery(doctorId, start, end, tz);
        var result = await service.GetDoctorAvailabilityAsync(query, cancellationToken);

        return MapResult(result, days => days.Select(d => new DoctorAvailabilityDayResponse(
            d.Date,
            d.Slots.Select(s => new AppointmentSlotResponse(
                s.Id,
                s.DoctorId,
                s.DepartmentId,
                s.StartUtc,
                s.EndUtc,
                s.Status.ToString(),
                s.HeldByPersonId,
                s.Version)).ToList())).ToList());
    }

    private static async Task<IResult> BookAppointmentAsync(
        [FromBody] BookAppointmentRequest request,
        HttpContext context,
        ISchedulingService service,
        INotificationService notificationService,
        IIdentityContactLookup contactLookup,
        IHospitalRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        var command = new BookAppointmentCommand(
            request.SlotId,
            request.PatientId,
            request.ReasonForVisit);

        var result = await service.BookAppointmentAsync(context.User, command, cancellationToken);
        if (result.Status == SchedulingOperationStatus.Succeeded && result.Value is not null)
        {
            var appt = result.Value;
            var contact = await contactLookup.FindByPersonIdAsync(appt.PatientId, cancellationToken);
            var outboxCmd = new PublishOutboxEventCommand(
                EventType: "Appointment.Booked",
                IdempotencyKey: $"appt-booked-{appt.Id}",
                RecipientPersonId: appt.PatientId,
                RecipientEmail: contact?.Email,
                RecipientPhone: null,
                Subject: "Randevunuz Onaylandı",
                Message: $"Sayın hastamız, {appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm} tarihindeki muayene randevunuz onaylanmıştır. Randevu Referansı: {appt.Id}.",
                TemplateKey: "Appointment.Booked",
                TemplateTokens: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["appointmentDate"] = $"{appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm}",
                    ["reference"] = appt.Id.ToString(),
                });

            await notificationService.EnqueueOutboxEventAsync(outboxCmd, cancellationToken);
            await notificationService.ProcessOutboxAsync(cancellationToken);

            await notifier.NotifySlotChangedAsync(appt.SlotId, appt.DoctorId, "Booked", cancellationToken);
            await notifier.NotifyQueueUpdatedAsync(appt.Id, appt.DoctorId, appt.PatientId, appt.QueueNumber, "Confirmed", cancellationToken);
            await notifier.NotifyNotificationReceivedAsync(Guid.NewGuid(), appt.PatientId, "Randevunuz Onaylandı", $"Sayın hastamız, {appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm} tarihindeki muayene randevunuz onaylanmıştır.", cancellationToken);
        }

        return MapResult(result, MapAppointmentDetail, isCreate: true);
    }

    private static async Task<IResult> CancelAppointmentAsync(
        Guid appointmentId,
        [FromBody] CancelAppointmentRequest request,
        HttpContext context,
        ISchedulingService service,
        INotificationService notificationService,
        IIdentityContactLookup contactLookup,
        IHospitalRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        var command = new CancelAppointmentCommand(appointmentId, request.Reason);
        var result = await service.CancelAppointmentAsync(context.User, command, cancellationToken);
        if (result.Status == SchedulingOperationStatus.Succeeded && result.Value is not null)
        {
            var appt = result.Value;
            var contact = await contactLookup.FindByPersonIdAsync(appt.PatientId, cancellationToken);
            var outboxCmd = new PublishOutboxEventCommand(
                EventType: "Appointment.Cancelled",
                IdempotencyKey: $"appt-cancelled-{appt.Id}",
                RecipientPersonId: appt.PatientId,
                RecipientEmail: contact?.Email,
                RecipientPhone: null,
                Subject: "Randevunuz İptal Edildi",
                Message: $"Sayın hastamız, {appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm} tarihindeki muayene randevunuz iptal edilmiştir.",
                TemplateKey: "Appointment.Cancelled",
                TemplateTokens: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["appointmentDate"] = $"{appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm}",
                });

            await notificationService.EnqueueOutboxEventAsync(outboxCmd, cancellationToken);
            await notificationService.ProcessOutboxAsync(cancellationToken);

            await notifier.NotifySlotChangedAsync(appt.SlotId, appt.DoctorId, "Available", cancellationToken);
            await notifier.NotifyQueueUpdatedAsync(appt.Id, appt.DoctorId, appt.PatientId, appt.QueueNumber, "Cancelled", cancellationToken);
            await notifier.NotifyNotificationReceivedAsync(Guid.NewGuid(), appt.PatientId, "Randevunuz İptal Edildi", $"Sayın hastamız, {appt.AppointmentTimeUtc:dd.MM.yyyy HH:mm} tarihindeki muayene randevunuz iptal edilmiştir.", cancellationToken);
        }

        return MapResult(result, MapAppointmentDetail);
    }

    private static async Task<IResult> CheckInAppointmentAsync(
        Guid appointmentId,
        HttpContext context,
        ISchedulingService service,
        INotificationService notificationService,
        IIdentityContactLookup contactLookup,
        IHospitalRealtimeNotifier notifier,
        CancellationToken cancellationToken)
    {
        var result = await service.CheckInAppointmentAsync(context.User, appointmentId, cancellationToken);
        if (result.Status == SchedulingOperationStatus.Succeeded && result.Value is not null)
        {
            var appt = result.Value;
            var contact = await contactLookup.FindByPersonIdAsync(appt.PatientId, cancellationToken);
            var outboxCmd = new PublishOutboxEventCommand(
                EventType: "Appointment.CheckedIn",
                IdempotencyKey: $"appt-checkin-{appt.Id}-{appt.QueueNumber}",
                RecipientPersonId: appt.PatientId,
                RecipientEmail: contact?.Email,
                RecipientPhone: null,
                Subject: "Randevu Girişiniz Yapıldı",
                Message: $"Sayın hastamız, hastanemize girişiniz tamamlanmıştır. Muayene Sıra Numaranız: #{appt.QueueNumber}.",
                TemplateKey: "Appointment.CheckedIn",
                TemplateTokens: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["queueNumber"] = $"#{appt.QueueNumber}",
                });

            await notificationService.EnqueueOutboxEventAsync(outboxCmd, cancellationToken);
            await notificationService.ProcessOutboxAsync(cancellationToken);

            await notifier.NotifyQueueUpdatedAsync(appt.Id, appt.DoctorId, appt.PatientId, appt.QueueNumber, "CheckedIn", cancellationToken);
            await notifier.NotifyNotificationReceivedAsync(Guid.NewGuid(), appt.PatientId, "Randevu Girişiniz Yapıldı", $"Sıra Numaranız: #{appt.QueueNumber}", cancellationToken);
        }

        return MapResult(result, MapAppointmentDetail);
    }

    private static async Task<IResult> CompleteAppointmentAsync(
        Guid appointmentId,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAppointmentAsync(context.User, appointmentId, cancellationToken);
        return MapResult(result, MapAppointmentDetail);
    }

    private static async Task<IResult> MarkNoShowAppointmentAsync(
        Guid appointmentId,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.MarkNoShowAppointmentAsync(context.User, appointmentId, cancellationToken);
        return MapResult(result, MapAppointmentDetail);
    }

    private static async Task<IResult> GetAppointmentByIdAsync(
        Guid appointmentId,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAppointmentByIdAsync(context.User, appointmentId, cancellationToken);
        return MapResult(result, MapAppointmentDetail);
    }

    private static async Task<IResult> GetPatientAppointmentsAsync(
        Guid patientId,
        HttpContext context,
        ISchedulingService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPatientAppointmentsAsync(context.User, patientId, cancellationToken);
        return MapResult(result, list => list.Select(MapAppointmentDetail).ToList());
    }

    private static async Task<IResult> GetDailyAppointmentsAsync(
        HttpContext context,
        ISchedulingService service,
        [FromQuery] DateOnly? date = null,
        [FromQuery] Guid? doctorId = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await service.GetDailyAppointmentsAsync(
            context.User,
            targetDate,
            doctorId,
            departmentId,
            cancellationToken);

        return MapResult(result, list => list.Select(MapAppointmentDetail).ToList());
    }

    private static DoctorScheduleResponse MapSchedule(DoctorScheduleDto dto) =>
        new(
            dto.Id,
            dto.DoctorId,
            dto.DepartmentId,
            dto.DayOfWeek.ToString(),
            dto.StartTime,
            dto.EndTime,
            dto.SlotDurationMinutes,
            dto.IsActive,
            dto.Breaks.Select(b => new ScheduleBreakDto(b.StartTime, b.EndTime, b.Reason)).ToList());

    private static AppointmentDetailResponse MapAppointmentDetail(AppointmentDto dto) =>
        new(
            dto.Id,
            dto.SlotId,
            dto.PatientId,
            dto.DoctorId,
            dto.DepartmentId,
            dto.AppointmentTimeUtc,
            dto.Status.ToString(),
            dto.ReasonForVisit,
            dto.CancellationReason,
            dto.CancelledAtUtc,
            dto.CheckedInAtUtc,
            dto.CompletedAtUtc,
            dto.QueueNumber,
            dto.Version);

    private static IResult MapResult<TIn, TOut>(
        SchedulingOperationResult<TIn> result,
        Func<TIn, TOut> mapper,
        bool isCreate = false) =>
        result.Status switch
        {
            SchedulingOperationStatus.Succeeded when isCreate => Results.Created(string.Empty, mapper(result.Value!)),
            SchedulingOperationStatus.Succeeded => Results.Ok(mapper(result.Value!)),
            SchedulingOperationStatus.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kayıt bulunamadı.",
                detail: result.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? "İstenen takvim/randevu kaydı bulunamadı."),
            SchedulingOperationStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors?.ToDictionary(k => k.Key, v => v.Value) ?? new Dictionary<string, string[]>()),
            SchedulingOperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Çakışma.",
                detail: result.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? "Randevu durumu veya slot çakışması tespit edildi."),
            SchedulingOperationStatus.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Erişim engellendi.",
                detail: "Bu randevu işlemine erişim yetkiniz bulunmamaktadır."),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
}
