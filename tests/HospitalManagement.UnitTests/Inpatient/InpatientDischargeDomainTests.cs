using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class InpatientDischargeDomainTests
{
    [Fact]
    public void CreateDischargeWithValidParametersCreatesDischarge()
    {
        var id = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var discharge = InpatientDischarge.Create(
            id,
            admissionId,
            patientId,
            doctorId,
            DischargeType.Home,
            "Hasta yatış süresince takip edilmiş olup klinik stabilite sağlanarak taburcu edilmiştir.",
            "I25.1",
            "Aterosklerotik Kalp Hastalığı",
            "Düşük tuzlu diyet ve istirahat.",
            "DEMO-Aspirin 100mg 1x1",
            now.AddDays(7),
            null,
            null,
            null,
            now,
            now);

        Assert.Equal(id, discharge.Id);
        Assert.Equal(admissionId, discharge.AdmissionId);
        Assert.Equal(patientId, discharge.PatientId);
        Assert.Equal(doctorId, discharge.DischargingDoctorId);
        Assert.Equal(DischargeType.Home, discharge.DischargeType);
        Assert.Equal("I25.1", discharge.FinalDiagnosisCode);
        Assert.Equal("Aterosklerotik Kalp Hastalığı", discharge.FinalDiagnosisDescription);
        Assert.Null(discharge.TransferFacilityName);
    }

    [Fact]
    public void CreateDischargeWithShortSummaryThrowsArgumentException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            InpatientDischarge.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DischargeType.Home,
                "Kısa özet",
                "I25.1",
                "Aterosklerotik Kalp Hastalığı",
                "Öneriler",
                null,
                null,
                null,
                null,
                null,
                now,
                now));
    }

    [Fact]
    public void CreateExternalTransferWithoutFacilityNameThrowsArgumentException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            InpatientDischarge.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DischargeType.TransferToOtherFacility,
                "Hasta ileri tetkik ve tedavi amacıyla sevk edilmiştir.",
                "I25.1",
                "Aterosklerotik Kalp Hastalığı",
                "Sevk",
                null,
                null,
                null,
                "",
                "İleri tetkik",
                now,
                now));
    }

    [Fact]
    public void AdmissionDischargeSetsStatusAndSummary()
    {
        var now = DateTime.UtcNow;
        var admission = InpatientAdmission.Request(
            Guid.NewGuid(),
            "DEMO-ADM-20260830-100400",
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Akut Koroner Sendrom",
            "I25.1",
            "Aterosklerotik Kalp Hastalığı",
            "LowSodium",
            50,
            IsolationType.None,
            4,
            now);

        admission.Accept(Guid.NewGuid(), now);
        admission.Admit(Guid.NewGuid(), now);

        admission.Discharge("Hasta taburculuk kriterlerini karşılamaktadır.", now);

        Assert.Equal(AdmissionStatus.Discharged, admission.Status);
        Assert.Equal("Hasta taburculuk kriterlerini karşılamaktadır.", admission.DischargeSummary);
        Assert.Equal(now, admission.DischargedAtUtc);
    }
}
