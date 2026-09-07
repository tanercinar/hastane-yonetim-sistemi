namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class SpecimenTransitionEvent
{
    public Guid Id
    {
        get; private set;
    }
    public Guid SpecimenId
    {
        get; private set;
    }
    public SpecimenStatus FromStatus
    {
        get; private set;
    }
    public SpecimenStatus ToStatus
    {
        get; private set;
    }
    public DateTime TransitionedAtUtc
    {
        get; private set;
    }
    public Guid ActorUserId
    {
        get; private set;
    }
    public string ActorRole { get; private set; } = string.Empty;
    public string? Location
    {
        get; private set;
    }
    public string? Notes
    {
        get; private set;
    }

    private SpecimenTransitionEvent()
    {
    }

    public static SpecimenTransitionEvent Create(
        Guid id,
        Guid specimenId,
        SpecimenStatus fromStatus,
        SpecimenStatus toStatus,
        DateTime transitionedAtUtc,
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Geçiş kayıt kimliği zorunludur.", nameof(id));
        }

        if (specimenId == Guid.Empty)
        {
            throw new ArgumentException("Numune kimliği zorunludur.", nameof(specimenId));
        }

        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException("İşlemi yapan kullanıcı kimliği zorunludur.", nameof(actorUserId));
        }

        return new SpecimenTransitionEvent
        {
            Id = id,
            SpecimenId = specimenId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            TransitionedAtUtc = transitionedAtUtc,
            ActorUserId = actorUserId,
            ActorRole = string.IsNullOrWhiteSpace(actorRole) ? "Unknown" : actorRole.Trim(),
            Location = location?.Trim(),
            Notes = notes?.Trim(),
        };
    }
}
