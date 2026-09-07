using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Contracts.Identity;

public sealed class RegisterPatientRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string? Password
    {
        get; set;
    }
}

public sealed class ConfirmPatientEmailRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }

    [Required]
    [StringLength(128)]
    public string? Code
    {
        get; set;
    }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }

    [Required]
    [StringLength(128)]
    public string? Password
    {
        get; set;
    }
}

public sealed class RequestPasswordResetRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }
}

public sealed class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }

    [Required]
    [StringLength(128)]
    public string? Code
    {
        get; set;
    }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string? NewPassword
    {
        get; set;
    }
}

public sealed class AcceptStaffInvitationRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string? Email
    {
        get; set;
    }

    [Required]
    [StringLength(128)]
    public string? Code
    {
        get; set;
    }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string? Password
    {
        get; set;
    }
}

public sealed class TwoFactorLoginRequest
{
    [Required]
    [StringLength(32, MinimumLength = 6)]
    public string? Code
    {
        get; set;
    }

    public bool IsRecoveryCode
    {
        get; set;
    }
}

public sealed class EnableMfaRequest
{
    [Required]
    [StringLength(16, MinimumLength = 6)]
    public string? VerificationCode
    {
        get; set;
    }
}

public sealed class DisableMfaRequest
{
    [Required]
    [StringLength(128)]
    public string? Password
    {
        get; set;
    }
}

public sealed class UpdateUserStatusRequest
{
    public bool IsEnabled
    {
        get; set;
    }
}

public sealed class UpdateUserRolesRequest
{
    [Required]
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public sealed class UserSearchRequest
{
    public string? Query
    {
        get; set;
    }

    public string? Role
    {
        get; set;
    }

    public bool? IsEnabled
    {
        get; set;
    }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}
