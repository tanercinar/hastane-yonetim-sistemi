using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class Encounter : IHasConcurrencyVersion
{
    private readonly List<EncounterParticipant> _participants = [];

    private Encounter()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid? AppointmentId
    {
        get; private set;
    }

    public Guid PatientId
    {
        get; private set;
    }

    public Guid DepartmentId
    {
        get; private set;
    }

    public Guid PrimaryPractitionerId
    {
        get; private set;
    }

    public EncounterType EncounterType
    {
        get; private set;
    }

    public EncounterStatus Status
    {
        get; private set;
    }

    public DateTime? PlannedStartTimeUtc
    {
        get; private set;
    }

    public DateTime? ActualStartTimeUtc
    {
        get; private set;
    }

    public DateTime? ActualEndTimeUtc
    {
        get; private set;
    }

    public string? ChiefComplaint
    {
        get; private set;
    }

    public string? CancellationReason
    {
        get; private set;
    }

    public string? EnteredInErrorReason
    {
        get; private set;
    }

    public string? ReopenReason
    {
        get; private set;
    }

    public DateTime? ReopenedAtUtc
    {
        get; private set;
    }

    public Guid? ReopenedByPractitionerId
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public IReadOnlyCollection<EncounterParticipant> Participants => _participants.AsReadOnly();

    public static Encounter Create(
        Guid id,
        Guid? appointmentId,
        Guid patientId,
        Guid departmentId,
        Guid primaryPractitionerId,
        EncounterType encounterType,
        DateTime? plannedStartTimeUtc,
        string? chiefComplaint,
        DateTime nowUtc,
        bool startImmediately = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Bölüm kimliği boş olamaz.", nameof(departmentId));
        }

        if (primaryPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Asıl sorumlu hekim kimliği boş olamaz.", nameof(primaryPractitionerId));
        }

        var encounter = new Encounter
        {
            Id = id,
            AppointmentId = appointmentId,
            PatientId = patientId,
            DepartmentId = departmentId,
            PrimaryPractitionerId = primaryPractitionerId,
            EncounterType = encounterType,
            Status = startImmediately ? EncounterStatus.InProgress : EncounterStatus.Planned,
            PlannedStartTimeUtc = plannedStartTimeUtc ?? (startImmediately ? nowUtc : null),
            ActualStartTimeUtc = startImmediately ? nowUtc : null,
            ChiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? null : chiefComplaint.Trim(),
            Version = 1,
            CreatedAtUtc = nowUtc,
        };

        var primaryParticipant = EncounterParticipant.Create(
            Guid.NewGuid(),
            id,
            primaryPractitionerId,
            ParticipantRole.PrimaryAttending,
            nowUtc);

        encounter._participants.Add(primaryParticipant);

        return encounter;
    }

    public EncounterParticipant? Start(Guid practitionerId, DateTime nowUtc)
    {
        if (Status != EncounterStatus.Planned)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Planlandı' (Planned) durumundaki karşılaşmalar başlatılabilir. Mevcut durum: {Status}");
        }

        Status = EncounterStatus.InProgress;
        ActualStartTimeUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;

        if (!_participants.Any(p => p.PractitionerId == practitionerId && p.LeftAtUtc == null))
        {
            var p = EncounterParticipant.Create(
                Guid.NewGuid(),
                Id,
                practitionerId,
                ParticipantRole.PrimaryAttending,
                nowUtc);
            _participants.Add(p);
            return p;
        }

        return null;
    }

    public void Complete(Guid practitionerId, DateTime nowUtc)
    {
        if (Status != EncounterStatus.InProgress)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Devam Ediyor' (InProgress) durumundaki karşılaşmalar tamamlanabilir. Mevcut durum: {Status}");
        }

        Status = EncounterStatus.Completed;
        ActualEndTimeUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Reopen(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status != EncounterStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Yalnızca 'Tamamlandı' (Completed) durumundaki karşılaşmalar yeniden açılabilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Muayeneyi yeniden açmak için geçerli bir klinik gerekçe girilmelidir.", nameof(reason));
        }

        Status = EncounterStatus.InProgress;
        ReopenReason = reason.Trim();
        ReopenedAtUtc = nowUtc;
        ReopenedByPractitionerId = practitionerId;
        ActualEndTimeUtc = null; // Re-opened for editing
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Cancel(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status == EncounterStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış bir klinik karşılaşma doğrudan iptal edilemez.");
        }

        if (Status == EncounterStatus.Cancelled)
        {
            throw new InvalidOperationException("Karşılaşma zaten iptal edilmiştir.");
        }

        if (Status == EncounterStatus.EnteredInError)
        {
            throw new InvalidOperationException("Hatalı giriş olarak işaretlenmiş bir karşılaşma iptal edilemez.");
        }

        Status = EncounterStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Hizmet verilmedi" : reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status == EncounterStatus.EnteredInError)
        {
            throw new InvalidOperationException("Karşılaşma zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş işaretlemesi için geçerli bir gerekçe girilmelidir.", nameof(reason));
        }

        Status = EncounterStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public EncounterParticipant? AddParticipant(Guid practitionerId, ParticipantRole role, DateTime nowUtc)
    {
        if (Status is EncounterStatus.Completed or EncounterStatus.Cancelled or EncounterStatus.EnteredInError)
        {
            throw new InvalidOperationException(
                $"Sonlandırılmış bir karşılaşmaya yeni katılımcı eklenemez. Mevcut durum: {Status}");
        }

        var existingActive = _participants.FirstOrDefault(p =>
            p.PractitionerId == practitionerId && p.Role == role && p.LeftAtUtc == null);

        if (existingActive is not null)
        {
            return null;
        }

        var participant = EncounterParticipant.Create(
            Guid.NewGuid(),
            Id,
            practitionerId,
            role,
            nowUtc);

        _participants.Add(participant);
        UpdatedAtUtc = nowUtc;
        Version++;
        return participant;
    }

    public void RemoveParticipant(Guid practitionerId, DateTime nowUtc)
    {
        var activeParticipants = _participants
            .Where(p => p.PractitionerId == practitionerId && p.LeftAtUtc == null)
            .ToList();

        if (activeParticipants.Count == 0)
        {
            return;
        }

        foreach (var p in activeParticipants)
        {
            p.MarkLeft(nowUtc);
        }

        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
