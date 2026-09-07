namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public static class VitalSignValidationRules
{
    public static (bool IsValid, string? ErrorMessage) Validate(VitalSignType type, decimal value, string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            return (false, "Ölçüm birimi boş olamaz.");
        }

        return type switch
        {
            VitalSignType.BodyTemperature => ValidateRange(value, 25.0m, 45.0m, "Vücut sıcaklığı 25.0°C ile 45.0°C arasında olmalıdır."),
            VitalSignType.BloodPressureSystolic => ValidateRange(value, 40m, 300m, "Sistolik tansiyon 40 ile 300 mmHg arasında olmalıdır."),
            VitalSignType.BloodPressureDiastolic => ValidateRange(value, 20m, 200m, "Diyastolik tansiyon 20 ile 200 mmHg arasında olmalıdır."),
            VitalSignType.HeartRate => ValidateRange(value, 20m, 300m, "Nabız (kalp hızı) 20 ile 300 bpm arasında olmalıdır."),
            VitalSignType.RespiratoryRate => ValidateRange(value, 4m, 80m, "Solunum sayısı 4 ile 80 /dk arasında olmalıdır."),
            VitalSignType.OxygenSaturation => ValidateRange(value, 40m, 100m, "Oksijen satürasyonu %40 ile %100 arasında olmalıdır."),
            VitalSignType.BodyWeight => ValidateRange(value, 0.2m, 500m, "Vücut ağırlığı 0.2 ile 500 kg arasında olmalıdır."),
            VitalSignType.BodyHeight => ValidateRange(value, 20m, 260m, "Boy uzunluğu 20 ile 260 cm arasında olmalıdır."),
            VitalSignType.BodyMassIndex => ValidateRange(value, 5m, 100m, "Vücut kitle indeksi (BMI) 5 ile 100 arasında olmalıdır."),
            VitalSignType.BloodGlucose => ValidateRange(value, 10m, 1200m, "Kan şekeri 10 ile 1200 mg/dL arasında olmalıdır."),
            VitalSignType.PainScore => ValidateRange(value, 0m, 10m, "Ağrı skoru 0 ile 10 arasında olmalıdır."),
            _ => (true, null),
        };
    }

    public static VitalInterpretation DetermineInterpretation(VitalSignType type, decimal value)
    {
        return type switch
        {
            VitalSignType.BodyTemperature => value switch
            {
                < 35.0m => VitalInterpretation.CriticalLow,
                < 36.0m => VitalInterpretation.Low,
                <= 37.5m => VitalInterpretation.Normal,
                <= 38.5m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.BloodPressureSystolic => value switch
            {
                < 70m => VitalInterpretation.CriticalLow,
                < 90m => VitalInterpretation.Low,
                <= 120m => VitalInterpretation.Normal,
                <= 140m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.BloodPressureDiastolic => value switch
            {
                < 50m => VitalInterpretation.CriticalLow,
                < 60m => VitalInterpretation.Low,
                <= 80m => VitalInterpretation.Normal,
                <= 90m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.HeartRate => value switch
            {
                < 50m => VitalInterpretation.CriticalLow,
                < 60m => VitalInterpretation.Low,
                <= 100m => VitalInterpretation.Normal,
                <= 130m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.RespiratoryRate => value switch
            {
                < 8m => VitalInterpretation.CriticalLow,
                < 12m => VitalInterpretation.Low,
                <= 20m => VitalInterpretation.Normal,
                <= 30m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.OxygenSaturation => value switch
            {
                < 90m => VitalInterpretation.CriticalLow,
                < 95m => VitalInterpretation.Low,
                _ => VitalInterpretation.Normal,
            },

            VitalSignType.BloodGlucose => value switch
            {
                < 54m => VitalInterpretation.CriticalLow,
                < 70m => VitalInterpretation.Low,
                <= 140m => VitalInterpretation.Normal,
                <= 250m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            VitalSignType.PainScore => value switch
            {
                <= 3m => VitalInterpretation.Normal,
                <= 7m => VitalInterpretation.High,
                _ => VitalInterpretation.CriticalHigh,
            },

            _ => VitalInterpretation.Normal,
        };
    }

    private static (bool IsValid, string? ErrorMessage) ValidateRange(
        decimal value,
        decimal min,
        decimal max,
        string message)
    {
        if (value < min || value > max)
        {
            return (false, message);
        }

        return (true, null);
    }
}
