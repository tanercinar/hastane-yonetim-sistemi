namespace HospitalManagement.Modules.Inpatient.Domain;

public enum MedicationAdministrationStatus
{
    Scheduled,
    Administered,
    Skipped,
    Refused,
    Delayed,
}

public sealed class MedicationAdministration
{
    public Guid Id
    {
        get; private set;
    }
    public Guid AdmissionId
    {
        get; private set;
    }
    public Guid PatientId
    {
        get; private set;
    }
    public Guid? PrescriptionId
    {
        get; private set;
    }
    public string MedicationName { get; private set; } = string.Empty;
    public string Dose { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty; // Oral, IV, IM, SC, Inhalation, Topical
    public DateTime ScheduledTimeUtc
    {
        get; private set;
    }
    public MedicationAdministrationStatus Status
    {
        get; private set;
    }
    public Guid? AdministeredByNurseId
    {
        get; private set;
    }
    public DateTime? AdministeredAtUtc
    {
        get; private set;
    }
    public bool Verified5Rights
    {
        get; private set;
    }
    public string? Reason
    {
        get; private set;
    }
    public string? Notes
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public int Version
    {
        get; private set;
    }

    private MedicationAdministration()
    {
    }

    public static MedicationAdministration Schedule(
        Guid id,
        Guid admissionId,
        Guid patientId,
        Guid? prescriptionId,
        string medicationName,
        string dose,
        string route,
        DateTime scheduledTimeUtc,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Uygulama ID boş olamaz.", nameof(id));
        if (admissionId == Guid.Empty)
            throw new ArgumentException("Yatış ID boş olamaz.", nameof(admissionId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        if (string.IsNullOrWhiteSpace(medicationName))
            throw new ArgumentException("İlaç adı boş olamaz.", nameof(medicationName));
        if (string.IsNullOrWhiteSpace(dose))
            throw new ArgumentException("Doz boş olamaz.", nameof(dose));
        if (string.IsNullOrWhiteSpace(route))
            throw new ArgumentException("Uygulama yolu boş olamaz.", nameof(route));

        return new MedicationAdministration
        {
            Id = id,
            AdmissionId = admissionId,
            PatientId = patientId,
            PrescriptionId = prescriptionId,
            MedicationName = medicationName.Trim(),
            Dose = dose.Trim(),
            Route = route.Trim(),
            ScheduledTimeUtc = scheduledTimeUtc,
            Status = MedicationAdministrationStatus.Scheduled,
            Verified5Rights = false,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Administer(
        Guid nurseId,
        DateTime administeredAtUtc,
        bool verified5Rights,
        string? notes)
    {
        if (Status == MedicationAdministrationStatus.Administered)
        {
            throw new InvalidOperationException("Bu doz zaten uygulanmış olarak kaydedilmiş.");
        }

        if (nurseId == Guid.Empty)
        {
            throw new ArgumentException("Uygulayan hemşire ID boş olamaz.", nameof(nurseId));
        }

        if (!verified5Rights)
        {
            throw new InvalidOperationException("İlaç uygulamadan önce 5 Doğru Kuralı (Hasta, İlaç, Doz, Zaman, Yol) doğrulanmalıdır.");
        }

        Status = MedicationAdministrationStatus.Administered;
        AdministeredByNurseId = nurseId;
        AdministeredAtUtc = administeredAtUtc;
        Verified5Rights = true;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Version++;
    }

    public void Skip(Guid nurseId, string reason, DateTime nowUtc)
    {
        if (Status == MedicationAdministrationStatus.Administered)
        {
            throw new InvalidOperationException("Uygulanmış olan bir doz atlanamaz.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("İlacın atlanma gerekçesi boş olamaz.", nameof(reason));
        }

        Status = MedicationAdministrationStatus.Skipped;
        AdministeredByNurseId = nurseId;
        AdministeredAtUtc = nowUtc;
        Reason = reason.Trim();
        Version++;
    }

    public void Refuse(Guid nurseId, string reason, DateTime nowUtc)
    {
        if (Status == MedicationAdministrationStatus.Administered)
        {
            throw new InvalidOperationException("Uygulanmış olan bir doz reddedilemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Hastanın ilacı reddetme gerekçesi belirtilmelidir.", nameof(reason));
        }

        Status = MedicationAdministrationStatus.Refused;
        AdministeredByNurseId = nurseId;
        AdministeredAtUtc = nowUtc;
        Reason = reason.Trim();
        Version++;
    }

    public void Delay(Guid nurseId, DateTime newScheduledTimeUtc, string reason, DateTime nowUtc)
    {
        if (Status == MedicationAdministrationStatus.Administered)
        {
            throw new InvalidOperationException("Uygulanmış olan bir doz ertelenemez.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Erteleme gerekçesi belirtilmelidir.", nameof(reason));
        }

        if (newScheduledTimeUtc <= nowUtc)
        {
            throw new ArgumentException("Yeni planlanan zaman gelecekte olmalıdır.", nameof(newScheduledTimeUtc));
        }

        Status = MedicationAdministrationStatus.Delayed;
        ScheduledTimeUtc = newScheduledTimeUtc;
        AdministeredByNurseId = nurseId;
        Reason = reason.Trim();
        Version++;
    }
}
