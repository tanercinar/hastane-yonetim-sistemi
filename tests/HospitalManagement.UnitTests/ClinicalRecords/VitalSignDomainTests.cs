using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class VitalSignDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G03")]
    public void CreateVitalSignInitializesPropertiesAndCalculatesInterpretation()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var observation = VitalSignObservation.Create(
            id,
            patientId,
            null,
            VitalSignType.BodyTemperature,
            38.8m,
            "°C",
            "Kulaktan Dijital Termometre",
            nowUtc,
            "Ateş yüksekliği gözlendi",
            nurseId,
            nowUtc);

        Assert.Equal(id, observation.Id);
        Assert.Equal(patientId, observation.PatientId);
        Assert.Equal(VitalSignType.BodyTemperature, observation.MeasurementType);
        Assert.Equal(38.8m, observation.Value);
        Assert.Equal("°C", observation.Unit);
        Assert.Equal(VitalInterpretation.CriticalHigh, observation.Interpretation);
        Assert.Equal("Kulaktan Dijital Termometre", observation.MeasurementMethod);
        Assert.False(observation.IsEnteredInError);
        Assert.Null(observation.EnteredInErrorReason);
        Assert.Equal(1, observation.Version);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G03")]
    [InlineData(VitalSignType.BodyTemperature, 20.0, "°C")]
    [InlineData(VitalSignType.BodyTemperature, 55.0, "°C")]
    [InlineData(VitalSignType.HeartRate, 10.0, "bpm")]
    [InlineData(VitalSignType.HeartRate, 400.0, "bpm")]
    [InlineData(VitalSignType.BloodPressureSystolic, 20.0, "mmHg")]
    [InlineData(VitalSignType.BloodPressureSystolic, 400.0, "mmHg")]
    [InlineData(VitalSignType.OxygenSaturation, 30.0, "%")]
    [InlineData(VitalSignType.RespiratoryRate, 2.0, "/dk")]
    public void OutOfRangeValuesThrowArgumentOutOfRangeException(VitalSignType type, decimal value, string unit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VitalSignObservation.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                type,
                value,
                unit,
                null,
                DateTime.UtcNow,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G03")]
    [InlineData(VitalSignType.HeartRate, 45, VitalInterpretation.CriticalLow)]
    [InlineData(VitalSignType.HeartRate, 55, VitalInterpretation.Low)]
    [InlineData(VitalSignType.HeartRate, 75, VitalInterpretation.Normal)]
    [InlineData(VitalSignType.HeartRate, 115, VitalInterpretation.High)]
    [InlineData(VitalSignType.HeartRate, 150, VitalInterpretation.CriticalHigh)]
    [InlineData(VitalSignType.OxygenSaturation, 88, VitalInterpretation.CriticalLow)]
    [InlineData(VitalSignType.OxygenSaturation, 92, VitalInterpretation.Low)]
    [InlineData(VitalSignType.OxygenSaturation, 98, VitalInterpretation.Normal)]
    public void DetermineInterpretationMapsCorrectClinicalSeverity(
        VitalSignType type,
        decimal value,
        VitalInterpretation expected)
    {
        var result = VitalSignValidationRules.DetermineInterpretation(type, value);
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G03")]
    public void MarkEnteredInErrorSetsFlagAndReasonAndIncrementsVersion()
    {
        var observation = VitalSignObservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            VitalSignType.HeartRate,
            78m,
            "bpm",
            null,
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);

        var nowUtc = DateTime.UtcNow;
        observation.MarkEnteredInError(Guid.NewGuid(), "Yanlış prob bağlantısı", nowUtc);

        Assert.True(observation.IsEnteredInError);
        Assert.Equal("Yanlış prob bağlantısı", observation.EnteredInErrorReason);
        Assert.Equal(2, observation.Version);
        Assert.Equal(nowUtc, observation.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G03")]
    public void MarkEnteredInErrorTwiceThrowsInvalidOperationException()
    {
        var observation = VitalSignObservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            VitalSignType.HeartRate,
            78m,
            "bpm",
            null,
            DateTime.UtcNow,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);

        observation.MarkEnteredInError(Guid.NewGuid(), "İlk hata", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            observation.MarkEnteredInError(Guid.NewGuid(), "İkinci hata", DateTime.UtcNow));
    }
}
