namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

public sealed class DentalExaminationRecord
{
    private DentalExaminationRecord()
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
    public string ExaminationProtocolNumber { get; private set; } = string.Empty;

    public Guid DentistId
    {
        get; private set;
    }
    public DateTime ExaminationDateUtc
    {
        get; private set;
    }
    public string? ChiefComplaint
    {
        get; private set;
    }
    public string? DiagnosisNotes
    {
        get; private set;
    }
    public string? TreatmentPlanSummary
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public static DentalExaminationRecord Create(
        Guid id,
        Guid patientId,
        Guid? encounterId,
        Guid dentistId,
        DateTime examinationDateUtc,
        string? chiefComplaint,
        string? diagnosisNotes,
        string? treatmentPlanSummary,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Muayene ID boş olamaz.", nameof(id));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        }

        if (dentistId == Guid.Empty)
        {
            throw new ArgumentException("Diş hekimi ID boş olamaz.", nameof(dentistId));
        }

        var protocol = $"DEMO-DEN-{nowUtc:yyyyMMdd}-{id.ToString("N")[..6].ToUpperInvariant()}";

        return new DentalExaminationRecord
        {
            Id = id,
            PatientId = patientId,
            EncounterId = encounterId,
            ExaminationProtocolNumber = protocol,
            DentistId = dentistId,
            ExaminationDateUtc = examinationDateUtc == default ? nowUtc : examinationDateUtc,
            ChiefComplaint = string.IsNullOrWhiteSpace(chiefComplaint) ? null : chiefComplaint.Trim(),
            DiagnosisNotes = string.IsNullOrWhiteSpace(diagnosisNotes) ? null : diagnosisNotes.Trim(),
            TreatmentPlanSummary = string.IsNullOrWhiteSpace(treatmentPlanSummary) ? null : treatmentPlanSummary.Trim(),
            CreatedAtUtc = nowUtc,
        };
    }
}
