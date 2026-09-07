namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

public sealed class DentalToothCondition
{
    private DentalToothCondition()
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
    public int ToothNumber
    {
        get; private set;
    }
    public ToothCondition Condition
    {
        get; private set;
    }
    public ToothSurface AffectedSurfaces
    {
        get; private set;
    }
    public string? Notes
    {
        get; private set;
    }
    public DateTime RecordedAtUtc
    {
        get; private set;
    }
    public Guid RecordedByStaffId
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    public static DentalToothCondition Record(
        Guid id,
        Guid patientId,
        int toothNumber,
        ToothCondition condition,
        ToothSurface affectedSurfaces,
        string? notes,
        Guid recordedByStaffId,
        int version,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Kayıt ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (!FdiToothValidator.IsValidToothNumber(toothNumber))
        {
            throw new ArgumentOutOfRangeException(nameof(toothNumber), $"Geçersiz FDI diş numarası: {toothNumber}. (11-48 veya 51-85 olmalıdır)");
        }

        if (recordedByStaffId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden personel ID boş olamaz.", nameof(recordedByStaffId));
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Versiyon en az 1 olmalıdır.");
        }

        return new DentalToothCondition
        {
            Id = id,
            PatientId = patientId,
            ToothNumber = toothNumber,
            Condition = condition,
            AffectedSurfaces = affectedSurfaces,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RecordedByStaffId = recordedByStaffId,
            Version = version,
            RecordedAtUtc = nowUtc,
        };
    }
}
