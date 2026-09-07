using HospitalManagement.Modules.Inpatient.Domain;
using Xunit;

namespace HospitalManagement.UnitTests.Inpatient;

public sealed class NursingCareDomainTests
{
    [Fact]
    public void RecordObservationWithValidVitalsCreatesCorrectObservation()
    {
        var id = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var obs = NursingObservation.Record(
            id,
            admissionId,
            patientId,
            nurseId,
            now,
            120,
            80,
            72,
            16,
            36.6m,
            98,
            2,
            300,
            500,
            400,
            0,
            0,
            ConsciousnessLevel.Alert,
            "Stabil seyrediyor",
            now);

        Assert.Equal(id, obs.Id);
        Assert.Equal(admissionId, obs.AdmissionId);
        Assert.Equal(patientId, obs.PatientId);
        Assert.Equal(120, obs.SystolicBp);
        Assert.Equal(80, obs.DiastolicBp);
        Assert.Equal(72, obs.HeartRate);
        Assert.Equal(36.6m, obs.BodyTemperatureCelsius);
        Assert.Equal(98, obs.OxygenSaturationPercent);
        Assert.Equal(2, obs.PainScale);
        Assert.False(obs.IsCorrection);
        Assert.Null(obs.CorrectedObservationId);
    }

    [Theory]
    [InlineData(20, 80)]   // Systolic too low
    [InlineData(350, 80)]  // Systolic too high
    [InlineData(120, 10)]  // Diastolic too low
    [InlineData(120, 220)] // Diastolic too high
    public void RecordObservationWithInvalidBloodPressureThrowsArgumentOutOfRangeException(int systolic, int diastolic)
    {
        var now = DateTime.UtcNow;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NursingObservation.Record(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                now,
                systolic,
                diastolic,
                70,
                16,
                36.5m,
                98,
                0,
                null, null, null, null, null,
                ConsciousnessLevel.Alert,
                null,
                now));
    }

    [Fact]
    public void CreateCorrectionWithReasonSetsIsCorrectionAndLinksOriginal()
    {
        var now = DateTime.UtcNow;
        var original = NursingObservation.Record(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(-1),
            120,
            80,
            72,
            16,
            36.6m,
            98,
            2,
            null, null, null, null, null,
            ConsciousnessLevel.Alert,
            null,
            now.AddHours(-1));

        var corrId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();
        var correction = NursingObservation.CreateCorrection(
            corrId,
            original,
            nurseId,
            "Sistolik tansiyon tekrar ölçüldü.",
            130,
            85,
            75,
            16,
            36.7m,
            98,
            1,
            null, null, null, null, null,
            ConsciousnessLevel.Alert,
            "Düzeltildi",
            now);

        Assert.True(correction.IsCorrection);
        Assert.Equal(original.Id, correction.CorrectedObservationId);
        Assert.Equal("Sistolik tansiyon tekrar ölçüldü.", correction.CorrectionReason);
        Assert.Equal(130, correction.SystolicBp);
        Assert.Equal(85, correction.DiastolicBp);
    }

    [Fact]
    public void NursingCarePlanCreateAndAddTaskMaintainsTaskHierarchy()
    {
        var planId = Guid.NewGuid();
        var admissionId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var plan = NursingCarePlan.Create(
            planId,
            admissionId,
            patientId,
            nurseId,
            "Düşme Riski",
            "Hasta yatış süresince düşme yaşamayacak.",
            now);

        Assert.Equal(CarePlanStatus.Active, plan.Status);
        Assert.Empty(plan.Tasks);

        var taskId = Guid.NewGuid();
        var task = plan.AddTask(taskId, "Yatak kenarlıkları kaldırılacak", "Q4H", now.AddHours(4), now);

        Assert.Single(plan.Tasks);
        Assert.Equal(taskId, task.Id);
        Assert.Equal(CareTaskStatus.Pending, task.Status);
        Assert.Equal("Q4H", task.Frequency);
    }

    [Fact]
    public void NursingCareTaskCompleteUpdatesStatusAndAudits()
    {
        var now = DateTime.UtcNow;
        var plan = NursingCarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ağrı", "Ağrı skorunu 3 altına düşür", now);
        var task = plan.AddTask(Guid.NewGuid(), "Analjezik sonrası ağrı skoru değerlendir", "PRN", now.AddMinutes(30), now);

        var nurseId = Guid.NewGuid();
        task.Complete(nurseId, "Ağrı skoru 2'ye geriledi.", now.AddMinutes(30));

        Assert.Equal(CareTaskStatus.Completed, task.Status);
        Assert.Equal(nurseId, task.CompletedByNurseId);
        Assert.Equal("Ağrı skoru 2'ye geriledi.", task.CompletionNotes);
        Assert.Equal(now.AddMinutes(30), task.CompletedAtUtc);
    }

    [Fact]
    public void NursingCareTaskCancelUpdatesStatusAndReason()
    {
        var now = DateTime.UtcNow;
        var plan = NursingCarePlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ödem", "Ödem takibi", now);
        var task = plan.AddTask(Guid.NewGuid(), "Çevre ölçümü", "Daily", now.AddDays(1), now);

        var nurseId = Guid.NewGuid();
        task.Cancel(nurseId, "Hasta taburcu oldu, takip gerekmiyor.", now.AddHours(2));

        Assert.Equal(CareTaskStatus.Cancelled, task.Status);
        Assert.Equal(nurseId, task.CancelledByNurseId);
        Assert.Equal("Hasta taburcu oldu, takip gerekmiyor.", task.CancellationReason);
    }

    [Fact]
    public void NursingCareTaskCheckAndMarkOverdueMarksPendingPastDueAsOverdue()
    {
        var now = DateTime.UtcNow;
        var task = NursingCareTask.Create(Guid.NewGuid(), Guid.NewGuid(), "Saatlik idrar takibi", "Q1H", now.AddHours(-2), now.AddHours(-3));

        Assert.Equal(CareTaskStatus.Pending, task.Status);

        task.CheckAndMarkOverdue(now);

        Assert.Equal(CareTaskStatus.Overdue, task.Status);
    }
}
