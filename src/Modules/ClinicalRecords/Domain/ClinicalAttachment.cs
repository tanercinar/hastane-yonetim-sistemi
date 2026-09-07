using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class ClinicalAttachment : IHasConcurrencyVersion
{
    private ClinicalAttachment()
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

    public Guid PatientId
    {
        get; private set;
    }

    public Guid UploadedByPractitionerId
    {
        get; private set;
    }

    public ClinicalAttachmentType AttachmentType
    {
        get; private set;
    }

    public string FileName { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long ByteSize
    {
        get; private set;
    }

    public string Sha256Checksum { get; private set; } = string.Empty;

    public string? Description
    {
        get; private set;
    }

    public DateTime UploadedAtUtc
    {
        get; private set;
    }

    public bool IsEnteredInError
    {
        get; private set;
    }

    public string? EnteredInErrorReason
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public static ClinicalAttachment Create(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid uploadedByPractitionerId,
        ClinicalAttachmentType attachmentType,
        string fileName,
        string storageKey,
        string contentType,
        long byteSize,
        string sha256Checksum,
        string? description,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Ek kimliği boş olamaz.", nameof(id));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (uploadedByPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Yükleyen sağlık personeli kimliği boş olamaz.", nameof(uploadedByPractitionerId));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Dosya adı boş olamaz.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Depolama anahtarı boş olamaz.", nameof(storageKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("İçerik türü (MIME) boş olamaz.", nameof(contentType));
        }

        if (byteSize <= 0)
        {
            throw new ArgumentException("Dosya boyutu sıfırdan büyük olmalıdır.", nameof(byteSize));
        }

        if (string.IsNullOrWhiteSpace(sha256Checksum))
        {
            throw new ArgumentException("SHA-256 sağlama değeri boş olamaz.", nameof(sha256Checksum));
        }

        return new ClinicalAttachment
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = patientId,
            UploadedByPractitionerId = uploadedByPractitionerId,
            AttachmentType = attachmentType,
            FileName = fileName.Trim(),
            StorageKey = storageKey.Trim(),
            ContentType = contentType.Trim(),
            ByteSize = byteSize,
            Sha256Checksum = sha256Checksum.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            UploadedAtUtc = nowUtc,
            IsEnteredInError = false,
            Version = 1,
        };
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (IsEnteredInError)
        {
            throw new InvalidOperationException("Bu klinik ek zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        IsEnteredInError = true;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
