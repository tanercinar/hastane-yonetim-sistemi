using HospitalManagement.Modules.Emergency.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Emergency;

public sealed class EmergencyAdmissionDomainTests
{
    private static readonly DateTime NowUtc = new(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid StaffId = Guid.NewGuid();
    private static readonly Guid NurseId = Guid.NewGuid();
    private static readonly Guid DoctorId = Guid.NewGuid();

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G01")]
    public void CreateAdmissionInitializesWithWaitingTriageAndValidProtocol()
    {
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            PatientId,
            EmergencyArrivalType.WalkIn,
            "Şiddetli baş ağrısı ve baş dönmesi",
            "Hasta acile ayaktan başvurdu",
            StaffId,
            NowUtc);

        Assert.Equal(EmergencyAdmissionStatus.WaitingTriage, admission.Status);
        Assert.Equal(PatientId, admission.PatientId);
        Assert.Equal(EmergencyArrivalType.WalkIn, admission.ArrivalType);
        Assert.Equal("Şiddetli baş ağrısı ve baş dönmesi", admission.ChiefComplaint);
        Assert.StartsWith("DEMO-EMG-20260831-", admission.EmergencyProtocolNumber, StringComparison.Ordinal);
        Assert.Null(admission.Triage);
        Assert.Null(admission.AssignedDoctorId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G01")]
    public void CreateAdmissionThrowsWhenChiefComplaintIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => EmergencyAdmission.Create(
            Guid.NewGuid(),
            PatientId,
            EmergencyArrivalType.WalkIn,
            string.Empty,
            null,
            StaffId,
            NowUtc));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G01")]
    public void RecordTriageSetsEducationalFlagAndTransitionsStatus()
    {
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            PatientId,
            EmergencyArrivalType.Ambulance,
            "Akut dispne ve göğüs ağrısı",
            null,
            StaffId,
            NowUtc);

        admission.RecordTriage(
            TriageLevel.YellowUrgent,
            "Taşikardi ve hafif hipoksi (SpO2 %93), acil muayene gerekir.",
            NurseId,
            systolicBp: 135,
            diastolicBp: 85,
            heartRate: 108,
            bodyTemperatureCelsius: 37.2m,
            respiratoryRate: 22,
            oxygenSaturationPercent: 93,
            painScale: 5,
            consciousness: "Alert",
            clinicalNotes: "Oksijen 2L/dk takıldı.",
            NowUtc.AddMinutes(5));

        Assert.Equal(EmergencyAdmissionStatus.TriagedWaitingDoctor, admission.Status);
        Assert.NotNull(admission.Triage);
        Assert.Equal(TriageLevel.YellowUrgent, admission.Triage.TriageLevel);
        Assert.True(admission.Triage.EducationalClassificationAssisted);
        Assert.Equal(108, admission.Triage.HeartRate);
        Assert.Equal(93, admission.Triage.OxygenSaturationPercent);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G01")]
    public void AssignDoctorTransitionsToInEvaluation()
    {
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            PatientId,
            EmergencyArrivalType.WalkIn,
            "Travma / Burkulma",
            null,
            StaffId,
            NowUtc);

        admission.RecordTriage(
            TriageLevel.GreenStandard,
            "Hayati tehlike yok, stabil.",
            NurseId,
            120, 80, 75, 36.5m, 16, 98, 3, "Alert", null,
            NowUtc.AddMinutes(5));

        admission.AssignDoctor(DoctorId, NowUtc.AddMinutes(10));
        admission.AssignBedOrZone("Yeşil Alan - Muayene 1", NowUtc.AddMinutes(10));

        Assert.Equal(EmergencyAdmissionStatus.InEvaluation, admission.Status);
        Assert.Equal(DoctorId, admission.AssignedDoctorId);
        Assert.Equal("Yeşil Alan - Muayene 1", admission.AssignedBedOrZone);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F08-G01")]
    public void CompletedAdmissionCannotRecordTriageOrAssignDoctor()
    {
        var admission = EmergencyAdmission.Create(
            Guid.NewGuid(),
            PatientId,
            EmergencyArrivalType.WalkIn,
            "Alerjik reaksiyon",
            null,
            StaffId,
            NowUtc);

        admission.UpdateStatus(EmergencyAdmissionStatus.Discharged, "Şifa ile taburcu", NowUtc.AddHours(2));

        Assert.Throws<InvalidOperationException>(() => admission.RecordTriage(
            TriageLevel.GreenStandard,
            "Gecikmiş triyaj",
            NurseId,
            null, null, null, null, null, null, null, null, null,
            NowUtc.AddHours(3)));

        Assert.Throws<InvalidOperationException>(() => admission.AssignDoctor(DoctorId, NowUtc.AddHours(3)));
    }
}
