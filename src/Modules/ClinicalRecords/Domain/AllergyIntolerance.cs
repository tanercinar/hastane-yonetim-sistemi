using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class AllergyIntolerance : IHasConcurrencyVersion
{
    private AllergyIntolerance()
    {
    }

    public Guid Id
    {
        get; private set;
    }

    public Guid PatientId
    {
        get; private set;
    }

    public Guid? EncounterId
    {
        get; private set;
    }

    public string Substance { get; private set; } = string.Empty;

    public AllergyCategory Category
    {
        get; private set;
    }

    public AllergyCriticality Criticality
    {
        get; private set;
    }

    public AllergyClinicalStatus ClinicalStatus
    {
        get; private set;
    }

    public AllergyVerificationStatus VerificationStatus
    {
        get; private set;
    }

    public string? Manifestation
    {
        get; private set;
    }

    public DateTime? OnsetDateTimeUtc
    {
        get; private set;
    }

    public string? Notes
    {
        get; private set;
    }

    public Guid RecordedByPractitionerId
    {
        get; private set;
    }

    public DateTime RecordedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }

    public string? EnteredInErrorReason
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public static AllergyIntolerance Create(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        string substance,
        AllergyCategory category,
        AllergyCriticality criticality,
        string? manifestation,
        DateTime? onsetDateTimeUtc,
        string? notes,
        Guid recordedByPractitionerId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Alerji kimliği boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(substance))
        {
            throw new ArgumentException("Alerjen madde / etken adı boş olamaz.", nameof(substance));
        }

        if (recordedByPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden sağlık personeli kimliği boş olamaz.", nameof(recordedByPractitionerId));
        }

        return new AllergyIntolerance
        {
            Id = id,
            PatientId = patientId,
            EncounterId = encounterId,
            Substance = substance.Trim(),
            Category = category,
            Criticality = criticality,
            ClinicalStatus = AllergyClinicalStatus.Active,
            VerificationStatus = AllergyVerificationStatus.Confirmed,
            Manifestation = string.IsNullOrWhiteSpace(manifestation) ? null : manifestation.Trim(),
            OnsetDateTimeUtc = onsetDateTimeUtc,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedByPractitionerId = recordedByPractitionerId,
            RecordedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void UpdateStatus(
        AllergyClinicalStatus newClinicalStatus,
        string? notes,
        Guid practitionerId,
        DateTime nowUtc)
    {
        if (VerificationStatus == AllergyVerificationStatus.EnteredInError)
        {
            throw new InvalidOperationException("Hatalı giriş olarak işaretlenmiş bir alerji kaydı güncellenemez.");
        }

        ClinicalStatus = newClinicalStatus;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }

        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (VerificationStatus == AllergyVerificationStatus.EnteredInError)
        {
            throw new InvalidOperationException("Alerji kaydı zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        VerificationStatus = AllergyVerificationStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
