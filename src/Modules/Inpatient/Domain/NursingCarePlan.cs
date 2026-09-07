namespace HospitalManagement.Modules.Inpatient.Domain;

public enum CarePlanStatus
{
    Active,
    Resolved,
    Discontinued,
}

public sealed class NursingCarePlan
{
    private readonly List<NursingCareTask> _tasks = [];

    public Guid Id
    {
        get; private set;
    }
    public Guid AdmissionId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public Guid CreatedByNurseId
    {
        get; private set;
    }
    public string NursingDiagnosis { get; private set; } = string.Empty;
    public string Goal { get; private set; } = string.Empty;
    public CarePlanStatus Status
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? ResolvedAtUtc
    {
        get; private set;
    }
    public string? ResolutionNotes
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    public IReadOnlyCollection<NursingCareTask> Tasks => _tasks.AsReadOnly();

    private NursingCarePlan()
    {
    }

    public static NursingCarePlan Create(
        Guid id,
        Guid admissionId,
        Guid patientId,
        Guid createdByNurseId,
        string nursingDiagnosis,
        string goal,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Bakım planı ID boş olamaz.", nameof(id));
        if (admissionId == Guid.Empty)
            throw new ArgumentException("Yatış ID boş olamaz.", nameof(admissionId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        if (createdByNurseId == Guid.Empty)
            throw new ArgumentException("Oluşturan hemşire ID boş olamaz.", nameof(createdByNurseId));
        if (string.IsNullOrWhiteSpace(nursingDiagnosis))
            throw new ArgumentException("Hemşirelik tanısı boş olamaz.", nameof(nursingDiagnosis));
        if (string.IsNullOrWhiteSpace(goal))
            throw new ArgumentException("Bakım hedefi boş olamaz.", nameof(goal));

        return new NursingCarePlan
        {
            Id = id,
            AdmissionId = admissionId,
            PatientId = patientId,
            CreatedByNurseId = createdByNurseId,
            NursingDiagnosis = nursingDiagnosis.Trim(),
            Goal = goal.Trim(),
            Status = CarePlanStatus.Active,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public NursingCareTask AddTask(
        Guid taskId,
        string title,
        string frequency,
        DateTime dueTimeUtc,
        DateTime nowUtc)
    {
        if (Status != CarePlanStatus.Active)
        {
            throw new InvalidOperationException($"Yalnızca aktif bakım planına görev eklenebilir. Mevcut durum: {Status}");
        }

        var task = NursingCareTask.Create(taskId, Id, title, frequency, dueTimeUtc, nowUtc);
        _tasks.Add(task);
        Version++;
        return task;
    }

    public void Resolve(Guid nurseId, string? notes, DateTime nowUtc)
    {
        if (Status != CarePlanStatus.Active)
        {
            throw new InvalidOperationException($"Yalnızca aktif plan sonlandırılabilir. Mevcut durum: {Status}");
        }

        Status = CarePlanStatus.Resolved;
        ResolvedAtUtc = nowUtc;
        ResolutionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Version++;
    }

    public void Discontinue(Guid nurseId, string reason, DateTime nowUtc)
    {
        if (Status != CarePlanStatus.Active)
        {
            throw new InvalidOperationException($"Yalnızca aktif plan iptal edilebilir. Mevcut durum: {Status}");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(reason));
        }

        Status = CarePlanStatus.Discontinued;
        ResolvedAtUtc = nowUtc;
        ResolutionNotes = reason.Trim();
        Version++;
    }
}
