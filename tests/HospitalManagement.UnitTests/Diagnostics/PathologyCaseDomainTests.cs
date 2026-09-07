using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class PathologyCaseDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G08")]
    public void CreateValidParametersInitializesWithOrderedStatus()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var pathCase = PathologyCase.Create(
            id,
            orderId,
            orderItemId,
            patientId,
            "DEMO-PAT-2026-123456",
            PathologySpecimenType.Biopsy,
            "Mide Antrum",
            "Dispepsi ve gastrit ön tanısı",
            now);

        Assert.Equal(id, pathCase.Id);
        Assert.Equal("DEMO-PAT-2026-123456", pathCase.PathologyNumber);
        Assert.Equal(PathologyCaseStatus.Ordered, pathCase.Status);
        Assert.Equal("Mide Antrum", pathCase.AnatomicSite);
        Assert.Equal(1, pathCase.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G08")]
    public void ReceiveSpecimenAndRecordGrossAndMicroscopicExamsTransitionsCorrectly()
    {
        var pathCase = PathologyCase.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-PAT-2026-123456",
            PathologySpecimenType.Biopsy,
            "Mide Antrum",
            "Ön tanı",
            DateTime.UtcNow);

        var techUserId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        pathCase.ReceiveSpecimen(techUserId, "%10 Formalin", now);
        Assert.Equal(PathologyCaseStatus.SpecimenReceived, pathCase.Status);
        Assert.Equal("%10 Formalin", pathCase.FixativeUsed);

        pathCase.RecordGrossExam(techUserId, "2 adet milimetrik mukozal doku parçası. 1 kaset takibe alındı.", now);
        Assert.Equal(PathologyCaseStatus.GrossExamCompleted, pathCase.Status);
        Assert.NotNull(pathCase.GrossDescription);

        pathCase.RecordMicroscopicExam(techUserId, "Lamina propriada kronik inflamatuar infiltrat.", now);
        Assert.Equal(PathologyCaseStatus.MicroscopicExamCompleted, pathCase.Status);
        Assert.NotNull(pathCase.MicroscopicDescription);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G08")]
    public void FinalizeAndCorrectReportEnforcesImmutabilityAndCorrectionAudit()
    {
        var pathCase = PathologyCase.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-PAT-2026-123456",
            PathologySpecimenType.Biopsy,
            "Mide Antrum",
            "Ön tanı",
            DateTime.UtcNow);

        var user = Guid.NewGuid();
        var now = DateTime.UtcNow;

        pathCase.ReceiveSpecimen(user, "%10 Formalin", now);
        pathCase.RecordGrossExam(user, "Makroskopi", now);
        pathCase.RecordMicroscopicExam(user, "Mikroskopi", now);
        pathCase.DraftReport("KRONİK GASTRİT TASLAK", now);
        Assert.Equal(PathologyCaseStatus.ReportDrafted, pathCase.Status);

        pathCase.FinalizeReport(user, "KRONİK AKTİF GASTRİT (H. Pylori negatif)", now);
        Assert.Equal(PathologyCaseStatus.ReportFinalized, pathCase.Status);

        // Cannot finalize or modify directly again
        Assert.Throws<InvalidOperationException>(() =>
            pathCase.FinalizeReport(user, "YENİ TANI", now));

        // Corrected report
        var corrected = PathologyCase.CreateCorrected(
            Guid.NewGuid(),
            pathCase,
            "Giemsa boyama ile H. Pylori pozitifliği tespit edildi.",
            "KRONİK AKTİF GASTRİT (H. Pylori POZİTİF)",
            user,
            now);

        Assert.Equal(PathologyCaseStatus.Corrected, corrected.Status);
        Assert.Equal(pathCase.Id, corrected.PreviousCaseId);
        Assert.Contains("-CORR", corrected.PathologyNumber, StringComparison.Ordinal);
        Assert.Equal("Giemsa boyama ile H. Pylori pozitifliği tespit edildi.", corrected.CorrectionReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void FinalizeBeforeMicroscopicExamIsRejected()
    {
        var pathCase = PathologyCase.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-PAT-2026-654321",
            PathologySpecimenType.Biopsy,
            "Mide Antrum",
            "Ön tanı",
            DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            pathCase.FinalizeReport(Guid.NewGuid(), "Erken final raporu", DateTime.UtcNow));

        Assert.Contains("mikroskopi", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PathologyCaseStatus.Ordered, pathCase.Status);
    }
}
