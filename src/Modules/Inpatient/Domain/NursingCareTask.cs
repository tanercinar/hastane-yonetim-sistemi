namespace HospitalManagement.Modules.Inpatient.Domain;

public enum CareTaskStatus
{
    Pending,
    Completed,
    Overdue,
    Cancelled,
}

public sealed class NursingCareTask
{
    public Guid Id
    {
        get; private set;
    }
    public Guid CarePlanId
    {
        get; private set;
    }
    public string Title { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = string.Empty; // Q4H, Q8H, Daily, PRN, Once
    public DateTime DueTimeUtc
    {
        get; private set;
    }
    public CareTaskStatus Status
    {
        get; private set;
    }
    public Guid? CompletedByNurseId
    {
        get; private set;
    }
    public DateTime? CompletedAtUtc
    {
        get; private set;
    }
    public string? CompletionNotes
    {
        get; private set;
    }
    public Guid? CancelledByNurseId
    {
        get; private set;
    }
    public DateTime? CancelledAtUtc
    {
        get; private set;
    }
    public string? CancellationReason
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    private NursingCareTask()
    {
    }

    public static NursingCareTask Create(
        Guid id,
        Guid carePlanId,
        string title,
        string frequency,
        DateTime dueTimeUtc,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Görev ID boş olamaz.", nameof(id));
        if (carePlanId == Guid.Empty)
            throw new ArgumentException("Bakım planı ID boş olamaz.", nameof(carePlanId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Görev başlığı boş olamaz.", nameof(title));

        return new NursingCareTask
        {
            Id = id,
            CarePlanId = carePlanId,
            Title = title.Trim(),
            Frequency = string.IsNullOrWhiteSpace(frequency) ? "Daily" : frequency.Trim(),
            DueTimeUtc = dueTimeUtc,
            Status = CareTaskStatus.Pending,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Complete(Guid nurseId, string? notes, DateTime nowUtc)
    {
        if (Status == CareTaskStatus.Completed)
        {
            throw new InvalidOperationException("Görev zaten tamamlanmış durumda.");
        }

        if (Status == CareTaskStatus.Cancelled)
        {
            throw new InvalidOperationException("İptal edilmiş bir görev tamamlanamaz.");
        }

        if (nurseId == Guid.Empty)
        {
            throw new ArgumentException("Tamamlayan hemşire ID boş olamaz.", nameof(nurseId));
        }

        Status = CareTaskStatus.Completed;
        CompletedByNurseId = nurseId;
        CompletedAtUtc = nowUtc;
        CompletionNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Version++;
    }

    public void Cancel(Guid nurseId, string reason, DateTime nowUtc)
    {
        if (Status == CareTaskStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış bir görev iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi boş olamaz.", nameof(reason));
        }

        Status = CareTaskStatus.Cancelled;
        CancelledByNurseId = nurseId;
        CancelledAtUtc = nowUtc;
        CancellationReason = reason.Trim();
        Version++;
    }

    public void CheckAndMarkOverdue(DateTime nowUtc)
    {
        if (Status == CareTaskStatus.Pending && DueTimeUtc < nowUtc)
        {
            Status = CareTaskStatus.Overdue;
            Version++;
        }
    }
}
