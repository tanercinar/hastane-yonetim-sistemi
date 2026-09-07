using System.Security.Claims;

namespace HospitalManagement.Modules.IdentityAccess.Application;

public interface IIdentityLifecycleService
{
    Task<IdentityLifecycleResult> RegisterPatientAsync(
        RegisterPatientCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> ConfirmPatientEmailAsync(
        ConfirmPatientEmailCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<CurrentAccount?> GetCurrentAccountAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> RequestPasswordResetAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> InviteStaffAsync(
        InviteStaffCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> AcceptStaffInvitationAsync(
        AcceptStaffInvitationCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> TwoFactorLoginAsync(
        TwoFactorLoginCommand command,
        CancellationToken cancellationToken = default);

    Task<MfaSetupDetails?> GetMfaSetupDetailsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<(IdentityLifecycleResult Result, IReadOnlyList<string>? RecoveryCodes)> EnableMfaAsync(
        ClaimsPrincipal principal,
        EnableMfaCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> DisableMfaAsync(
        ClaimsPrincipal principal,
        DisableMfaCommand command,
        CancellationToken cancellationToken = default);

    Task<(IdentityLifecycleResult Result, IReadOnlyList<string>? RecoveryCodes)> RegenerateRecoveryCodesAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<UserListResult> GetUsersAsync(
        UserSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> UpdateUserStatusAsync(
        ClaimsPrincipal adminPrincipal,
        UpdateUserStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<IdentityLifecycleResult> UpdateUserRolesAsync(
        ClaimsPrincipal adminPrincipal,
        UpdateUserRolesCommand command,
        CancellationToken cancellationToken = default);
}
