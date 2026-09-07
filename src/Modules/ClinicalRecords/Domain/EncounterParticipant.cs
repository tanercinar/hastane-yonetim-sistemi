namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class EncounterParticipant
{
    private EncounterParticipant()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid EncounterId
    {
        get; private set;
    }

    public Guid PractitionerId
    {
        get; private set;
    }

    public ParticipantRole Role
    {
        get; private set;
    }

    public DateTime JoinedAtUtc
    {
        get; private set;
    }

    public DateTime? LeftAtUtc
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static EncounterParticipant Create(
        Guid id,
        Guid encounterId,
        Guid practitionerId,
        ParticipantRole role,
        DateTime joinedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Katılımcı kimliği boş olamaz.", nameof(id));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (practitionerId == Guid.Empty)
        {
            throw new ArgumentException("Sağlık çalışanı kimliği boş olamaz.", nameof(practitionerId));
        }

        return new EncounterParticipant
        {
            Id = id,
            EncounterId = encounterId,
            PractitionerId = practitionerId,
            Role = role,
            JoinedAtUtc = joinedAtUtc,
            CreatedAtUtc = joinedAtUtc,
        };
    }

    public void MarkLeft(DateTime leftAtUtc)
    {
        if (leftAtUtc < JoinedAtUtc)
        {
            throw new InvalidOperationException("Ayrılma zamanı katılım zamanından önce olamaz.");
        }

        LeftAtUtc = leftAtUtc;
    }
}
