using HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;
using Xunit;

namespace HospitalManagement.UnitTests.SpecialtyCare;

public sealed class DentalDomainTests
{
    [Theory]
    [InlineData(11, true, true, false)]
    [InlineData(18, true, true, false)]
    [InlineData(26, true, true, false)]
    [InlineData(37, true, true, false)]
    [InlineData(48, true, true, false)]
    [InlineData(51, true, false, true)]
    [InlineData(85, true, false, true)]
    [InlineData(19, false, false, false)]
    [InlineData(29, false, false, false)]
    [InlineData(50, false, false, false)]
    [InlineData(99, false, false, false)]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G03")]
    public void FdiToothValidatorValidatesToothNumbersCorrectly(int toothNumber, bool isValid, bool isAdult, bool isPrimary)
    {
        Assert.Equal(isValid, FdiToothValidator.IsValidToothNumber(toothNumber));
        Assert.Equal(isAdult, FdiToothValidator.IsAdultTooth(toothNumber));
        Assert.Equal(isPrimary, FdiToothValidator.IsPrimaryTooth(toothNumber));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G03")]
    public void DentalToothConditionRecordCreatesValidConditionAndVersion()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var condition = DentalToothCondition.Record(
            id,
            patientId,
            16,
            ToothCondition.Caries,
            ToothSurface.Occlusal | ToothSurface.Mesial,
            "Derin oklüzal kavite",
            staffId,
            1,
            now);

        Assert.Equal(id, condition.Id);
        Assert.Equal(patientId, condition.PatientId);
        Assert.Equal(16, condition.ToothNumber);
        Assert.Equal(ToothCondition.Caries, condition.Condition);
        Assert.Equal(ToothSurface.Occlusal | ToothSurface.Mesial, condition.AffectedSurfaces);
        Assert.Equal(1, condition.Version);
        Assert.Equal(staffId, condition.RecordedByStaffId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G03")]
    public void DentalProcedureLifecyclePlanAndCompleteSucceeds()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var proc = DentalProcedure.Plan(
            id,
            patientId,
            null,
            26,
            ToothSurface.Occlusal,
            "DNT-FILLING",
            "Kompozit Dolgu",
            850,
            doctorId,
            now.AddDays(1),
            "1. seans dolgu",
            now);

        Assert.Equal(id, proc.Id);
        Assert.StartsWith("DEMO-DNT-", proc.ProcedureProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(DentalProcedureStatus.Planned, proc.Status);
        Assert.Equal(850, proc.EstimatedCost);

        proc.Complete(now.AddDays(1), "Dolgu tamamlandı, yükseklik kontrolü yapıldı", now.AddDays(1));

        Assert.Equal(DentalProcedureStatus.Completed, proc.Status);
        Assert.NotNull(proc.CompletedDateUtc);
        Assert.Contains("Dolgu tamamlandı", proc.ClinicalNotes, StringComparison.Ordinal);

        // Cannot complete again
        Assert.Throws<InvalidOperationException>(() => proc.Complete(DateTime.UtcNow, null, DateTime.UtcNow));
        // Cannot cancel completed
        Assert.Throws<InvalidOperationException>(() => proc.Cancel("İptal denemesi", DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F09-G03")]
    public void DentalExaminationRecordCreateSetsProtocolAndProperties()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var dentistId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var exam = DentalExaminationRecord.Create(
            id,
            patientId,
            null,
            dentistId,
            now,
            "Sıcak/soğuk hassasiyeti",
            "16 çürük, 46 eksik",
            "16 dolgu, 46 köprü/implant planı",
            now);

        Assert.Equal(id, exam.Id);
        Assert.StartsWith("DEMO-DEN-", exam.ExaminationProtocolNumber, StringComparison.Ordinal);
        Assert.Equal(patientId, exam.PatientId);
        Assert.Equal(dentistId, exam.DentistId);
        Assert.Equal("Sıcak/soğuk hassasiyeti", exam.ChiefComplaint);
    }
}
