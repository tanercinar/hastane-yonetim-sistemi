namespace HospitalManagement.Modules.Pharmacy.Domain;

/// <summary>
/// Durable receipt for a dispense request. The request fingerprint contains no clinical text.
/// </summary>
public sealed class PrescriptionDispenseOperation
{
    private PrescriptionDispenseOperation()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid IdempotencyKey
    {
        get; private set;
    }
    public Guid PrescriptionId
    {
        get; private set;
    }
    public string RequestFingerprint { get; private set; } = string.Empty;
    public DateTime CompletedAtUtc
    {
        get; private set;
    }

    public static PrescriptionDispenseOperation Create(
        Guid id,
        Guid idempotencyKey,
        Guid prescriptionId,
        string requestFingerprint,
        DateTime completedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("İşlem kimliği boş olamaz.", nameof(id));
        }

        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException("Idempotency anahtarı boş olamaz.", nameof(idempotencyKey));
        }

        if (prescriptionId == Guid.Empty)
        {
            throw new ArgumentException("Reçete kimliği boş olamaz.", nameof(prescriptionId));
        }

        if (string.IsNullOrWhiteSpace(requestFingerprint))
        {
            throw new ArgumentException("İstek parmak izi boş olamaz.", nameof(requestFingerprint));
        }

        return new PrescriptionDispenseOperation
        {
            Id = id,
            IdempotencyKey = idempotencyKey,
            PrescriptionId = prescriptionId,
            RequestFingerprint = requestFingerprint.Trim(),
            CompletedAtUtc = DateTime.SpecifyKind(completedAtUtc, DateTimeKind.Utc),
        };
    }
}
