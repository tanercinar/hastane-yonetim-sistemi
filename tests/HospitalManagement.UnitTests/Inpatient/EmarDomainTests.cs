using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class EmarDomainTests
{
    [Fact]
    public void ScheduleMedicationWithValidParametersCreatesScheduledMedication()
    {
        var id = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var scheduledTime = now.AddHours(2);

        var med = MedicationAdministration.Schedule(
            id,
            admissionId,
            patientId,
            null,
            "DEMO-Parasetamol 500mg Tablet",
            "500 mg",
            "Oral",
            scheduledTime,
            now);

        Assert.Equal(id, med.Id);
        Assert.Equal(admissionId, med.AdmissionId);
        Assert.Equal(patientId, med.PatientId);
        Assert.Equal("DEMO-Parasetamol 500mg Tablet", med.MedicationName);
        Assert.Equal("500 mg", med.Dose);
        Assert.Equal("Oral", med.Route);
        Assert.Equal(scheduledTime, med.ScheduledTimeUtc);
        Assert.Equal(MedicationAdministrationStatus.Scheduled, med.Status);
        Assert.False(med.Verified5Rights);
        Assert.Null(med.AdministeredByNurseId);
    }

    [Fact]
    public void ScheduleMedicationWithEmptyMedicationNameThrowsArgumentException()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            MedicationAdministration.Schedule(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "",
                "500 mg",
                "Oral",
                now.AddHours(2),
                now));
    }

    [Fact]
    public void AdministerWith5RightsSetsStatusToAdministered()
    {
        var now = DateTime.UtcNow;
        var nurseId = Guid.NewGuid();

        var med = MedicationAdministration.Schedule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "DEMO-Seftriakson 1g Flakon",
            "1 g",
            "IV",
            now.AddHours(1),
            now);

        med.Administer(nurseId, now.AddHours(1), true, "IV yavaş infüzyon yapıldı.");

        Assert.Equal(MedicationAdministrationStatus.Administered, med.Status);
        Assert.Equal(nurseId, med.AdministeredByNurseId);
        Assert.True(med.Verified5Rights);
        Assert.Equal("IV yavaş infüzyon yapıldı.", med.Notes);
    }

    [Fact]
    public void AdministerWithout5RightsThrowsInvalidOperationException()
    {
        var now = DateTime.UtcNow;
        var nurseId = Guid.NewGuid();

        var med = MedicationAdministration.Schedule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "DEMO-Seftriakson 1g Flakon",
            "1 g",
            "IV",
            now.AddHours(1),
            now);

        Assert.Throws<InvalidOperationException>(() =>
            med.Administer(nurseId, now.AddHours(1), false, "Not"));
    }

    [Fact]
    public void SkipMedicationSetsStatusToSkippedWithReason()
    {
        var now = DateTime.UtcNow;
        var nurseId = Guid.NewGuid();

        var med = MedicationAdministration.Schedule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "DEMO-Aspirin 100mg",
            "100 mg",
            "Oral",
            now.AddHours(1),
            now);

        med.Skip(nurseId, "Hasta operasyona gideceği için atlandı.", now);

        Assert.Equal(MedicationAdministrationStatus.Skipped, med.Status);
        Assert.Equal("Hasta operasyona gideceği için atlandı.", med.Reason);
        Assert.Equal(nurseId, med.AdministeredByNurseId);
    }

    [Fact]
    public void RefuseMedicationSetsStatusToRefusedWithReason()
    {
        var now = DateTime.UtcNow;
        var nurseId = Guid.NewGuid();

        var med = MedicationAdministration.Schedule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "DEMO-Aspirin 100mg",
            "100 mg",
            "Oral",
            now.AddHours(1),
            now);

        med.Refuse(nurseId, "Hasta ilacı mide yanması sebebiyle almak istemedi.", now);

        Assert.Equal(MedicationAdministrationStatus.Refused, med.Status);
        Assert.Equal("Hasta ilacı mide yanması sebebiyle almak istemedi.", med.Reason);
        Assert.Equal(nurseId, med.AdministeredByNurseId);
    }

    [Fact]
    public void DelayMedicationUpdatesScheduledTimeAndSetsDelayedStatus()
    {
        var now = DateTime.UtcNow;
        var nurseId = Guid.NewGuid();
        var originalTime = now.AddHours(1);
        var delayedTime = now.AddHours(4);

        var med = MedicationAdministration.Schedule(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "DEMO-Aspirin 100mg",
            "100 mg",
            "Oral",
            originalTime,
            now);

        med.Delay(nurseId, delayedTime, "Hasta MR çekiminde.", now);

        Assert.Equal(MedicationAdministrationStatus.Delayed, med.Status);
        Assert.Equal(delayedTime, med.ScheduledTimeUtc);
        Assert.Equal("Hasta MR çekiminde.", med.Reason);
    }
}
