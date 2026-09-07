namespace HospitalManagement.Modules.SurgeryCriticalCare.Domain;

public sealed class IcuFlowsheetEntry
{
    private IcuFlowsheetEntry()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid IcuAdmissionId
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

    // Vital Signs
    public int? HeartRateBpm
    {
        get; private set;
    }
    public int? SystolicBpMmHg
    {
        get; private set;
    }
    public int? DiastolicBpMmHg
    {
        get; private set;
    }
    public int? MeanArterialPressureMmHg
    {
        get; private set;
    }
    public int? RespiratoryRateBpm
    {
        get; private set;
    }
    public decimal? OxygenSaturationPct
    {
        get; private set;
    }
    public decimal? BodyTemperatureCelsius
    {
        get; private set;
    }

    // Clinical Scores
    public int? GlasgowComaScale
    {
        get; private set;
    } // 3 - 15
    public int? RichmondAgitationSedationScale
    {
        get; private set;
    } // -5 to +4

    // Ventilator Simulation Parameters
    public IcuVentilationMode VentilationMode
    {
        get; private set;
    }
    public int? FractionOfInspiredOxygenPct
    {
        get; private set;
    } // FiO2 21 - 100%
    public int? PositiveEndExpiratoryPressure
    {
        get; private set;
    } // PEEP cmH2O
    public int? TidalVolumeMl
    {
        get; private set;
    }
    public int? PeakInspiratoryPressure
    {
        get; private set;
    } // PIP cmH2O

    // Fluid Balance (Intake & Output in ml)
    public int? IvFluidIntakeMl
    {
        get; private set;
    }
    public int? EnteralNutritionIntakeMl
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

    public string? ClinicalNotes
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public int TotalIntakeMl => (IvFluidIntakeMl ?? 0) + (EnteralNutritionIntakeMl ?? 0);
    public int TotalOutputMl => (UrineOutputMl ?? 0) + (DrainOutputMl ?? 0);
    public int NetFluidBalanceMl => TotalIntakeMl - TotalOutputMl;

    public static IcuFlowsheetEntry Create(
        Guid id,
        Guid icuAdmissionId,
        DateTime recordedAtUtc,
        Guid recordedByStaffId,
        int? heartRateBpm,
        int? systolicBpMmHg,
        int? diastolicBpMmHg,
        int? respiratoryRateBpm,
        decimal? oxygenSaturationPct,
        decimal? bodyTemperatureCelsius,
        int? glasgowComaScale,
        int? richmondAgitationSedationScale,
        IcuVentilationMode ventilationMode,
        int? fractionOfInspiredOxygenPct,
        int? positiveEndExpiratoryPressure,
        int? tidalVolumeMl,
        int? peakInspiratoryPressure,
        int? ivFluidIntakeMl,
        int? enteralNutritionIntakeMl,
        int? urineOutputMl,
        int? drainOutputMl,
        string? clinicalNotes,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Kayıt ID boş olamaz.", nameof(id));
        }

        if (icuAdmissionId == Guid.Empty)
        {
            throw new ArgumentException("Yoğun bakım kabul ID boş olamaz.", nameof(icuAdmissionId));
        }

        if (recordedByStaffId == Guid.Empty)
        {
            throw new ArgumentException("Kaydeden personel ID boş olamaz.", nameof(recordedByStaffId));
        }

        if (heartRateBpm is < 20 or > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(heartRateBpm), "Nabız fizyolojik sınırlar dışında (20-300).");
        }

        if (systolicBpMmHg is < 30 or > 300)
        {
            throw new ArgumentOutOfRangeException(nameof(systolicBpMmHg), "Sistolik tansiyon fizyolojik sınırlar dışında (30-300).");
        }

        if (diastolicBpMmHg is < 10 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(diastolicBpMmHg), "Diyastolik tansiyon fizyolojik sınırlar dışında (10-200).");
        }

        if (oxygenSaturationPct is < 40 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenSaturationPct), "SpO2 geçerli yüzde aralığında olmalıdır (40-100).");
        }

        if (bodyTemperatureCelsius is < 25 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(bodyTemperatureCelsius), "Vücut sıcaklığı fizyolojik sınırlar dışında (25-45).");
        }

        if (glasgowComaScale is < 3 or > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(glasgowComaScale), "Glasgow Koma Skalası 3 ile 15 arasında olmalıdır.");
        }

        if (richmondAgitationSedationScale is < -5 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(richmondAgitationSedationScale), "RASS skoru -5 ile +4 arasında olmalıdır.");
        }

        if (fractionOfInspiredOxygenPct is < 21 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(fractionOfInspiredOxygenPct), "FiO2 %21 ile %100 arasında olmalıdır.");
        }

        int? map = null;
        if (systolicBpMmHg.HasValue && diastolicBpMmHg.HasValue)
        {
            // MAP = (2*Diastolic + Systolic) / 3
            map = ((2 * diastolicBpMmHg.Value) + systolicBpMmHg.Value) / 3;
        }

        return new IcuFlowsheetEntry
        {
            Id = id,
            IcuAdmissionId = icuAdmissionId,
            RecordedAtUtc = recordedAtUtc,
            RecordedByStaffId = recordedByStaffId,
            HeartRateBpm = heartRateBpm,
            SystolicBpMmHg = systolicBpMmHg,
            DiastolicBpMmHg = diastolicBpMmHg,
            MeanArterialPressureMmHg = map,
            RespiratoryRateBpm = respiratoryRateBpm,
            OxygenSaturationPct = oxygenSaturationPct,
            BodyTemperatureCelsius = bodyTemperatureCelsius,
            GlasgowComaScale = glasgowComaScale,
            RichmondAgitationSedationScale = richmondAgitationSedationScale,
            VentilationMode = ventilationMode,
            FractionOfInspiredOxygenPct = fractionOfInspiredOxygenPct,
            PositiveEndExpiratoryPressure = positiveEndExpiratoryPressure,
            TidalVolumeMl = tidalVolumeMl,
            PeakInspiratoryPressure = peakInspiratoryPressure,
            IvFluidIntakeMl = ivFluidIntakeMl,
            EnteralNutritionIntakeMl = enteralNutritionIntakeMl,
            UrineOutputMl = urineOutputMl,
            DrainOutputMl = drainOutputMl,
            ClinicalNotes = string.IsNullOrWhiteSpace(clinicalNotes) ? null : clinicalNotes.Trim(),
            CreatedAtUtc = nowUtc,
        };
    }
}
