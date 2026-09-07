using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class LabResultDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G04")]
    public void EvaluateFlagCorrectlyIdentifiesReferenceRangesAndCriticalThresholds()
    {
        // Reference range: 20 to 100
        decimal low = 20m;
        decimal high = 100m;

        // Normal
        Assert.Equal(LabResultInterpretation.Normal, LabResultItem.EvaluateFlag(50m, null, low, high));

        // Low
        Assert.Equal(LabResultInterpretation.Low, LabResultItem.EvaluateFlag(15m, null, low, high));

        // Critical Low (<= 50% of low -> <= 10)
        Assert.Equal(LabResultInterpretation.CriticalLow, LabResultItem.EvaluateFlag(10m, null, low, high));
        Assert.Equal(LabResultInterpretation.CriticalLow, LabResultItem.EvaluateFlag(5m, null, low, high));

        // High
        Assert.Equal(LabResultInterpretation.High, LabResultItem.EvaluateFlag(150m, null, low, high));

        // Critical High (>= 200% of high -> >= 200)
        Assert.Equal(LabResultInterpretation.CriticalHigh, LabResultItem.EvaluateFlag(200m, null, low, high));
        Assert.Equal(LabResultInterpretation.CriticalHigh, LabResultItem.EvaluateFlag(350m, null, low, high));

        // String value abnormal tests
        Assert.Equal(LabResultInterpretation.Abnormal, LabResultItem.EvaluateFlag(null, "Pozitif", null, null));
        Assert.Equal(LabResultInterpretation.Abnormal, LabResultItem.EvaluateFlag(null, "Reaktif", null, null));
        Assert.Equal(LabResultInterpretation.Normal, LabResultItem.EvaluateFlag(null, "Negatif", null, null));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G04")]
    public void CreateDraftInitializesWithDraftStatusAndUpdatesItemValues()
    {
        var resultId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var item1 = LabResultItem.Create(
            Guid.NewGuid(),
            resultId,
            "GLU",
            "Glukoz",
            null,
            null,
            "mg/dL",
            70m,
            100m,
            "70 - 100");

        var labResult = LabResult.CreateDraft(
            resultId,
            orderId,
            itemId,
            null,
            patientId,
            "L-BIO-01",
            "Açlık Kan Şekeri",
            "Rutin kontrol",
            now,
            [item1]);

        Assert.Equal(LabResultStatus.Draft, labResult.Status);
        Assert.Equal(1, labResult.Version);
        Assert.Single(labResult.Items);
        Assert.Equal(LabResultInterpretation.Normal, labResult.Items.First().Flag);

        // Update items in draft
        labResult.UpdateItems([("GLU", 180m, null, "Tokluk şüphesi")], "Yeniden değerlendirilecek", now.AddMinutes(5));

        Assert.Equal(2, labResult.Version);
        Assert.Equal(180m, labResult.Items.First().NumericValue);
        Assert.Equal(LabResultInterpretation.High, labResult.Items.First().Flag);
        Assert.Equal("Tokluk şüphesi", labResult.Items.First().Notes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G04")]
    public void TechnicalAndClinicalApprovalLifecycleLocksDirectModification()
    {
        var resultId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var techUserId = Guid.NewGuid();
        var docUserId = Guid.NewGuid();

        var item = LabResultItem.Create(Guid.NewGuid(), resultId, "WBC", "Lökosit", 7.5m, null, "10^3/uL", 4.0m, 10.0m, "4.0 - 10.0");
        var result = LabResult.CreateDraft(resultId, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "L-HEM-01", "Hemogram", null, now, [item]);

        // Technical approve
        result.ApproveTechnically(techUserId, now.AddMinutes(10));
        Assert.Equal(LabResultStatus.TechnicallyApproved, result.Status);
        Assert.Equal(techUserId, result.TechnicallyApprovedByUserId);
        Assert.Equal(2, result.Version);

        // Clinical approve
        result.ApproveClinically(docUserId, now.AddMinutes(20));
        Assert.Equal(LabResultStatus.FinalApproved, result.Status);
        Assert.Equal(docUserId, result.ClinicallyApprovedByUserId);
        Assert.Equal(3, result.Version);

        // Attempting to update approved result directly must throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
            result.UpdateItems([("WBC", 12.0m, null, null)], null, now.AddMinutes(25)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void ClinicalApprovalBeforeTechnicalApprovalIsRejected()
    {
        var resultId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var item = LabResultItem.Create(
            Guid.NewGuid(),
            resultId,
            "WBC",
            "Lökosit",
            7.5m,
            null,
            "10^3/uL",
            4.0m,
            10.0m,
            "4.0 - 10.0");
        var result = LabResult.CreateDraft(
            resultId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            "L-HEM-01",
            "Hemogram",
            null,
            now,
            [item]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            result.ApproveClinically(Guid.NewGuid(), now.AddMinutes(5)));

        Assert.Contains("teknik onayı", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(LabResultStatus.Draft, result.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G04")]
    public void CreateCorrectionCreatesNewLinkedResultPreservingAuditChain()
    {
        var resultId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var docUserId = Guid.NewGuid();

        var item = LabResultItem.Create(Guid.NewGuid(), resultId, "K", "Potasyum", 3.8m, null, "mmol/L", 3.5m, 5.1m, "3.5 - 5.1");
        var originalResult = LabResult.CreateDraft(resultId, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), "L-BIO-02", "Elektrolitler", null, now, [item]);
        originalResult.ApproveTechnically(docUserId, now.AddMinutes(2));
        originalResult.ApproveClinically(docUserId, now.AddMinutes(5));

        // Create correction
        var newResultId = Guid.NewGuid();
        var correctedResult = originalResult.CreateCorrection(
            newResultId,
            "Hemolizli numune şüphesiyle tekrar çalışıldı.",
            docUserId,
            now.AddMinutes(30),
            [("K", 4.2m, null, "Doğrulandı")]);

        Assert.Equal(LabResultStatus.Corrected, correctedResult.Status);
        Assert.Equal(originalResult.Id, correctedResult.PreviousResultId);
        Assert.Equal("Hemolizli numune şüphesiyle tekrar çalışıldı.", correctedResult.CorrectionReason);
        Assert.Single(correctedResult.Items);
        Assert.Equal(4.2m, correctedResult.Items.First().NumericValue);
        Assert.Equal(LabResultInterpretation.Normal, correctedResult.Items.First().Flag);
    }
}
