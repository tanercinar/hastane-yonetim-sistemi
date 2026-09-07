using HospitalManagement.BuildingBlocks.Persistence;

namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public sealed class VitalSignObservation : IHasConcurrencyVersion
{
    private VitalSignObservation()
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

    public VitalSignType MeasurementType
    {
        get; private set;
    }

    public decimal Value
    {
        get; private set;
    }

    public string Unit { get; private set; } = string.Empty;

    public VitalInterpretation Interpretation
    {
        get; private set;
    }

    public string? MeasurementMethod
    {
        get; private set;
    }

    public DateTime MeasuredAtUtc
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

    public bool IsEnteredInError
    {
        get; private set;
    }

    public string? EnteredInErrorReason
    {
        get; private set;
    }

    public long Version { get; set; } = 1;

    public static VitalSignObservation Create(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        VitalSignType measurementType,
        decimal value,
        string unit,
        string? measurementMethod,
        DateTime measuredAtUtc,
        string? notes,
        Guid recordedByPractitionerId,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Gözlem kimliği boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta kimliği boş olamaz.", nameof(patientId));
        }

        if (recordedByPractitionerId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden sağlık personeli kimliği boş olamaz.", nameof(recordedByPractitionerId));
        }

        var (isValid, errorMessage) = VitalSignValidationRules.Validate(measurementType, value, unit);
        if (!isValid)
        {
            throw new ArgumentOutOfRangeException(nameof(value), errorMessage);
        }

        var interpretation = VitalSignValidationRules.DetermineInterpretation(measurementType, value);

        return new VitalSignObservation
        {
            Id = id,
            PatientId = patientId,
            EncounterId = encounterId,
            MeasurementType = measurementType,
            Value = value,
            Unit = unit.Trim(),
            Interpretation = interpretation,
            MeasurementMethod = string.IsNullOrWhiteSpace(measurementMethod) ? null : measurementMethod.Trim(),
            MeasuredAtUtc = measuredAtUtc == default ? nowUtc : measuredAtUtc,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedByPractitionerId = recordedByPractitionerId,
            RecordedAtUtc = nowUtc,
            IsEnteredInError = false,
            EnteredInErrorReason = null,
            Version = 1,
        };
    }

    public void MarkEnteredInError(Guid practitionerId, string reason, DateTime nowUtc)
    {
        if (IsEnteredInError)
        {
            throw new InvalidOperationException("Vital bulgu kaydı zaten hatalı giriş olarak işaretlenmiştir.");
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
