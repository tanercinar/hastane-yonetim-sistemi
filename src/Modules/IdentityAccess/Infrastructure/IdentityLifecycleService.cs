using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Domain;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure;

public sealed class IdentityLifecycleService(
    UserManager<ApplicationUser> userManager,
    HospitalSignInManager signInManager,
    IdentityAccessDbContext dbContext,
    IIdentityMessageSender messageSender,
    IOptions<IdentityAccessOptions> options,
    TimeProvider timeProvider,
    ILogger<IdentityLifecycleService> logger,
    IAuditEventPublisher? auditPublisher = null) : IIdentityLifecycleService
{
    private static readonly ApplicationUser TimingUser = ApplicationUser.CreatePatient(
        Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff1"),
        Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffff2"),
        "DEMO-timing@hospital.invalid",
        DateTime.UnixEpoch);

    private static readonly string TimingPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(
            TimingUser,
            "DEMO-Timing-Password-Only!1");

    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly HospitalSignInManager _signInManager = signInManager;
    private readonly IdentityAccessDbContext _dbContext = dbContext;
    private readonly IIdentityMessageSender _messageSender = messageSender;
    private readonly IdentityAccessOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<IdentityLifecycleService> _logger = logger;
    private readonly IAuditEventPublisher? _auditPublisher = auditPublisher;

    private async Task PublishAuditAsync(
        string action,
        string targetResourceType,
        string targetResourceId,
        AuditOutcome outcome,
        Guid? actorUserId = null,
        Guid? actorPersonId = null,
        string? actorRole = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (_auditPublisher is null)
        {
            return;
        }

        try
        {
            await _auditPublisher.PublishAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    _timeProvider.GetUtcNow().UtcDateTime,
                    actorUserId,
                    actorPersonId,
                    actorRole,
                    ActorIpAddress: null,
                    ActorUserAgent: null,
                    action,
                    targetResourceType,
                    targetResourceId,
                    outcome,
                    reason,
                    Guid.NewGuid().ToString("N")),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log as debug without breaking flow
            _ = ex;
        }
    }

    public async Task<IdentityLifecycleResult> RegisterPatientAsync(
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        if (email is null)
        {
            return EmailValidationFailure();
        }

        var passwordValidation = await ValidatePasswordAsync(
            ApplicationUser.CreatePatient(Guid.NewGuid(), Guid.NewGuid(), email, GetUtcNow()),
            command.Password);
        if (passwordValidation is not null)
        {
            return passwordValidation;
        }

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            if (existingUser.AccountKind == AccountKind.Patient && !existingUser.EmailConfirmed)
            {
                await ReissueCodeAndNotifyAsync(
                    existingUser,
                    IdentityActionPurpose.ConfirmPatientEmail,
                    IdentityMessageKind.PatientEmailConfirmation,
                    issuedByUserId: null,
                    cancellationToken);
            }

            return IdentityLifecycleResult.Accepted();
        }

        var user = ApplicationUser.CreatePatient(
            Guid.NewGuid(),
            Guid.NewGuid(),
            email,
            GetUtcNow());
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var createResult = await _userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            if (createResult.Errors.All(error => error.Code.StartsWith("Duplicate", StringComparison.Ordinal)))
            {
                return IdentityLifecycleResult.Accepted();
            }

            return MapIdentityErrors(createResult.Errors);
        }

        var issuedCode = await IssueCodeAsync(
            user.Id,
            IdentityActionPurpose.ConfirmPatientEmail,
            issuedByUserId: null,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await SendSafelyAsync(
            new IdentityMessage(
                IdentityMessageKind.PatientEmailConfirmation,
                email,
                issuedCode.RawCode,
                issuedCode.ExpiresAtUtc),
            cancellationToken);

        return IdentityLifecycleResult.Accepted();
    }

    public async Task<IdentityLifecycleResult> ConfirmPatientEmailAsync(
        ConfirmPatientEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        var user = email is null ? null : await _userManager.FindByEmailAsync(email);
        if (user is null || user.AccountKind != AccountKind.Patient)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }
        if (user.EmailConfirmed)
        {
            return IdentityLifecycleResult.Success();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var actionCode = await FindUsableCodeAsync(
            user.Id,
            IdentityActionPurpose.ConfirmPatientEmail,
            command.Code,
            cancellationToken);
        if (actionCode is null)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }

        actionCode.Consume(GetUtcNow());
        user.ConfirmPatientEmail();
        try
        {
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return MapIdentityErrors(updateResult.Errors);
            }

            if (!await _userManager.IsInRoleAsync(user, "PAT"))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "PAT");
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return MapIdentityErrors(roleResult.Errors);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return IdentityLifecycleResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityLifecycleResult.InvalidActionCode();
        }
    }

    public async Task<IdentityLifecycleResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        var user = email is null ? null : await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _ = new PasswordHasher<ApplicationUser>().VerifyHashedPassword(
                TimingUser,
                TimingPasswordHash,
                command.Password);
            return IdentityLifecycleResult.InvalidCredentials();
        }

        var passwordMatches = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordMatches)
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                return IdentityLifecycleResult.InvalidCredentials();
            }

            _ = await _userManager.AccessFailedAsync(user);
            return IdentityLifecycleResult.InvalidCredentials();
        }

        if (!user.IsEnabled
            || !user.EmailConfirmed
            || await _userManager.IsLockedOutAsync(user))
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.Id.ToString()));
            if (user.Email is not null)
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            }

            await _signInManager.Context.SignInAsync(
                IdentityConstants.TwoFactorUserIdScheme,
                new ClaimsPrincipal(identity));
            return IdentityLifecycleResult.RequiresTwoFactor();
        }

        _ = await _userManager.ResetAccessFailedCountAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: "Password");
        await PublishAuditAsync(
            AuditAction.UserLogin,
            "User",
            user.Id.ToString(),
            AuditOutcome.Success,
            actorUserId: user.Id,
            actorPersonId: user.PersonId,
            cancellationToken: cancellationToken);
        return IdentityLifecycleResult.Success();
    }

    public async Task<IdentityLifecycleResult> TwoFactorLoginAsync(
        TwoFactorLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var authResult = await _signInManager.Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        if (authResult?.Principal is null)
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        var userIdString = authResult.Principal.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsEnabled || !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user))
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        bool isValid = false;
        if (command.IsRecoveryCode)
        {
            var rawCode = command.Code.Trim();
            var recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, rawCode);
            if (!recoveryResult.Succeeded)
            {
                var cleanCode = rawCode.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
                if (!string.Equals(cleanCode, rawCode, StringComparison.Ordinal))
                {
                    recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, cleanCode);
                }
            }

            isValid = recoveryResult.Succeeded;
        }
        else
        {
            var cleanTotp = command.Code
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal);

            isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                cleanTotp);
        }

        if (isValid)
        {
            _ = await _userManager.ResetAccessFailedCountAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: "MFA");
            await _signInManager.Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            await PublishAuditAsync(
                AuditAction.TwoFactorVerify,
                "User",
                user.Id.ToString(),
                AuditOutcome.Success,
                actorUserId: user.Id,
                actorPersonId: user.PersonId,
                cancellationToken: cancellationToken);
            return IdentityLifecycleResult.Success();
        }

        _ = await _userManager.AccessFailedAsync(user);
        return IdentityLifecycleResult.InvalidCredentials();
    }

    public async Task<MfaSetupDetails?> GetMfaSetupDetailsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsEnabled)
        {
            return null;
        }

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrEmpty(unformattedKey))
        {
            return null;
        }

        var email = user.Email ?? string.Empty;
        var authenticatorUri = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "otpauth://totp/HospitalManagement:{0}?secret={1}&issuer=HospitalManagement&digits=6",
            Uri.EscapeDataString(email),
            unformattedKey);

        return new MfaSetupDetails(unformattedKey, authenticatorUri);
    }

    public async Task<(IdentityLifecycleResult Result, IReadOnlyList<string>? RecoveryCodes)> EnableMfaAsync(
        ClaimsPrincipal principal,
        EnableMfaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsEnabled)
        {
            return (IdentityLifecycleResult.InvalidCredentials(), null);
        }

        var cleanCode = command.VerificationCode
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            cleanCode);

        if (!is2faTokenValid)
        {
            return (IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
            {
                ["verificationCode"] = ["Doğrulama kodu geçersiz."]
            }), null);
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))?.ToArray()
            ?? Array.Empty<string>();

        await PublishAuditAsync(
            AuditAction.MfaEnable,
            "UserMfa",
            user.Id.ToString(),
            AuditOutcome.Success,
            actorUserId: user.Id,
            actorPersonId: user.PersonId,
            cancellationToken: cancellationToken);

        return (IdentityLifecycleResult.Success(), recoveryCodes);
    }

    public async Task<IdentityLifecycleResult> DisableMfaAsync(
        ClaimsPrincipal principal,
        DisableMfaCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsEnabled)
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        var passwordMatches = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordMatches)
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        await PublishAuditAsync(
            AuditAction.MfaDisable,
            "UserMfa",
            user.Id.ToString(),
            AuditOutcome.Success,
            actorUserId: user.Id,
            actorPersonId: user.PersonId,
            cancellationToken: cancellationToken);

        return IdentityLifecycleResult.Success();
    }

    public async Task<(IdentityLifecycleResult Result, IReadOnlyList<string>? RecoveryCodes)> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsEnabled || !await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return (IdentityLifecycleResult.InvalidCredentials(), null);
        }

        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8))?.ToArray()
            ?? Array.Empty<string>();

        return (IdentityLifecycleResult.Success(), recoveryCodes);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _signInManager.SignOutAsync();
        await PublishAuditAsync(
            AuditAction.UserLogout,
            "Session",
            "Current",
            AuditOutcome.Success,
            cancellationToken: cancellationToken);
    }

    public async Task<CurrentAccount?> GetCurrentAccountAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.GetUserAsync(principal);
        if (user is null || !user.IsEnabled)
        {
            return null;
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = RolePermissionDefaults.GetPermissionsForRoles(roles).ToList();

        return new CurrentAccount(
            user.Id,
            user.Email ?? string.Empty,
            user.AccountKind.ToString(),
            user.PersonId,
            user.TwoFactorEnabled,
            roles,
            permissions);
    }

    public async Task<UserListResult> GetUsersAsync(
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var usersQuery = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var cleanQuery = query.Query.Trim();
            usersQuery = usersQuery.Where(u => u.Email != null && u.Email.Contains(cleanQuery));
        }

        if (query.IsEnabled.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsEnabled == query.IsEnabled.Value);
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var pagedUsers = await usersQuery
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = pagedUsers.Select(u => u.Id).ToList();

        var userRoles = await (from ur in _dbContext.UserRoles
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId)
                               select new
                               {
                                   ur.UserId,
                                   RoleName = r.Name
                               })
                              .ToListAsync(cancellationToken);

        var rolesGrouped = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.RoleName!).ToList());

        var items = pagedUsers.Select(u => new UserSummaryDto(
            u.Id,
            u.PersonId,
            u.Email ?? string.Empty,
            u.AccountKind.ToString(),
            u.IsEnabled,
            u.EmailConfirmed,
            rolesGrouped.TryGetValue(u.Id, out var roles) ? roles : Array.Empty<string>(),
            u.CreatedAtUtc)).ToList();

        return new UserListResult(items, totalCount, page, pageSize);
    }

    public async Task<IdentityLifecycleResult> UpdateUserStatusAsync(
        ClaimsPrincipal adminPrincipal,
        UpdateUserStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adminPrincipal);
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var targetUser = await _userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (targetUser is null)
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        if (!command.IsEnabled)
        {
            if (await _userManager.IsInRoleAsync(targetUser, HospitalRoles.SystemAdministrator))
            {
                var activeAdmins = await _userManager.GetUsersInRoleAsync(HospitalRoles.SystemAdministrator);
                var remainingActiveAdmins = activeAdmins.Count(u => u.IsEnabled && u.Id != command.TargetUserId);
                if (remainingActiveAdmins == 0)
                {
                    return IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
                    {
                        ["user"] = ["Son sistem yöneticisi hesabı devre dışı bırakılamaz."],
                    });
                }
            }
        }

        targetUser.SetEnabled(command.IsEnabled);
        var updateResult = await _userManager.UpdateAsync(targetUser);
        if (!updateResult.Succeeded)
        {
            return MapIdentityErrors(updateResult.Errors);
        }

        var currentAdmin = await _userManager.GetUserAsync(adminPrincipal);
        await PublishAuditAsync(
            command.IsEnabled ? "Identity.UserEnable" : AuditAction.UserDisable,
            "User",
            targetUser.Id.ToString(),
            AuditOutcome.Success,
            actorUserId: currentAdmin?.Id,
            actorPersonId: currentAdmin?.PersonId,
            reason: command.IsEnabled ? "Hesap etkinleştirildi." : "Hesap devre dışı bırakıldı.",
            cancellationToken: cancellationToken);

        return IdentityLifecycleResult.Success();
    }

    public async Task<IdentityLifecycleResult> UpdateUserRolesAsync(
        ClaimsPrincipal adminPrincipal,
        UpdateUserRolesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(adminPrincipal);
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var targetUser = await _userManager.FindByIdAsync(command.TargetUserId.ToString());
        if (targetUser is null)
        {
            return IdentityLifecycleResult.InvalidCredentials();
        }

        foreach (var role in command.Roles)
        {
            if (!HospitalRoles.IsKnown(role))
            {
                return IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
                {
                    ["roles"] = [$"Bilinmeyen rol: {role}"],
                });
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(targetUser);

        if (currentRoles.Contains(HospitalRoles.SystemAdministrator) && !command.Roles.Contains(HospitalRoles.SystemAdministrator))
        {
            var activeAdmins = await _userManager.GetUsersInRoleAsync(HospitalRoles.SystemAdministrator);
            var remainingActiveAdmins = activeAdmins.Count(u => u.IsEnabled && u.Id != command.TargetUserId);
            if (remainingActiveAdmins == 0)
            {
                return IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
                {
                    ["roles"] = ["Son sistem yöneticisi hesabından yönetici yetkisi kaldırılamaz."],
                });
            }
        }

        var rolesToRemove = currentRoles.Except(command.Roles, StringComparer.Ordinal).ToList();
        var rolesToAdd = command.Roles.Except(currentRoles, StringComparer.Ordinal).ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(targetUser, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return MapIdentityErrors(removeResult.Errors);
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(targetUser, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return MapIdentityErrors(addResult.Errors);
            }
        }

        var currentAdmin = await _userManager.GetUserAsync(adminPrincipal);
        await PublishAuditAsync(
            AuditAction.RoleAssign,
            "User",
            targetUser.Id.ToString(),
            AuditOutcome.Success,
            actorUserId: currentAdmin?.Id,
            actorPersonId: currentAdmin?.PersonId,
            reason: $"Roller güncellendi: [{string.Join(", ", command.Roles)}]",
            cancellationToken: cancellationToken);

        return IdentityLifecycleResult.Success();
    }

    public async Task<IdentityLifecycleResult> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        var user = email is null ? null : await _userManager.FindByEmailAsync(email);
        if (user is { IsEnabled: true, EmailConfirmed: true })
        {
            await ReissueCodeAndNotifyAsync(
                user,
                IdentityActionPurpose.ResetPassword,
                IdentityMessageKind.PasswordReset,
                issuedByUserId: null,
                cancellationToken);
        }

        return IdentityLifecycleResult.Accepted();
    }

    public async Task<IdentityLifecycleResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        var validationUser = email is null
            ? TimingUser
            : ApplicationUser.CreatePatient(Guid.NewGuid(), Guid.NewGuid(), email, GetUtcNow());
        var passwordValidation = await ValidatePasswordAsync(validationUser, command.NewPassword);
        if (passwordValidation is not null)
        {
            return passwordValidation;
        }

        var user = email is null ? null : await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsEnabled || !user.EmailConfirmed)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var actionCode = await FindUsableCodeAsync(
            user.Id,
            IdentityActionPurpose.ResetPassword,
            command.Code,
            cancellationToken);
        if (actionCode is null)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }

        actionCode.Consume(GetUtcNow());
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return MapIdentityErrors(removeResult.Errors);
        }

        var addResult = await _userManager.AddPasswordAsync(user, command.NewPassword);
        if (!addResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return MapIdentityErrors(addResult.Errors);
        }

        _ = await _userManager.SetLockoutEndDateAsync(user, null);
        _ = await _userManager.ResetAccessFailedCountAsync(user);
        _ = await _userManager.UpdateSecurityStampAsync(user);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return IdentityLifecycleResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityLifecycleResult.InvalidActionCode();
        }
    }

    public async Task<IdentityLifecycleResult> InviteStaffAsync(
        InviteStaffCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.IssuedByUserId == Guid.Empty || command.PersonId == Guid.Empty)
        {
            return IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
            {
                ["invitation"] = ["Davet veren kullanıcı ve kişi kimlikleri zorunludur."],
            });
        }

        var email = NormalizeDemoEmail(command.Email);
        if (email is null)
        {
            return EmailValidationFailure();
        }
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return IdentityLifecycleResult.Accepted();
        }

        var user = ApplicationUser.CreateInvitedStaff(
            Guid.NewGuid(),
            command.PersonId,
            email,
            GetUtcNow());
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return createResult.Errors.All(error => error.Code.StartsWith("Duplicate", StringComparison.Ordinal))
                ? IdentityLifecycleResult.Accepted()
                : MapIdentityErrors(createResult.Errors);
        }

        var issuedCode = await IssueCodeAsync(
            user.Id,
            IdentityActionPurpose.AcceptStaffInvitation,
            command.IssuedByUserId,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await SendSafelyAsync(
            new IdentityMessage(
                IdentityMessageKind.StaffInvitation,
                email,
                issuedCode.RawCode,
                issuedCode.ExpiresAtUtc),
            cancellationToken);

        return IdentityLifecycleResult.Accepted();
    }

    public async Task<IdentityLifecycleResult> AcceptStaffInvitationAsync(
        AcceptStaffInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var email = NormalizeDemoEmail(command.Email);
        var validationUser = email is null
            ? TimingUser
            : ApplicationUser.CreateInvitedStaff(Guid.NewGuid(), Guid.NewGuid(), email, GetUtcNow());
        var passwordValidation = await ValidatePasswordAsync(validationUser, command.Password);
        if (passwordValidation is not null)
        {
            return passwordValidation;
        }

        var user = email is null ? null : await _userManager.FindByEmailAsync(email);
        if (user is null || user.AccountKind != AccountKind.Staff || user.IsEnabled)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var actionCode = await FindUsableCodeAsync(
            user.Id,
            IdentityActionPurpose.AcceptStaffInvitation,
            command.Code,
            cancellationToken);
        if (actionCode is null)
        {
            return IdentityLifecycleResult.InvalidActionCode();
        }

        actionCode.Consume(GetUtcNow());
        user.AcceptStaffInvitation();
        var passwordResult = await _userManager.AddPasswordAsync(user, command.Password);
        if (!passwordResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return MapIdentityErrors(passwordResult.Errors);
        }

        _ = await _userManager.UpdateSecurityStampAsync(user);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return IdentityLifecycleResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityLifecycleResult.InvalidActionCode();
        }
    }

    private async Task ReissueCodeAndNotifyAsync(
        ApplicationUser user,
        IdentityActionPurpose purpose,
        IdentityMessageKind messageKind,
        Guid? issuedByUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var issuedCode = await IssueCodeAsync(user.Id, purpose, issuedByUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await SendSafelyAsync(
            new IdentityMessage(
                messageKind,
                user.Email ?? string.Empty,
                issuedCode.RawCode,
                issuedCode.ExpiresAtUtc),
            cancellationToken);
    }

    private async Task<IssuedCode> IssueCodeAsync(
        Guid userId,
        IdentityActionPurpose purpose,
        Guid? issuedByUserId,
        CancellationToken cancellationToken)
    {
        var nowUtc = GetUtcNow();
        var existingCodes = await _dbContext.ActionCodes
            .Where(code =>
                code.UserId == userId
                && code.Purpose == purpose
                && code.ConsumedAtUtc == null
                && code.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var existingCode in existingCodes)
        {
            existingCode.Revoke(nowUtc);
        }

        var rawCode = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(24));
        var expiresAtUtc = nowUtc.AddMinutes(_options.ActionCodeMinutes);
        _dbContext.ActionCodes.Add(IdentityActionCode.Issue(
            Guid.NewGuid(),
            userId,
            purpose,
            HashCode(rawCode),
            nowUtc,
            expiresAtUtc,
            issuedByUserId));
        return new IssuedCode(rawCode, expiresAtUtc);
    }

    private async Task<IdentityActionCode?> FindUsableCodeAsync(
        Guid userId,
        IdentityActionPurpose purpose,
        string rawCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawCode) || rawCode.Length > 128)
        {
            return null;
        }

        var codeHash = HashCode(rawCode.Trim());
        var code = await _dbContext.ActionCodes.SingleOrDefaultAsync(
            action =>
                action.UserId == userId
                && action.Purpose == purpose
                && action.CodeHash == codeHash,
            cancellationToken);
        return code is not null && code.IsUsableAt(GetUtcNow()) ? code : null;
    }

    private async Task<IdentityLifecycleResult?> ValidatePasswordAsync(
        ApplicationUser user,
        string password)
    {
        var errors = new List<IdentityError>();
        foreach (var validator in _userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(_userManager, user, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count == 0 ? null : MapIdentityErrors(errors);
    }

    private async Task SendSafelyAsync(
        IdentityMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _messageSender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            IdentityAccessLog.IdentityMessageDeliveryFailed(_logger);
        }
    }

    private DateTime GetUtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static string HashCode(string rawCode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));

    private static string? NormalizeDemoEmail(string email)
    {
        try
        {
            var trimmed = email.Trim();
            var parsed = new MailAddress(trimmed);
            return string.Equals(parsed.Address, trimmed, StringComparison.Ordinal)
                && parsed.Host.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
                    ? trimmed
                    : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static IdentityLifecycleResult EmailValidationFailure() =>
        IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
        {
            ["email"] = ["DEMO hesapları ayrılmış .invalid alan adında geçerli bir e-posta kullanmalıdır."],
        });

    private static IdentityLifecycleResult MapIdentityErrors(IEnumerable<IdentityError> errors) =>
        IdentityLifecycleResult.Validation(new Dictionary<string, string[]>
        {
            ["password"] = errors
                .Select(error => error.Description)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
        });

    private sealed record IssuedCode(string RawCode, DateTime ExpiresAtUtc);
}
