namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class Specimen
{
    private readonly List<SpecimenTransitionEvent> _transitions = [];

    public Guid Id
    {
        get; private set;
    }
    public string Barcode { get; private set; } = string.Empty;
    public Guid DiagnosticOrderId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public string SpecimenType { get; private set; } = string.Empty;
    public string ContainerType { get; private set; } = string.Empty;
    public SpecimenStatus Status
    {
        get; private set;
    }
    public string? CollectionNotes
    {
        get; private set;
    }
    public string? RejectionReason
    {
        get; private set;
    }
    public DateTime? CollectedAtUtc
    {
        get; private set;
    }
    public Guid? CollectedByUserId
    {
        get; private set;
    }
    public DateTime? ReceivedAtUtc
    {
        get; private set;
    }
    public Guid? ReceivedByUserId
    {
        get; private set;
    }
    public DateTime? RejectedAtUtc
    {
        get; private set;
    }
    public Guid? RejectedByUserId
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
    public int Version
    {
        get; private set;
    }

    public IReadOnlyCollection<SpecimenTransitionEvent> Transitions => _transitions.AsReadOnly();

    private Specimen()
    {
    }

    public static Specimen Collect(
        Guid id,
        string barcode,
        Guid diagnosticOrderId,
        Guid patientId,
        string specimenType,
        string containerType,
        Guid collectedByUserId,
        string collectedByRole,
        string? collectionLocation,
        string? collectionNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Numune kimliği zorunludur.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new ArgumentException("Numune barkodu zorunludur.", nameof(barcode));
        }

        if (diagnosticOrderId == Guid.Empty)
        {
            throw new ArgumentException("Tanısal istem kimliği zorunludur.", nameof(diagnosticOrderId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği zorunludur.", nameof(patientId));
        }

        if (collectedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Numuneyi alan kullanıcı kimliği zorunludur.", nameof(collectedByUserId));
        }

        var specimen = new Specimen
        {
            Id = id,
            Barcode = barcode.Trim().ToUpperInvariant(),
            DiagnosticOrderId = diagnosticOrderId,
            PatientId = patientId,
            SpecimenType = string.IsNullOrWhiteSpace(specimenType) ? "Venöz Tam Kan" : specimenType.Trim(),
            ContainerType = string.IsNullOrWhiteSpace(containerType) ? "Mor Kapaklı EDTA Tüp" : containerType.Trim(),
            Status = SpecimenStatus.Collected,
            CollectionNotes = collectionNotes?.Trim(),
            CollectedAtUtc = nowUtc,
            CollectedByUserId = collectedByUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = null,
            Version = 1,
        };

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            id,
            SpecimenStatus.Collected,
            SpecimenStatus.Collected,
            nowUtc,
            collectedByUserId,
            collectedByRole,
            collectionLocation ?? "Örnek Alma Ünitesi",
            collectionNotes ?? "Numune başarıyla alındı ve barkodlandı.");

        specimen._transitions.Add(transition);
        return specimen;
    }

    public SpecimenTransitionEvent MarkInTransit(
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != SpecimenStatus.Collected)
        {
            throw new InvalidOperationException($"Yalnızca 'Toplandı' durumundaki numuneler taşımaya verilebilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.InTransit;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Pnömatik Taşıma / Numune Kuryesi",
            notes ?? "Numune laboratuvara transfer edilmek üzere kuryeye/pnömatik hatta verildi.");

        _transitions.Add(transition);
        return transition;
    }

    public SpecimenTransitionEvent ReceiveAtLab(
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != SpecimenStatus.Collected && Status != SpecimenStatus.InTransit)
        {
            throw new InvalidOperationException($"Yalnızca 'Toplandı' veya 'Taşımada' durumundaki numuneler laboratuvarda kabul edilebilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.Received;
        ReceivedAtUtc = nowUtc;
        ReceivedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Merkez Laboratuvar Numune Kabul Masası",
            notes ?? "Numune laboratuvar tarafından kabul edildi ve barkod doğrulandı.");

        _transitions.Add(transition);
        return transition;
    }

    public SpecimenTransitionEvent Reject(
        Guid actorUserId,
        string actorRole,
        string rejectionReason,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            throw new ArgumentException("Numune reddi için zorunlu bir red gerekçesi belirtilmelidir.", nameof(rejectionReason));
        }

        if (Status != SpecimenStatus.Collected && Status != SpecimenStatus.InTransit && Status != SpecimenStatus.Received)
        {
            throw new InvalidOperationException($"Yalnızca çalışmaya alınmamış numuneler reddedilebilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.Rejected;
        RejectionReason = rejectionReason.Trim();
        RejectedAtUtc = nowUtc;
        RejectedByUserId = actorUserId;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Merkez Laboratuvar",
            $"Numune Reddedildi. Gerekçe: {rejectionReason}. Ek Not: {notes}");

        _transitions.Add(transition);
        return transition;
    }

    public SpecimenTransitionEvent StartProcessing(
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != SpecimenStatus.Received)
        {
            throw new InvalidOperationException($"Yalnızca laboratuvarda kabul edilmiş numuneler çalışmaya alınabilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.Processing;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Analizör Ünitesi",
            notes ?? "Numune cihazda analiz çalışmasına alındı.");

        _transitions.Add(transition);
        return transition;
    }

    public SpecimenTransitionEvent Complete(
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != SpecimenStatus.Processing)
        {
            throw new InvalidOperationException($"Yalnızca çalışılan numuneler tamamlandı durumuna geçebilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.Completed;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Merkez Laboratuvar",
            notes ?? "Numune analizi tamamlandı.");

        _transitions.Add(transition);
        return transition;
    }

    public SpecimenTransitionEvent Dispose(
        Guid actorUserId,
        string actorRole,
        string? location,
        string? notes,
        DateTime nowUtc)
    {
        if (Status != SpecimenStatus.Completed && Status != SpecimenStatus.Rejected)
        {
            throw new InvalidOperationException($"Yalnızca tamamlanmış veya reddedilmiş numuneler imha edilebilir. Mevcut durum: {Status}");
        }

        var fromStatus = Status;
        Status = SpecimenStatus.Disposed;
        UpdatedAtUtc = nowUtc;
        Version++;

        var transition = SpecimenTransitionEvent.Create(
            Guid.NewGuid(),
            Id,
            fromStatus,
            Status,
            nowUtc,
            actorUserId,
            actorRole,
            location ?? "Tıbbi Atık Ünitesi",
            notes ?? "Numune yasal saklama süresi sonrası güvenli tıbbi atık protokolü ile imha edildi.");

        _transitions.Add(transition);
        return transition;
    }
}
