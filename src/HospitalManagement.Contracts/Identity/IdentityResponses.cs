namespace HospitalManagement.Contracts.Identity;

public sealed record IdentityOperationResponse(string Message);

public sealed record LoginResponse(string Message, bool RequiresTwoFactor = false);

public sealed record AntiforgeryTokenResponse(string Token);

public sealed record CurrentAccountResponse(
    string Email,
    string AccountKind,
    string PersonId,
    bool TwoFactorEnabled = false,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Permissions = null);

public sealed record MfaSetupResponse(
    string SharedKey,
    string AuthenticatorUri);

public sealed record MfaRecoveryCodesResponse(
    IReadOnlyList<string> RecoveryCodes);

public sealed record UserSummaryResponse(
    Guid Id,
    Guid PersonId,
    string Email,
    string AccountKind,
    bool IsEnabled,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);

public sealed record UserListResponse(
    IReadOnlyList<UserSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
