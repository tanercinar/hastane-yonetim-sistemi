using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.IdentityAccess.Domain;

public sealed class IdentityActionCode : IHasConcurrencyVersion
{
    private IdentityActionCode()
    {
    }

    private IdentityActionCode(
        Guid id,
        Guid userId,
        IdentityActionPurpose purpose,
        string codeHash,
        Guid? issuedByUserId,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Action code identifier cannot be empty.", nameof(id));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }
        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }
        if (codeHash.Length != 64 || !codeHash.All(char.IsAsciiHexDigit))
        {
            throw new ArgumentException("Action code hash must be a SHA-256 hexadecimal value.", nameof(codeHash));
        }
        if (issuedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Issuer identifier cannot be empty.", nameof(issuedByUserId));
        }
        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation time must be UTC.", nameof(createdAtUtc));
        }
        if (expiresAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
        }

        Id = id;
        UserId = userId;
        Purpose = purpose;
        CodeHash = codeHash.ToUpperInvariant();
        IssuedByUserId = issuedByUserId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid UserId
    {
        get; private set;
    }

    public IdentityActionPurpose Purpose
    {
        get; private set;
    }

    public string CodeHash { get; private set; } = string.Empty;

    public Guid? IssuedByUserId
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime ExpiresAtUtc
    {
        get; private set;
    }

    public DateTime? ConsumedAtUtc
    {
        get; private set;
    }

    public DateTime? RevokedAtUtc
    {
        get; private set;
    }

    public long Version
    {
        get; set;
    }

    public bool IsUsableAt(DateTime nowUtc) =>
        nowUtc.Kind == DateTimeKind.Utc
        && ConsumedAtUtc is null
        && RevokedAtUtc is null
        && nowUtc < ExpiresAtUtc;

    public static IdentityActionCode Issue(
        Guid id,
        Guid userId,
        IdentityActionPurpose purpose,
        string codeHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        Guid? issuedByUserId = null) =>
        new(
            id,
            userId,
            purpose,
            codeHash,
            issuedByUserId,
            createdAtUtc,
            expiresAtUtc);

    public void Consume(DateTime consumedAtUtc)
    {
        if (!IsUsableAt(consumedAtUtc))
        {
            throw new InvalidOperationException("Action code is expired, revoked, or already consumed.");
        }

        ConsumedAtUtc = consumedAtUtc;
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        if (revokedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Revocation time must be UTC.", nameof(revokedAtUtc));
        }
        if (ConsumedAtUtc is not null || RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
    }
}
