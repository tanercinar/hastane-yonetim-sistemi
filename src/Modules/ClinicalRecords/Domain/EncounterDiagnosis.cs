using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class EncounterDiagnosis : IHasConcurrencyVersion
{
    private EncounterDiagnosis()
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

    public Guid DiagnosedByPractitionerId
    {
        get; private set;
    }

    public DiagnosisType DiagnosisType
    {
        get; private set;
    }

    public bool IsCoded
    {
        get; private set;
    }

    public string? Icd10Code
    {
        get; private set;
    }

    public string DiagnosisTitle { get; private set; } = string.Empty;

    public string? CatalogVersion
    {
        get; private set;
    }

    public string? Notes
    {
        get; private set;
    }

    public DateTime DiagnosedAtUtc
    {
        get; private set;
    }

    public DateTime? UpdatedAtUtc
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

    public long Version { get; set; } = 1;

    public static EncounterDiagnosis CreateCoded(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid diagnosedByPractitionerId,
        DiagnosisType diagnosisType,
        string icd10Code,
        string diagnosisTitle,
        string catalogVersion,
        string? notes,
        DateTime nowUtc)
    {
        ValidateCommon(id, encounterId, patientId, diagnosedByPractitionerId, diagnosisTitle);

        if (string.IsNullOrWhiteSpace(icd10Code))
        {
            throw new ArgumentException("Kodlanmış tanıda ICD-10 kodu zorunludur.", nameof(icd10Code));
        }

        return new EncounterDiagnosis
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosedByPractitionerId = diagnosedByPractitionerId,
            DiagnosisType = diagnosisType,
            IsCoded = true,
            Icd10Code = icd10Code.Trim().ToUpperInvariant(),
            DiagnosisTitle = diagnosisTitle.Trim(),
            CatalogVersion = string.IsNullOrWhiteSpace(catalogVersion) ? "ICD-10-TR-2026.1" : catalogVersion.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DiagnosedAtUtc = nowUtc,
            IsEnteredInError = false,
            EnteredInErrorReason = null,
            Version = 1,
        };
    }

    public static EncounterDiagnosis CreateFreeText(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid diagnosedByPractitionerId,
        DiagnosisType diagnosisType,
        string diagnosisTitle,
        string? notes,
        DateTime nowUtc)
    {
        ValidateCommon(id, encounterId, patientId, diagnosedByPractitionerId, diagnosisTitle);

        return new EncounterDiagnosis
        {
            Id = id,
            EncounterId = encounterId,
            PatientId = patientId,
            DiagnosedByPractitionerId = diagnosedByPractitionerId,
            DiagnosisType = diagnosisType,
            IsCoded = false,
            Icd10Code = null,
            DiagnosisTitle = diagnosisTitle.Trim(),
            CatalogVersion = null,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DiagnosedAtUtc = nowUtc,
            IsEnteredInError = false,
            EnteredInErrorReason = null,
            Version = 1,
        };
    }

    public void Update(
        DiagnosisType diagnosisType,
        bool isCoded,
        string? icd10Code,
        string diagnosisTitle,
        string? catalogVersion,
        string? notes,
        DateTime nowUtc)
    {
        if (IsEnteredInError)
        {
            throw new InvalidOperationException("Hatalı giriş olarak işaretlenmiş bir tanı kaydı güncellenemez.");
        }

        if (DiagnosisType == DiagnosisType.Final)
        {
            throw new InvalidOperationException(
                "Kesin tanı sessizce değiştirilemez; gerekçeli hatalı giriş ve yeni kayıt akışı kullanılmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(diagnosisTitle))
        {
            throw new ArgumentException("Tanı başlığı boş olamaz.", nameof(diagnosisTitle));
        }

        if (isCoded && string.IsNullOrWhiteSpace(icd10Code))
        {
            throw new ArgumentException("Kodlanmış tanıda ICD-10 kodu zorunludur.", nameof(icd10Code));
        }

        DiagnosisType = diagnosisType;
        IsCoded = isCoded;
        Icd10Code = isCoded ? icd10Code?.Trim().ToUpperInvariant() : null;
        DiagnosisTitle = diagnosisTitle.Trim();
        CatalogVersion = isCoded ? (string.IsNullOrWhiteSpace(catalogVersion) ? "ICD-10-TR-2026.1" : catalogVersion.Trim()) : null;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (IsEnteredInError)
        {
            throw new InvalidOperationException("Tanı kaydı zaten hatalı giriş olarak işaretlenmiştir.");
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

    private static void ValidateCommon(
        Guid id,
        Guid encounterId,
        Guid patientId,
        Guid diagnosedByPractitionerId,
        string diagnosisTitle)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Tanı kaydı kimliği boş olamaz.", nameof(id));
        }

        if (encounterId == Guid.Empty)
        {
            throw new ArgumentException("Karşılaşma kimliği boş olamaz.", nameof(encounterId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (diagnosedByPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Tanı koyan hekim kimliği boş olamaz.", nameof(diagnosedByPractitionerId));
        }

        if (string.IsNullOrWhiteSpace(diagnosisTitle))
        {
            throw new ArgumentException("Tanı başlığı boş olamaz.", nameof(diagnosisTitle));
        }
    }
}
