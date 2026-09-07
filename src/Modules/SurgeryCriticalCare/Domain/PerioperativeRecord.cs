namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class PerioperativeRecord
{
    private readonly List<PerioperativeCorrection> _corrections = [];

    private PerioperativeRecord()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid SurgeryBookingId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public Guid OperatingRoomId
    {
        get; private set;
    }
    public DateTime? RoomEntryTimeUtc
    {
        get; private set;
    }
    public DateTime? AnesthesiaStartTimeUtc
    {
        get; private set;
    }
    public DateTime? IncisionTimeUtc
    {
        get; private set;
    }
    public DateTime? ClosureTimeUtc
    {
        get; private set;
    }
    public DateTime? AnesthesiaEndTimeUtc
    {
        get; private set;
    }
    public DateTime? RoomExitTimeUtc
    {
        get; private set;
    }
    public AnesthesiaType AnesthesiaType
    {
        get; private set;
    }
    public string? AnesthesiaNotes
    {
        get; private set;
    }
    public string? IntraoperativeFindings
    {
        get; private set;
    }
    public string? IntraoperativeComplications
    {
        get; private set;
    }
    public int? EstimatedBloodLossMl
    {
        get; private set;
    }
    public string? SpecimensCollected
    {
        get; private set;
    }
    public bool CountsConfirmed
    {
        get; private set;
    }
    public PostOpDisposition PostOpDisposition
    {
        get; private set;
    }
    public string? PostOpInstructions
    {
        get; private set;
    }
    public bool IsSigned
    {
        get; private set;
    }
    public Guid? SignedByDoctorId
    {
        get; private set;
    }
    public DateTime? SignedAtUtc
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
    public uint Version
    {
        get; internal set;
    }

    public IReadOnlyCollection<PerioperativeCorrection> Corrections => _corrections.AsReadOnly();

    public static PerioperativeRecord Create(
        Guid id,
        Guid surgeryBookingId,
        Guid patientId,
        Guid operatingRoomId,
        AnesthesiaType anesthesiaType,
        PostOpDisposition postOpDisposition,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Kayıt ID boş olamaz.", nameof(id));
        }

        if (surgeryBookingId == Guid.Empty)
        {
            throw new ArgumentException("Ameliyat randevu ID boş olamaz.", nameof(surgeryBookingId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (operatingRoomId == Guid.Empty)
        {
            throw new ArgumentException("Ameliyathane salon ID boş olamaz.", nameof(operatingRoomId));
        }

        return new PerioperativeRecord
        {
            Id = id,
            SurgeryBookingId = surgeryBookingId,
            PatientId = patientId,
            OperatingRoomId = operatingRoomId,
            AnesthesiaType = anesthesiaType,
            PostOpDisposition = postOpDisposition,
            IsSigned = false,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void UpdateDraft(
        DateTime? roomEntryTimeUtc,
        DateTime? anesthesiaStartTimeUtc,
        DateTime? incisionTimeUtc,
        DateTime? closureTimeUtc,
        DateTime? anesthesiaEndTimeUtc,
        DateTime? roomExitTimeUtc,
        AnesthesiaType anesthesiaType,
        string? anesthesiaNotes,
        string? intraoperativeFindings,
        string? intraoperativeComplications,
        int? estimatedBloodLossMl,
        string? specimensCollected,
        bool countsConfirmed,
        PostOpDisposition postOpDisposition,
        string? postOpInstructions,
        DateTime nowUtc)
    {
        if (IsSigned)
        {
            throw new InvalidOperationException("İmzalı perioperatif kayıt doğrudan güncellenemez. Düzeltme / ek not ekleyiniz.");
        }

        ValidateTimeSequence(
            roomEntryTimeUtc,
            anesthesiaStartTimeUtc,
            incisionTimeUtc,
            closureTimeUtc,
            anesthesiaEndTimeUtc,
            roomExitTimeUtc);

        RoomEntryTimeUtc = roomEntryTimeUtc;
        AnesthesiaStartTimeUtc = anesthesiaStartTimeUtc;
        IncisionTimeUtc = incisionTimeUtc;
        ClosureTimeUtc = closureTimeUtc;
        AnesthesiaEndTimeUtc = anesthesiaEndTimeUtc;
        RoomExitTimeUtc = roomExitTimeUtc;
        AnesthesiaType = anesthesiaType;
        AnesthesiaNotes = string.IsNullOrWhiteSpace(anesthesiaNotes) ? null : anesthesiaNotes.Trim();
        IntraoperativeFindings = string.IsNullOrWhiteSpace(intraoperativeFindings) ? null : intraoperativeFindings.Trim();
        IntraoperativeComplications = string.IsNullOrWhiteSpace(intraoperativeComplications) ? null : intraoperativeComplications.Trim();
        EstimatedBloodLossMl = estimatedBloodLossMl >= 0 ? estimatedBloodLossMl : null;
        SpecimensCollected = string.IsNullOrWhiteSpace(specimensCollected) ? null : specimensCollected.Trim();
        CountsConfirmed = countsConfirmed;
        PostOpDisposition = postOpDisposition;
        PostOpInstructions = string.IsNullOrWhiteSpace(postOpInstructions) ? null : postOpInstructions.Trim();
        UpdatedAtUtc = nowUtc;
    }

    public void Sign(Guid doctorId, DateTime nowUtc)
    {
        if (IsSigned)
        {
            throw new InvalidOperationException("Bu kayıt zaten imzalanmıştır.");
        }

        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException("İmzalayan hekim ID boş olamaz.", nameof(doctorId));
        }

        ValidateTimeSequence(
            RoomEntryTimeUtc,
            AnesthesiaStartTimeUtc,
            IncisionTimeUtc,
            ClosureTimeUtc,
            AnesthesiaEndTimeUtc,
            RoomExitTimeUtc);

        IsSigned = true;
        SignedByDoctorId = doctorId;
        SignedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }

    public PerioperativeCorrection AddCorrection(
        Guid correctionId,
        Guid doctorId,
        string reason,
        string note,
        DateTime nowUtc)
    {
        if (!IsSigned)
        {
            throw new InvalidOperationException("Yalnızca imzalanmış kayıtlara düzeltme / ek not eklenebilir. Taslak kaydı doğrudan düzenleyebilirsiniz.");
        }

        var correction = PerioperativeCorrection.Create(
            correctionId,
            Id,
            doctorId,
            reason,
            note,
            nowUtc);

        _corrections.Add(correction);
        UpdatedAtUtc = nowUtc;
        return correction;
    }

    private static void ValidateTimeSequence(
        DateTime? roomEntry,
        DateTime? anesthesiaStart,
        DateTime? incision,
        DateTime? closure,
        DateTime? anesthesiaEnd,
        DateTime? roomExit)
    {
        if (roomEntry.HasValue && anesthesiaStart.HasValue && roomEntry.Value > anesthesiaStart.Value)
        {
            throw new ArgumentException("Salona giriş zamanı anestezi başlangıç zamanından sonra olamaz.", nameof(roomEntry));
        }

        if (anesthesiaStart.HasValue && incision.HasValue && anesthesiaStart.Value > incision.Value)
        {
            throw new ArgumentException("Anestezi başlangıç zamanı cerrahi kesi zamanından sonra olamaz.", nameof(anesthesiaStart));
        }

        if (incision.HasValue && closure.HasValue && incision.Value > closure.Value)
        {
            throw new ArgumentException("Cerrahi kesi zamanı kapatma zamanından sonra olamaz.", nameof(incision));
        }

        if (closure.HasValue && anesthesiaEnd.HasValue && closure.Value > anesthesiaEnd.Value)
        {
            throw new ArgumentException("Kapatma zamanı anestezi bitiş zamanından sonra olamaz.", nameof(closure));
        }

        if (anesthesiaEnd.HasValue && roomExit.HasValue && anesthesiaEnd.Value > roomExit.Value)
        {
            throw new ArgumentException("Anestezi bitiş zamanı salondan çıkış zamanından sonra olamaz.", nameof(anesthesiaEnd));
        }
    }
}
