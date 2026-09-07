namespace HospitalManagement.Modules.Inpatient.Domain;

public enum TransferStatus
{
    Requested = 1,
    Accepted = 2,
    Completed = 3,
    Cancelled = 4,
}

public sealed class InpatientTransfer
{
    private InpatientTransfer()
    {
    }

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
    public Guid SourceWardId
    {
        get; private set;
    }
    public Guid SourceBedId
    {
        get; private set;
    }
    public Guid TargetWardId
    {
        get; private set;
    }
    public Guid? TargetBedId
    {
        get; private set;
    }
    public string TransferReason { get; private set; } = string.Empty;
    public string? ClinicalNotes
    {
        get; private set;
    }
    public TransferStatus Status
    {
        get; private set;
    }
    public Guid RequestedByUserId
    {
        get; private set;
    }
    public DateTime RequestedAtUtc
    {
        get; private set;
    }
    public Guid? AcceptedByUserId
    {
        get; private set;
    }
    public DateTime? AcceptedAtUtc
    {
        get; private set;
    }
    public Guid? CompletedByUserId
    {
        get; private set;
    }
    public DateTime? CompletedAtUtc
    {
        get; private set;
    }
    public Guid? CancelledByUserId
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
    public int Version
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public static InpatientTransfer Request(
        Guid id,
        Guid admissionId,
        Guid patientId,
        Guid sourceWardId,
        Guid sourceBedId,
        Guid targetWardId,
        Guid? targetBedId,
        string transferReason,
        string? clinicalNotes,
        Guid requestedByUserId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id boş olamaz.", nameof(id));
        if (admissionId == Guid.Empty)
            throw new ArgumentException("AdmissionId boş olamaz.", nameof(admissionId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("PatientId boş olamaz.", nameof(patientId));
        if (sourceWardId == Guid.Empty)
            throw new ArgumentException("SourceWardId boş olamaz.", nameof(sourceWardId));
        if (sourceBedId == Guid.Empty)
            throw new ArgumentException("SourceBedId boş olamaz.", nameof(sourceBedId));
        if (targetWardId == Guid.Empty)
            throw new ArgumentException("TargetWardId boş olamaz.", nameof(targetWardId));
        if (string.IsNullOrWhiteSpace(transferReason))
            throw new ArgumentException("Transfer gerekçesi belirtilmelidir.", nameof(transferReason));

        return new InpatientTransfer
        {
            Id = id,
            AdmissionId = admissionId,
            PatientId = patientId,
            SourceWardId = sourceWardId,
            SourceBedId = sourceBedId,
            TargetWardId = targetWardId,
            TargetBedId = targetBedId,
            TransferReason = transferReason.Trim(),
            ClinicalNotes = clinicalNotes?.Trim(),
            Status = TransferStatus.Requested,
            RequestedByUserId = requestedByUserId,
            RequestedAtUtc = nowUtc,
            Version = 1,
            CreatedAtUtc = nowUtc,
        };
    }

    public void Accept(Guid acceptedByUserId, Guid? targetBedId, DateTime nowUtc)
    {
        if (Status != TransferStatus.Requested)
        {
            throw new InvalidOperationException($"Yalnızca beklemedeki (Requested) transferler kabul edilebilir. Mevcut durum: {Status}");
        }

        Status = TransferStatus.Accepted;
        AcceptedByUserId = acceptedByUserId;
        AcceptedAtUtc = nowUtc;
        if (targetBedId.HasValue && targetBedId.Value != Guid.Empty)
        {
            TargetBedId = targetBedId.Value;
        }
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Complete(Guid completedByUserId, Guid targetBedId, DateTime nowUtc)
    {
        if (Status != TransferStatus.Requested && Status != TransferStatus.Accepted)
        {
            throw new InvalidOperationException($"Yalnızca beklemede veya kabul edilmiş transferler tamamlanabilir. Mevcut durum: {Status}");
        }

        if (targetBedId == Guid.Empty)
        {
            throw new ArgumentException("Transferin tamamlanabilmesi için hedef yatak belirtilmelidir.", nameof(targetBedId));
        }

        Status = TransferStatus.Completed;
        TargetBedId = targetBedId;
        CompletedByUserId = completedByUserId;
        CompletedAtUtc = nowUtc;
        Version++;
        UpdatedAtUtc = nowUtc;
    }

    public void Cancel(Guid cancelledByUserId, string reason, DateTime nowUtc)
    {
        if (Status == TransferStatus.Completed)
        {
            throw new InvalidOperationException("Tamamlanmış bir transfer iptal edilemez.");
        }

        if (Status == TransferStatus.Cancelled)
        {
            throw new InvalidOperationException("Transfer zaten iptal edilmiş durumda.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İptal gerekçesi belirtilmelidir.", nameof(reason));
        }

        Status = TransferStatus.Cancelled;
        CancelledByUserId = cancelledByUserId;
        CancelledAtUtc = nowUtc;
        CancellationReason = reason.Trim();
        Version++;
        UpdatedAtUtc = nowUtc;
    }
}
