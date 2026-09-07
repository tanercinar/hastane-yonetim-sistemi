namespace HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

public sealed class NewbornRecord
{
    private NewbornRecord()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid DeliveryRecordId
    {
        get; private set;
    }
    public Guid NewbornPatientId
    {
        get; private set;
    }
    public int BirthOrder
    {
        get; private set;
    }
    public DateTime BirthTimeUtc
    {
        get; private set;
    }
    public NewbornGender Gender
    {
        get; private set;
    }

    public decimal BirthWeightGrams
    {
        get; private set;
    }
    public decimal BirthLengthCm
    {
        get; private set;
    }
    public decimal HeadCircumferenceCm
    {
        get; private set;
    }

    public int ApgarScore1Min
    {
        get; private set;
    }
    public int ApgarScore5Min
    {
        get; private set;
    }
    public int? ApgarScore10Min
    {
        get; private set;
    }

    public ResuscitationIntervention ResuscitationGiven
    {
        get; private set;
    }
    public string? CordBloodPh
    {
        get; private set;
    }
    public string? ComplicationsNotes
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static NewbornRecord Create(
        Guid id,
        Guid deliveryRecordId,
        Guid newbornPatientId,
        int birthOrder,
        DateTime birthTimeUtc,
        NewbornGender gender,
        decimal birthWeightGrams,
        decimal birthLengthCm,
        decimal headCircumferenceCm,
        int apgarScore1Min,
        int apgarScore5Min,
        int? apgarScore10Min,
        ResuscitationIntervention resuscitationGiven,
        string? cordBloodPh,
        string? complicationsNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Yenidoğan kayıt ID boş olamaz.", nameof(id));
        }

        if (deliveryRecordId == Guid.Empty)
        {
            throw new ArgumentException("Doğum kayıt ID boş olamaz.", nameof(deliveryRecordId));
        }

        if (newbornPatientId == Guid.Empty)
        {
            throw new ArgumentException("Yenidoğan hasta ID boş olamaz.", nameof(newbornPatientId));
        }

        if (birthOrder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(birthOrder), "Doğum sırası en az 1 olmalıdır.");
        }

        if (birthWeightGrams is < 300 or > 7000)
        {
            throw new ArgumentOutOfRangeException(nameof(birthWeightGrams), "Doğum ağırlığı fizyolojik sınırda (300 - 7000 g) olmalıdır.");
        }

        if (birthLengthCm is < 20 or > 70)
        {
            throw new ArgumentOutOfRangeException(nameof(birthLengthCm), "Doğum boyu fizyolojik sınırda (20 - 70 cm) olmalıdır.");
        }

        if (headCircumferenceCm is < 15 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(headCircumferenceCm), "Baş çevresi fizyolojik sınırda (15 - 50 cm) olmalıdır.");
        }

        if (apgarScore1Min is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(apgarScore1Min), "1. dakika Apgar skoru 0-10 aralığında olmalıdır.");
        }

        if (apgarScore5Min is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(apgarScore5Min), "5. dakika Apgar skoru 0-10 aralığında olmalıdır.");
        }

        if (apgarScore10Min.HasValue && apgarScore10Min.Value is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(apgarScore10Min), "10. dakika Apgar skoru 0-10 aralığında olmalıdır.");
        }

        return new NewbornRecord
        {
            Id = id,
            DeliveryRecordId = deliveryRecordId,
            NewbornPatientId = newbornPatientId,
            BirthOrder = birthOrder,
            BirthTimeUtc = birthTimeUtc,
            Gender = gender,
            BirthWeightGrams = birthWeightGrams,
            BirthLengthCm = birthLengthCm,
            HeadCircumferenceCm = headCircumferenceCm,
            ApgarScore1Min = apgarScore1Min,
            ApgarScore5Min = apgarScore5Min,
            ApgarScore10Min = apgarScore10Min,
            ResuscitationGiven = resuscitationGiven,
            CordBloodPh = string.IsNullOrWhiteSpace(cordBloodPh) ? null : cordBloodPh.Trim(),
            ComplicationsNotes = string.IsNullOrWhiteSpace(complicationsNotes) ? null : complicationsNotes.Trim(),
            CreatedAtUtc = nowUtc,
        };
    }

}
