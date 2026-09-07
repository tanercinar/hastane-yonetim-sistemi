namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class PerioperativeCorrection
{
    private PerioperativeCorrection()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid PerioperativeRecordId
    {
        get; private set;
    }
    public Guid CorrectedByDoctorId
    {
        get; private set;
    }
    public DateTime CorrectedAtUtc
    {
        get; private set;
    }
    public string ReasonForCorrection { get; private set; } = string.Empty;
    public string CorrectionNote { get; private set; } = string.Empty;

    public static PerioperativeCorrection Create(
        Guid id,
        Guid perioperativeRecordId,
        Guid correctedByDoctorId,
        string reasonForCorrection,
        string correctionNote,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Düzeltme ID boş olamaz.", nameof(id));
        }

        if (perioperativeRecordId == Guid.Empty)
        {
            throw new ArgumentException("Perioperatif kayıt ID boş olamaz.", nameof(perioperativeRecordId));
        }

        if (correctedByDoctorId == Guid.Empty)
        {
            throw new ArgumentException("Düzeltmeyi yapan hekim ID boş olamaz.", nameof(correctedByDoctorId));
        }

        if (string.IsNullOrWhiteSpace(reasonForCorrection))
        {
            throw new ArgumentException("Düzeltme gerekçesi boş olamaz.", nameof(reasonForCorrection));
        }

        if (string.IsNullOrWhiteSpace(correctionNote))
        {
            throw new ArgumentException("Düzeltme notu boş olamaz.", nameof(correctionNote));
        }

        return new PerioperativeCorrection
        {
            Id = id,
            PerioperativeRecordId = perioperativeRecordId,
            CorrectedByDoctorId = correctedByDoctorId,
            ReasonForCorrection = reasonForCorrection.Trim(),
            CorrectionNote = correctionNote.Trim(),
            CorrectedAtUtc = nowUtc,
        };
    }
}
