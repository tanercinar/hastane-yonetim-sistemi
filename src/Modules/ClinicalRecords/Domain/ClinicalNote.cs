using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class ClinicalNote : IHasConcurrencyVersion
{
    private ClinicalNote()
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

    public Guid AuthorPractitionerId
    {
        get; private set;
    }

    public ClinicalNoteType NoteType
    {
        get; private set;
    }

    public ClinicalNoteStatus Status
    {
        get; private set;
    }

    public string Title { get; private set; } = string.Empty;

    public string? ChiefComplaint
    {
        get; private set;
    }

    public string? HistoryOfPresentIllness
    {
        get; private set;
    }

    public string? PhysicalExamination
    {
        get; private set;
    }

    public string? Assessment
    {
        get; private set;
    }

    public string? Plan
    {
        get; private set;
    }

    public string? Content
    {
        get; private set;
    }

    public DateTime? SignedAtUtc
    {
        get; private set;
    }

    public Guid? SignedByPractitionerId
    {
        get; private set;
    }

    public Guid? ParentNoteId
    {
        get; private set;
    }

    public string? CorrectionReason
    {
        get; private set;
    }

    public string? EnteredInErrorReason
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

    public long Version { get; set; } = 1;

    public static ClinicalNote CreateDraft(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid authorPractitionerId,
        ClinicalNoteType noteType,
        string title,
        string? chiefComplaint,
        string? historyOfPresentIllness,
        string? physicalExamination,
        string? assessment,
        string? plan,
        string? content,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Not kimliği boş olamaz.", nameof(id));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (authorPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Yazar sağlık personeli kimliği boş olamaz.", nameof(authorPractitionerId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Not başlığı boş olamaz.", nameof(title));
        }

        return new ClinicalNote
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = patientId,
            AuthorPractitionerId = authorPractitionerId,
            NoteType = noteType,
            Status = ClinicalNoteStatus.Draft,
            Title = title.Trim(),
            ChiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? null : chiefComplaint.Trim(),
            HistoryOfPresentIllness = string.IsNullOrWhiteSpace(historyOfPresentIllness) ? null : historyOfPresentIllness.Trim(),
            PhysicalExamination = string.IsNullOrWhiteSpace(physicalExamination) ? null : physicalExamination.Trim(),
            Assessment = string.IsNullOrWhiteSpace(assessment) ? null : assessment.Trim(),
            Plan = string.IsNullOrWhiteSpace(plan) ? null : plan.Trim(),
            Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim(),
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void UpdateDraft(
        string title,
        string? chiefComplaint,
        string? historyOfPresentIllness,
        string? physicalExamination,
        string? assessment,
        string? plan,
        string? content,
        DateTime nowUtc)
    {
        if (Status != ClinicalNoteStatus.Draft)
        {
            throw new InvalidOperationException("İmzalanmış veya kapatılmış bir klinik not doğrudan değiştirilemez. Lütfen ek not (addendum) oluşturunuz.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Not başlığı boş olamaz.", nameof(title));
        }

        Title = title.Trim();
        ChiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? null : chiefComplaint.Trim();
        HistoryOfPresentIllness = string.IsNullOrWhiteSpace(historyOfPresentIllness) ? null : historyOfPresentIllness.Trim();
        PhysicalExamination = string.IsNullOrWhiteSpace(physicalExamination) ? null : physicalExamination.Trim();
        Assessment = string.IsNullOrWhiteSpace(assessment) ? null : assessment.Trim();
        Plan = string.IsNullOrWhiteSpace(plan) ? null : plan.Trim();
        Content = string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Sign(Guid practitionerId, DateTime nowUtc)
    {
        if (Status != ClinicalNoteStatus.Draft)
        {
            throw new InvalidOperationException("Yalnızca taslak durumundaki klinik notlar imzalanabilir.");
        }

        if (practitionerId == Guid.Empty)
        {
            throw new ArgumentException("İmzalayan sağlık personeli kimliği boş olamaz.", nameof(practitionerId));
        }

        Status = ClinicalNoteStatus.Signed;
        SignedByPractitionerId = practitionerId;
        SignedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public ClinicalNote CreateAddendum(
        Guid addendumId,
        string addendumContent,
        string reason,
        Guid authorPractitionerId,
        DateTime nowUtc)
    {
        if (Status != ClinicalNoteStatus.Signed && Status != ClinicalNoteStatus.Amended)
        {
            throw new InvalidOperationException("Yalnızca imzalanmış notlara ek not (addendum) eklenebilir.");
        }

        if (string.IsNullOrWhiteSpace(addendumContent))
        {
            throw new ArgumentException("Ek not içeriği boş olamaz.", nameof(addendumContent));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Ek not gerekçesi boş olamaz.", nameof(reason));
        }

        Status = ClinicalNoteStatus.Amended;
        UpdatedAtUtc = nowUtc;
        Version++;

        return new ClinicalNote
        {
            Id = addendumId,
            EncounterId = EncounterId,
            PatientId = PatientId,
            AuthorPractitionerId = authorPractitionerId,
            NoteType = ClinicalNoteType.Addendum,
            Status = ClinicalNoteStatus.Signed,
            Title = $"Ek Not ({Title})",
            Content = addendumContent.Trim(),
            ParentNoteId = Id,
            CorrectionReason = reason.Trim(),
            SignedByPractitionerId = authorPractitionerId,
            SignedAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (Status == ClinicalNoteStatus.EnteredInError)
        {
            throw new InvalidOperationException("Klinik not zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        Status = ClinicalNoteStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
