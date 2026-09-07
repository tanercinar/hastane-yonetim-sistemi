namespace HospitalManagement.Modules.IdentityAccess.Application;

public enum IdentityLifecycleStatus
{
    Succeeded = 1,
    Accepted = 2,
    InvalidCredentials = 3,
    InvalidActionCode = 4,
    ValidationFailed = 5,
    RequiresTwoFactor = 6,
}

public sealed record IdentityLifecycleResult(
    IdentityLifecycleStatus Status,
    IReadOnlyDictionary<string, string[]> Errors)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    public bool Succeeded => Status is IdentityLifecycleStatus.Succeeded
        or IdentityLifecycleStatus.Accepted;

    public bool NeedsTwoFactor => Status is IdentityLifecycleStatus.RequiresTwoFactor;

    public static IdentityLifecycleResult Success() =>
        new(IdentityLifecycleStatus.Succeeded, NoErrors);

    public static IdentityLifecycleResult Accepted() =>
        new(IdentityLifecycleStatus.Accepted, NoErrors);

    public static IdentityLifecycleResult InvalidCredentials() =>
        new(IdentityLifecycleStatus.InvalidCredentials, NoErrors);

    public static IdentityLifecycleResult InvalidActionCode() =>
        new(IdentityLifecycleStatus.InvalidActionCode, NoErrors);

    public static IdentityLifecycleResult RequiresTwoFactor() =>
        new(IdentityLifecycleStatus.RequiresTwoFactor, NoErrors);

    public static IdentityLifecycleResult Validation(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(IdentityLifecycleStatus.ValidationFailed, errors);
}

public sealed record CurrentAccount(
    Guid UserId,
    string Email,
    string AccountKind,
    Guid PersonId,
    bool TwoFactorEnabled = false,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Permissions = null);

public sealed record MfaSetupDetails(
    string SharedKey,
    string AuthenticatorUri);

public sealed class RegisterPatientCommand(string email, string password)
{
    public string Email { get; } = email;

    public string Password { get; } = password;
}

public sealed class ConfirmPatientEmailCommand(string email, string code)
{
    public string Email { get; } = email;

    public string Code { get; } = code;
}

public sealed class LoginCommand(string email, string password)
{
    public string Email { get; } = email;

    public string Password { get; } = password;
}

public sealed class TwoFactorLoginCommand(string code, bool isRecoveryCode)
{
    public string Code { get; } = code;

    public bool IsRecoveryCode { get; } = isRecoveryCode;
}

public sealed class EnableMfaCommand(string verificationCode)
{
    public string VerificationCode { get; } = verificationCode;
}

public sealed class DisableMfaCommand(string password)
{
    public string Password { get; } = password;
}

public sealed class RequestPasswordResetCommand(string email)
{
    public string Email { get; } = email;
}

public sealed class ResetPasswordCommand(string email, string code, string newPassword)
{
    public string Email { get; } = email;

    public string Code { get; } = code;

    public string NewPassword { get; } = newPassword;
}

public sealed class InviteStaffCommand(Guid issuedByUserId, Guid personId, string email)
{
    public Guid IssuedByUserId { get; } = issuedByUserId;

    public Guid PersonId { get; } = personId;

    public string Email { get; } = email;
}

public sealed class AcceptStaffInvitationCommand(string email, string code, string password)
{
    public string Email { get; } = email;

    public string Code { get; } = code;

    public string Password { get; } = password;
}

public sealed record UserSummaryDto(
    Guid Id,
    Guid PersonId,
    string Email,
    string AccountKind,
    bool IsEnabled,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);

public sealed record UserListResult(
    IReadOnlyList<UserSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record UserSearchQuery(
    string? Query,
    string? Role,
    bool? IsEnabled,
    int Page,
    int PageSize);

public sealed class UpdateUserStatusCommand(Guid targetUserId, bool isEnabled)
{
    public Guid TargetUserId { get; } = targetUserId;

    public bool IsEnabled { get; } = isEnabled;
}

public sealed class UpdateUserRolesCommand(Guid targetUserId, IReadOnlyList<string> roles)
{
    public Guid TargetUserId { get; } = targetUserId;

    public IReadOnlyList<string> Roles { get; } = roles;
}
