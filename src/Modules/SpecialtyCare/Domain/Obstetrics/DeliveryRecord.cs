namespace HospitalManagement.Modules.SpecialtyCare.Domain.Obstetrics;

public sealed class DeliveryRecord
{
    private readonly List<NewbornRecord> _newborns = [];

    private DeliveryRecord()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid? PregnancyEpisodeId
    {
        get; private set;
    }
    public Guid MotherPatientId
    {
        get; private set;
    }
    public Guid? EncounterId
    {
        get; private set;
    }
    public string DeliveryProtocolNumber { get; private set; } = string.Empty;

    public DeliveryMode DeliveryMode
    {
        get; private set;
    }
    public DateTime DeliveryTimeUtc
    {
        get; private set;
    }
    public int GestationalAgeWeeks
    {
        get; private set;
    }
    public int GestationalAgeDays
    {
        get; private set;
    }

    public PerinealTearDegree PerinealTear
    {
        get; private set;
    }
    public decimal EstimatedBloodLossMl
    {
        get; private set;
    }

    public Guid AttendingDoctorId
    {
        get; private set;
    }
    public Guid? AssistingMidwifeId
    {
        get; private set;
    }
    public Guid? PediatricianDoctorId
    {
        get; private set;
    }

    public string? MaternalComplicationsNotes
    {
        get; private set;
    }
    public string? DeliverySummaryNotes
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime UpdatedAtUtc
    {
        get; private set;
    }

    public IReadOnlyCollection<NewbornRecord> Newborns => _newborns.AsReadOnly();

    public static DeliveryRecord Create(
        Guid id,
        Guid? pregnancyEpisodeId,
        Guid motherPatientId,
        Guid? encounterId,
        DeliveryMode deliveryMode,
        DateTime deliveryTimeUtc,
        int gestationalAgeWeeks,
        int gestationalAgeDays,
        PerinealTearDegree perinealTear,
        decimal estimatedBloodLossMl,
        Guid attendingDoctorId,
        Guid? assistingMidwifeId,
        Guid? pediatricianDoctorId,
        string? maternalComplicationsNotes,
        string? deliverySummaryNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Doğum kayıt ID boş olamaz.", nameof(id));
        }

        if (motherPatientId == Guid.Empty)
        {
            throw new ArgumentException("Anne hasta ID boş olamaz.", nameof(motherPatientId));
        }

        if (attendingDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Sorumlu doğum hekimi ID boş olamaz.", nameof(attendingDoctorId));
        }

        if (gestationalAgeWeeks is < 20 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(gestationalAgeWeeks), "Doğum gebelik haftası 20-45 aralığında olmalıdır.");
        }

        if (gestationalAgeDays is < 0 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(gestationalAgeDays), "Doğum gebelik günü 0-6 aralığında olmalıdır.");
        }

        if (estimatedBloodLossMl < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedBloodLossMl), "Tahmini kan kaybı negatif olamaz.");
        }

        var protocol = $"DEMO-DEL-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new DeliveryRecord
        {
            Id = id,
            PregnancyEpisodeId = pregnancyEpisodeId,
            MotherPatientId = motherPatientId,
            EncounterId = encounterId,
            DeliveryProtocolNumber = protocol,
            DeliveryMode = deliveryMode,
            DeliveryTimeUtc = deliveryTimeUtc,
            GestationalAgeWeeks = gestationalAgeWeeks,
            GestationalAgeDays = gestationalAgeDays,
            PerinealTear = perinealTear,
            EstimatedBloodLossMl = estimatedBloodLossMl,
            AttendingDoctorId = attendingDoctorId,
            AssistingMidwifeId = assistingMidwifeId,
            PediatricianDoctorId = pediatricianDoctorId,
            MaternalComplicationsNotes = string.IsNullOrWhiteSpace(maternalComplicationsNotes) ? null : maternalComplicationsNotes.Trim(),
            DeliverySummaryNotes = string.IsNullOrWhiteSpace(deliverySummaryNotes) ? null : deliverySummaryNotes.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public NewbornRecord AddNewborn(
        Guid newbornRecordId,
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
        if (_newborns.Any(newborn => newborn.NewbornPatientId == newbornPatientId))
        {
            throw new InvalidOperationException("Aynı Patient kimliği bir doğum kaydında yalnızca bir yenidoğana bağlanabilir.");
        }

        var baby = NewbornRecord.Create(
            newbornRecordId,
            Id,
            newbornPatientId,
            birthOrder,
            birthTimeUtc,
            gender,
            birthWeightGrams,
            birthLengthCm,
            headCircumferenceCm,
            apgarScore1Min,
            apgarScore5Min,
            apgarScore10Min,
            resuscitationGiven,
            cordBloodPh,
            complicationsNotes,
            nowUtc);

        _newborns.Add(baby);
        UpdatedAtUtc = nowUtc;
        return baby;
    }
}
