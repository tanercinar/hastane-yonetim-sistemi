namespace HospitalManagement.Modules.ClinicalRecords.Domain;

public enum VitalSignType
{
    BodyTemperature = 1,
    BloodPressureSystolic = 2,
    BloodPressureDiastolic = 3,
    HeartRate = 4,
    RespiratoryRate = 5,
    OxygenSaturation = 6,
    BodyWeight = 7,
    BodyHeight = 8,
    BodyMassIndex = 9,
    BloodGlucose = 10,
    PainScore = 11,
}

public enum VitalInterpretation
{
    Normal = 1,
    Low = 2,
    High = 3,
    CriticalLow = 4,
    CriticalHigh = 5,
}
