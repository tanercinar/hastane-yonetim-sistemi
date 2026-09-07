using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class ClinicalProblem : IHasConcurrencyVersion
{
    private ClinicalProblem()
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

    public string ProblemTitle { get; private set; } = string.Empty;

    public string? Code
    {
        get; private set;
    }

    public ProblemCategory Category
    {
        get; private set;
    }

    public ProblemClinicalStatus ClinicalStatus
    {
        get; private set;
    }

    public ProblemVerificationStatus VerificationStatus
    {
        get; private set;
    }

    public DateOnly? OnsetDate
    {
        get; private set;
    }

    public DateOnly? ResolvedDate
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

    public static ClinicalProblem Create(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        string problemTitle,
        string? code,
        ProblemCategory category,
        DateOnly? onsetDate,
        string? notes,
        Guid recordedByPractitionerId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Problem kimliği boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (string.IsNullOrWhiteSpace(problemTitle))
        {
            throw new ArgumentException("Problem başlığı boş olamaz.", nameof(problemTitle));
        }

        if (recordedByPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden hekim kimliği boş olamaz.", nameof(recordedByPractitionerId));
        }

        return new ClinicalProblem
        {
            Id = id,
            PatientId = patientId,
            EncounterId = encounterId,
            ProblemTitle = problemTitle.Trim(),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            Category = category,
            ClinicalStatus = ProblemClinicalStatus.Active,
            VerificationStatus = ProblemVerificationStatus.Confirmed,
            OnsetDate = onsetDate,
            ResolvedDate = null,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedByPractitionerId = recordedByPractitionerId,
            RecordedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void UpdateStatus(
        ProblemClinicalStatus newClinicalStatus,
        DateOnly? resolvedDate,
        string? notes,
        Guid practitionerId,
        DateTime nowUtc)
    {
        if (VerificationStatus == ProblemVerificationStatus.EnteredInError)
        {
            throw new InvalidOperationException("Hatalı giriş olarak işaretlenmiş bir problem kaydı güncellenemez.");
        }

        ClinicalStatus = newClinicalStatus;
        if (newClinicalStatus == ProblemClinicalStatus.Resolved && resolvedDate.HasValue)
        {
            ResolvedDate = resolvedDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }

        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (VerificationStatus == ProblemVerificationStatus.EnteredInError)
        {
            throw new InvalidOperationException("Problem kaydı zaten hatalı giriş olarak işaretlenmiştir.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hatalı giriş gerekçesi zorunludur.", nameof(reason));
        }

        VerificationStatus = ProblemVerificationStatus.EnteredInError;
        EnteredInErrorReason = reason.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
