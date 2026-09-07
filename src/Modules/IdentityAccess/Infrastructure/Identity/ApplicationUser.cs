using HospitalManagement.Modules.IdentityAccess.Domain;

using Microsoft.AspNetCore.Identity;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    private ApplicationUser()
    {
    }

    public Guid PersonId
    {
        get; private set;
    }

    public AccountKind AccountKind
    {
        get; private set;
    }

    public bool IsEnabled
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static ApplicationUser CreatePatient(
        Guid id,
        Guid personId,
        string email,
        DateTime createdAtUtc) =>
        Create(id, personId, email, AccountKind.Patient, isEnabled: true, createdAtUtc);

    public static ApplicationUser CreateInvitedStaff(
        Guid id,
        Guid personId,
        string email,
        DateTime createdAtUtc) =>
        Create(id, personId, email, AccountKind.Staff, isEnabled: false, createdAtUtc);

    public void ConfirmPatientEmail()
    {
        if (AccountKind != AccountKind.Patient)
        {
            throw new InvalidOperationException("Only a patient account can use patient email confirmation.");
        }

        EmailConfirmed = true;
    }

    public void AcceptStaffInvitation()
    {
        if (AccountKind != AccountKind.Staff)
        {
            throw new InvalidOperationException("Only a staff account can accept a staff invitation.");
        }

        EmailConfirmed = true;
        IsEnabled = true;
    }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    private static ApplicationUser Create(
        Guid id,
        Guid personId,
        string email,
        AccountKind accountKind,
        bool isEnabled,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(id));
        }
        if (personId == Guid.Empty)
        {
            throw new ArgumentException("Person identifier cannot be empty.", nameof(personId));
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email address is required.", nameof(email));
        }
        if (!Enum.IsDefined(accountKind))
        {
            throw new ArgumentOutOfRangeException(nameof(accountKind));
        }
        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation time must be UTC.", nameof(createdAtUtc));
        }

        var normalizedEmail = email.Trim();
        return new ApplicationUser
        {
            Id = id,
            PersonId = personId,
            UserName = normalizedEmail,
            Email = normalizedEmail,
            AccountKind = accountKind,
            IsEnabled = isEnabled,
            CreatedAtUtc = createdAtUtc,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture),
            ConcurrencyStamp = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture),
        };
    }
}
