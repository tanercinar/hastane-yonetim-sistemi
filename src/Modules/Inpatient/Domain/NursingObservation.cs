namespace HospitalManagement.Modules.Inpatient.Domain;

public enum ConsciousnessLevel
{
    Alert,
    Voice,
    Pain,
    Unresponsive,
}

public sealed class NursingObservation
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
    public Guid RecordedByNurseId
    {
        get; private set;
    }
    public DateTime ObservedAtUtc
    {
        get; private set;
    }

    // Vital Bulgular
    public int? SystolicBp
    {
        get; private set;
    }
    public int? DiastolicBp
    {
        get; private set;
    }
    public int? HeartRate
    {
        get; private set;
    }
    public int? RespiratoryRate
    {
        get; private set;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; private set;
    }
    public int? OxygenSaturationPercent
    {
        get; private set;
    }
    public int? PainScale
    {
        get; private set;
    } // 0-10

    // Sıvı Dengesi (Giriş / Çıkış)
    public int? OralIntakeMl
    {
        get; private set;
    }
    public int? IvIntakeMl
    {
        get; private set;
    }
    public int? UrineOutputMl
    {
        get; private set;
    }
    public int? DrainOutputMl
    {
        get; private set;
    }
    public int? OtherOutputMl
    {
        get; private set;
    }

    public ConsciousnessLevel Consciousness
    {
        get; private set;
    }
    public string? ClinicalNotes
    {
        get; private set;
    }

    // Düzeltme / Hata Takibi
    public bool IsCorrection
    {
        get; private set;
    }
    public Guid? CorrectedObservationId
    {
        get; private set;
    }
    public string? CorrectionReason
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

    private NursingObservation()
    {
    }

    public static NursingObservation Record(
        Guid id,
        Guid admissionId,
        Guid patientId,
        Guid recordedByNurseId,
        DateTime observedAtUtc,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        int? respiratoryRate,
        decimal? bodyTemperatureCelsius,
        int? oxygenSaturationPercent,
        int? painScale,
        int? oralIntakeMl,
        int? ivIntakeMl,
        int? urineOutputMl,
        int? drainOutputMl,
        int? otherOutputMl,
        ConsciousnessLevel consciousness,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Gözlem ID boş olamaz.", nameof(id));
        if (admissionId == Guid.Empty)
            throw new ArgumentException("Yatış ID boş olamaz.", nameof(admissionId));
        if (patientId == Guid.Empty)
            throw new ArgumentException("Hasta ID boş olamaz.", nameof(patientId));
        if (recordedByNurseId == Guid.Empty)
            throw new ArgumentException("Hemşire ID boş olamaz.", nameof(recordedByNurseId));

        if (systolicBp.HasValue && (systolicBp.Value < 30 || systolicBp.Value > 300))
        {
            throw new ArgumentOutOfRangeException(nameof(systolicBp), "Sistolik tansiyon geçerli aralıkta olmalıdır (30-300 mmHg).");
        }

        if (diastolicBp.HasValue && (diastolicBp.Value < 20 || diastolicBp.Value > 200))
        {
            throw new ArgumentOutOfRangeException(nameof(diastolicBp), "Diyastolik tansiyon geçerli aralıkta olmalıdır (20-200 mmHg).");
        }

        if (heartRate.HasValue && (heartRate.Value < 20 || heartRate.Value > 300))
        {
            throw new ArgumentOutOfRangeException(nameof(heartRate), "Nabız geçerli aralıkta olmalıdır (20-300 bpm).");
        }

        if (bodyTemperatureCelsius.HasValue && (bodyTemperatureCelsius.Value < 30m || bodyTemperatureCelsius.Value > 45m))
        {
            throw new ArgumentOutOfRangeException(nameof(bodyTemperatureCelsius), "Vücut sıcaklığı geçerli aralıkta olmalıdır (30-45 °C).");
        }

        if (oxygenSaturationPercent.HasValue && (oxygenSaturationPercent.Value < 50 || oxygenSaturationPercent.Value > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenSaturationPercent), "Oksijen satürasyonu geçerli aralıkta olmalıdır (%50-100).");
        }

        if (painScale.HasValue && (painScale.Value < 0 || painScale.Value > 10))
        {
            throw new ArgumentOutOfRangeException(nameof(painScale), "Ağrı skalası 0 ile 10 arasında olmalıdır.");
        }

        return new NursingObservation
        {
            Id = id,
            AdmissionId = admissionId,
            PatientId = patientId,
            RecordedByNurseId = recordedByNurseId,
            ObservedAtUtc = observedAtUtc,
            SystolicBp = systolicBp,
            DiastolicBp = diastolicBp,
            HeartRate = heartRate,
            RespiratoryRate = respiratoryRate,
            BodyTemperatureCelsius = bodyTemperatureCelsius,
            OxygenSaturationPercent = oxygenSaturationPercent,
            PainScale = painScale,
            OralIntakeMl = oralIntakeMl,
            IvIntakeMl = ivIntakeMl,
            UrineOutputMl = urineOutputMl,
            DrainOutputMl = drainOutputMl,
            OtherOutputMl = otherOutputMl,
            Consciousness = consciousness,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            IsCorrection = false,
            CorrectedObservationId = null,
            CorrectionReason = null,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public static NursingObservation CreateCorrection(
        Guid id,
        NursingObservation original,
        Guid recordedByNurseId,
        string correctionReason,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        int? respiratoryRate,
        decimal? bodyTemperatureCelsius,
        int? oxygenSaturationPercent,
        int? painScale,
        int? oralIntakeMl,
        int? ivIntakeMl,
        int? urineOutputMl,
        int? drainOutputMl,
        int? otherOutputMl,
        ConsciousnessLevel consciousness,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (string.IsNullOrWhiteSpace(correctionReason))
        {
            throw new ArgumentException("Düzeltme gerekçesi boş olamaz.", nameof(correctionReason));
        }

        var obs = Record(
            id,
            original.AdmissionId,
            original.PatientId,
            recordedByNurseId,
            original.ObservedAtUtc,
            systolicBp,
            diastolicBp,
            heartRate,
            respiratoryRate,
            bodyTemperatureCelsius,
            oxygenSaturationPercent,
            painScale,
            oralIntakeMl,
            ivIntakeMl,
            urineOutputMl,
            drainOutputMl,
            otherOutputMl,
            consciousness,
            clinicalNotes,
            nowUtc);

        obs.IsCorrection = true;
        obs.CorrectedObservationId = original.Id;
        obs.CorrectionReason = correctionReason.Trim();

        return obs;
    }
}
