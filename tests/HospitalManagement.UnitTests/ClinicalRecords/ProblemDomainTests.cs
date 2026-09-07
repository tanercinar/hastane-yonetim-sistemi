using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class ProblemDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void CreateProblemInitializesActiveStatusAndProperties()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var problem = ClinicalProblem.Create(
            id,
            patientId,
            null,
            "Tip 2 Diabetes Mellitus",
            "E11.9",
            ProblemCategory.ChronicCondition,
            new DateOnly(2020, 5, 10),
            "Oral antidiyabetik kullanıyor",
            doctorId,
            nowUtc);

        Assert.Equal(id, problem.Id);
        Assert.Equal(patientId, problem.PatientId);
        Assert.Equal("Tip 2 Diabetes Mellitus", problem.ProblemTitle);
        Assert.Equal("E11.9", problem.Code);
        Assert.Equal(ProblemCategory.ChronicCondition, problem.Category);
        Assert.Equal(ProblemClinicalStatus.Active, problem.ClinicalStatus);
        Assert.Equal(ProblemVerificationStatus.Confirmed, problem.VerificationStatus);
        Assert.Equal(new DateOnly(2020, 5, 10), problem.OnsetDate);
        Assert.Null(problem.ResolvedDate);
        Assert.Equal(1, problem.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void UpdateProblemStatusToResolvedSetsResolvedDateAndIncrementsVersion()
    {
        var problem = CreateSampleProblem();
        var resolvedDate = new DateOnly(2026, 8, 1);
        var nowUtc = DateTime.UtcNow;

        problem.UpdateStatus(ProblemClinicalStatus.Resolved, resolvedDate, "Tam iyileşme sağlandı", Guid.NewGuid(), nowUtc);

        Assert.Equal(ProblemClinicalStatus.Resolved, problem.ClinicalStatus);
        Assert.Equal(resolvedDate, problem.ResolvedDate);
        Assert.Equal("Tam iyileşme sağlandı", problem.Notes);
        Assert.Equal(2, problem.Version);
        Assert.Equal(nowUtc, problem.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void MarkEnteredInErrorSetsVerificationStatusAndReason()
    {
        var problem = CreateSampleProblem();
        var nowUtc = DateTime.UtcNow;

        problem.MarkEnteredInError(Guid.NewGuid(), "Mükerrer veya yanlış kayıt", nowUtc);

        Assert.Equal(ProblemVerificationStatus.EnteredInError, problem.VerificationStatus);
        Assert.Equal("Mükerrer veya yanlış kayıt", problem.EnteredInErrorReason);
        Assert.Equal(2, problem.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void UpdateStatusOnEnteredInErrorThrowsInvalidOperationException()
    {
        var problem = CreateSampleProblem();
        problem.MarkEnteredInError(Guid.NewGuid(), "Hata", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            problem.UpdateStatus(ProblemClinicalStatus.Inactive, null, null, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G02")]
    public void EmptyTitleOrPatientThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            ClinicalProblem.Create(
                Guid.NewGuid(),
                Guid.Empty,
                null,
                "Hipertansiyon",
                null,
                ProblemCategory.ChronicCondition,
                null,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            ClinicalProblem.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "   ",
                null,
                ProblemCategory.ChronicCondition,
                null,
                null,
                Guid.NewGuid(),
                DateTime.UtcNow));
    }

    private static ClinicalProblem CreateSampleProblem()
    {
        return ClinicalProblem.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Akut Farenjit",
            "J02",
            ProblemCategory.ActiveProblem,
            new DateOnly(2026, 8, 20),
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);
    }
}
