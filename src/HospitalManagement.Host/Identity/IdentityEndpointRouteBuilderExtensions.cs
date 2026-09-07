using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Contracts.Identity;
using HospitalManagement.Host.Authorization;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagement.Host.Identity;

public static class IdentityEndpointRouteBuilderExtensions
{
    private const string GenericAcceptedMessage =
        "Bilgiler uygunsa işlem kodu MOCK teslim kanalına gönderilecektir.";

    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/api/v1/identity")
            .WithGroupName("v1")
            .WithTags("Identity")
            .RequireRateLimiting(IdentityAccessConstants.RateLimitPolicyName);

        group.MapGet("/antiforgery", GetAntiforgeryToken)
            .WithName("GetIdentityAntiforgeryToken")
            .Produces<AntiforgeryTokenResponse>();

        group.MapPost("/patient-registrations", RegisterPatientAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RegisterPatient")
            .Produces<IdentityOperationResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();
        group.MapPost("/patient-email-confirmations", ConfirmPatientEmailAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ConfirmPatientEmail")
            .Produces<IdentityOperationResponse>()
            .ProducesValidationProblem();
        group.MapPost("/sessions", LoginAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("CreateIdentitySession")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/two-factor-sessions", TwoFactorLoginAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("TwoFactorLogin")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/sessions/logout", LogoutAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequireAuthorization()
            .WithName("DeleteIdentitySession")
            .Produces(StatusCodes.Status204NoContent);
        group.MapGet("/session", GetCurrentAccountAsync)
            .RequirePermission(HospitalPermissions.Identity.ProfileViewOwn)
            .WithName("GetCurrentIdentitySession")
            .Produces<CurrentAccountResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapPost("/password-reset-requests", RequestPasswordResetAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("RequestPasswordReset")
            .Produces<IdentityOperationResponse>(StatusCodes.Status202Accepted);
        group.MapPost("/password-resets", ResetPasswordAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("ResetPassword")
            .Produces<IdentityOperationResponse>()
            .ProducesValidationProblem();
        group.MapPost("/staff-invitation-acceptances", AcceptStaffInvitationAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .WithName("AcceptStaffInvitation")
            .Produces<IdentityOperationResponse>()
            .ProducesValidationProblem();

        // MFA Routes
        group.MapGet("/mfa/setup", GetMfaSetupDetailsAsync)
            .RequirePermission(HospitalPermissions.Identity.ProfileEditOwn)
            .WithName("GetMfaSetupDetails")
            .Produces<MfaSetupResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPost("/mfa/enable", EnableMfaAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.ProfileEditOwn)
            .WithName("EnableMfa")
            .Produces<MfaRecoveryCodesResponse>()
            .ProducesValidationProblem();
        group.MapPost("/mfa/disable", DisableMfaAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.ProfileEditOwn)
            .WithName("DisableMfa")
            .Produces<IdentityOperationResponse>()
            .ProducesValidationProblem();
        group.MapPost("/mfa/recovery-codes", RegenerateRecoveryCodesAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.ProfileEditOwn)
            .WithName("RegenerateRecoveryCodes")
            .Produces<MfaRecoveryCodesResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/seed", SeedAsync)
            .RequirePermission(HospitalPermissions.Identity.RoleAssign)
            .WithName("SeedIdentityData")
            .Produces<IdentityOperationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // Admin User Management Routes
        group.MapGet("/users", GetUsersAsync)
            .RequirePermission(HospitalPermissions.Identity.RoleAssign)
            .WithName("GetIdentityUsers")
            .Produces<UserListResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/users/{id:guid}/status", UpdateUserStatusAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.UserDisable)
            .WithName("UpdateIdentityUserStatus")
            .Produces<IdentityOperationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        group.MapPost("/users/{id:guid}/roles", UpdateUserRolesAsync)
            .AddEndpointFilter<IdentityAntiforgeryEndpointFilter>()
            .RequirePermission(HospitalPermissions.Identity.RoleAssign)
            .WithName("UpdateIdentityUserRoles")
            .Produces<IdentityOperationResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        return app;
    }

    private static AntiforgeryTokenResponse GetAntiforgeryToken(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return new AntiforgeryTokenResponse(tokens.RequestToken
            ?? throw new InvalidOperationException("Antiforgery request token was not generated."));
    }

    private static async Task<IResult> RegisterPatientAsync(
        RegisterPatientRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RegisterPatientAsync(
            new RegisterPatientCommand(request.Email!, request.Password!),
            cancellationToken);
        return MapResult(result, acceptedOnSuccess: true);
    }

    private static async Task<IResult> ConfirmPatientEmailAsync(
        ConfirmPatientEmailRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmPatientEmailAsync(
            new ConfirmPatientEmailCommand(request.Email!, request.Code!),
            cancellationToken);
        return MapResult(result);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(
            new LoginCommand(request.Email!, request.Password!),
            cancellationToken);

        if (result.NeedsTwoFactor)
        {
            return Results.Ok(new LoginResponse("İki faktörlü doğrulama kodu gereklidir.", RequiresTwoFactor: true));
        }

        if (result.Succeeded)
        {
            return Results.Ok(new LoginResponse("Giriş başarılı."));
        }

        return MapResult(result);
    }

    private static async Task<IResult> TwoFactorLoginAsync(
        TwoFactorLoginRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.TwoFactorLoginAsync(
            new TwoFactorLoginCommand(request.Code!, request.IsRecoveryCode),
            cancellationToken);

        if (result.Succeeded)
        {
            return Results.Ok(new LoginResponse("Giriş başarılı."));
        }

        return MapResult(result);
    }

    private static async Task<IResult> GetMfaSetupDetailsAsync(
        HttpContext context,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var details = await service.GetMfaSetupDetailsAsync(context.User, cancellationToken);
        return details is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Oturum geçersiz.")
            : Results.Ok(new MfaSetupResponse(details.SharedKey, details.AuthenticatorUri));
    }

    private static async Task<IResult> EnableMfaAsync(
        HttpContext context,
        EnableMfaRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var (result, recoveryCodes) = await service.EnableMfaAsync(
            context.User,
            new EnableMfaCommand(request.VerificationCode!),
            cancellationToken);

        if (result.Succeeded && recoveryCodes is not null)
        {
            return Results.Ok(new MfaRecoveryCodesResponse(recoveryCodes));
        }

        return MapResult(result);
    }

    private static async Task<IResult> DisableMfaAsync(
        HttpContext context,
        DisableMfaRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DisableMfaAsync(
            context.User,
            new DisableMfaCommand(request.Password!),
            cancellationToken);
        return MapResult(result);
    }

    private static async Task<IResult> RegenerateRecoveryCodesAsync(
        HttpContext context,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var (result, recoveryCodes) = await service.RegenerateRecoveryCodesAsync(
            context.User,
            cancellationToken);

        if (result.Succeeded && recoveryCodes is not null)
        {
            return Results.Ok(new MfaRecoveryCodesResponse(recoveryCodes));
        }

        return MapResult(result);
    }

    private static async Task<IResult> LogoutAsync(
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        await service.LogoutAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentAccountAsync(
        HttpContext context,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var account = await service.GetCurrentAccountAsync(
            context.User,
            cancellationToken);
        return account is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Oturum geçersiz.")
            : Results.Ok(new CurrentAccountResponse(
                account.Email,
                account.AccountKind,
                account.PersonId.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
                account.TwoFactorEnabled,
                account.Roles ?? Array.Empty<string>(),
                account.Permissions ?? Array.Empty<string>()));
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RequestPasswordResetAsync(
            new RequestPasswordResetCommand(request.Email!),
            cancellationToken);
        return MapResult(result, acceptedOnSuccess: true);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ResetPasswordAsync(
            new ResetPasswordCommand(request.Email!, request.Code!, request.NewPassword!),
            cancellationToken);

        if (result.Status is IdentityLifecycleStatus.Succeeded)
        {
            await service.LogoutAsync(cancellationToken);
        }

        return MapResult(result);
    }

    private static async Task<IResult> AcceptStaffInvitationAsync(
        AcceptStaffInvitationRequest request,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptStaffInvitationAsync(
            new AcceptStaffInvitationCommand(request.Email!, request.Code!, request.Password!),
            cancellationToken);
        return MapResult(result);
    }

    private static async Task<IResult> SeedAsync(
        IIdentityDataSeeder seeder,
        CancellationToken cancellationToken)
    {
        await seeder.SeedAsync(cancellationToken);
        return Results.Ok(new IdentityOperationResponse("DEMO verileri başarıyla seed edildi."));
    }

    private static async Task<IResult> GetUsersAsync(
        string? query,
        string? role,
        bool? isEnabled,
        int page,
        int pageSize,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var effectivePageSize = pageSize <= 0 ? 50 : pageSize;

        var result = await service.GetUsersAsync(
            new UserSearchQuery(query, role, isEnabled, effectivePage, effectivePageSize),
            cancellationToken);

        var response = new UserListResponse(
            result.Items.Select(i => new UserSummaryResponse(
                i.Id,
                i.PersonId,
                i.Email,
                i.AccountKind,
                i.IsEnabled,
                i.EmailConfirmed,
                i.Roles,
                i.CreatedAtUtc)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateUserStatusAsync(
        Guid id,
        UpdateUserStatusRequest request,
        HttpContext context,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserStatusAsync(
            context.User,
            new UpdateUserStatusCommand(id, request.IsEnabled),
            cancellationToken);

        return MapResult(result);
    }

    private static async Task<IResult> UpdateUserRolesAsync(
        Guid id,
        UpdateUserRolesRequest request,
        HttpContext context,
        IIdentityLifecycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateUserRolesAsync(
            context.User,
            new UpdateUserRolesCommand(id, request.Roles),
            cancellationToken);

        return MapResult(result);
    }

    private static IResult MapResult(
        IdentityLifecycleResult result,
        bool acceptedOnSuccess = false) =>
        result.Status switch
        {
            IdentityLifecycleStatus.Accepted => Results.Accepted(
                value: new IdentityOperationResponse(GenericAcceptedMessage)),
            IdentityLifecycleStatus.Succeeded when acceptedOnSuccess => Results.Accepted(
                value: new IdentityOperationResponse(GenericAcceptedMessage)),
            IdentityLifecycleStatus.Succeeded => Results.Ok(
                new IdentityOperationResponse("İşlem tamamlandı.")),
            IdentityLifecycleStatus.InvalidCredentials => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "E-posta veya parola hatalı."),
            IdentityLifecycleStatus.InvalidActionCode => Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["code"] = ["Kod geçersiz, kullanılmış veya süresi dolmuş."],
                }),
            IdentityLifecycleStatus.ValidationFailed => Results.ValidationProblem(result.Errors),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
}

