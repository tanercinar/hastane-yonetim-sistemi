using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Notifications;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Host.Identity;
using HospitalManagement.Modules.Notifications.Application;

using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Notifications;

public static class NotificationEndpointRouteBuilderExtensions
{
    public static WebApplication MapNotificationEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications");

        group.MapGet("/my", GetMyNotificationsAsync)
            .RequireAuthorization()
            .WithName("GetMyNotifications")
            .Produces<IReadOnlyList<NotificationDetailResponse>>();

        group.MapPost("/{notificationId:guid}/read", MarkAsReadAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .WithName("MarkNotificationAsRead")
            .Produces<bool>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/preferences", GetMyPreferenceAsync)
            .RequirePermission(HospitalPermissions.Identity.ProfileViewOwn)
            .WithName("GetMyNotificationPreference")
            .Produces<NotificationPreferenceResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/preferences", UpdateMyPreferenceAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.ProfileEditOwn)
            .WithName("UpdateMyNotificationPreference")
            .Produces<NotificationPreferenceResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> GetMyNotificationsAsync(
        HttpContext context,
        INotificationService service,
        CancellationToken cancellationToken)
    {
        var items = await service.GetMyNotificationsAsync(context.User, cancellationToken);
        var response = items.Select(n => new NotificationDetailResponse(
            n.Id,
            n.Title,
            n.Message,
            n.ActionUrl,
            n.IsRead,
            n.CreatedAtUtc,
            n.ReadAtUtc)).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> MarkAsReadAsync(
        Guid notificationId,
        HttpContext context,
        INotificationService service,
        CancellationToken cancellationToken)
    {
        var success = await service.MarkAsReadAsync(context.User, notificationId, cancellationToken);
        if (!success)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Bildirim bulunamadı.",
                detail: "İstenen bildirim bulunamadı veya erişim yetkiniz yok.");
        }

        return Results.Ok(true);
    }

    private static async Task<IResult> GetMyPreferenceAsync(
        HttpContext context,
        INotificationService service,
        CancellationToken cancellationToken)
    {
        var preference = await service.GetMyPreferenceAsync(context.User, cancellationToken);
        return preference is null
            ? Results.Forbid()
            : Results.Ok(MapPreference(preference));
    }

    private static async Task<IResult> UpdateMyPreferenceAsync(
        [FromBody] UpdateNotificationPreferenceRequest request,
        HttpContext context,
        INotificationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var preference = await service.UpdateMyPreferenceAsync(
                context.User,
                new UpdateNotificationPreferenceCommand(
                    request.EmailEnabled,
                    request.SmsEnabled,
                    request.Locale),
                cancellationToken);

            return preference is null
                ? Results.Forbid()
                : Results.Ok(MapPreference(preference));
        }
        catch (ArgumentException)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["locale"] = ["Desteklenen locale değerleri tr-TR ve en-US'tir."],
                },
                title: "Bildirim tercihi doğrulanamadı.");
        }
    }

    private static NotificationPreferenceResponse MapPreference(NotificationPreferenceDto preference) =>
        new(
            preference.EmailEnabled,
            preference.SmsEnabled,
            preference.Locale,
            preference.UpdatedAtUtc);
}
