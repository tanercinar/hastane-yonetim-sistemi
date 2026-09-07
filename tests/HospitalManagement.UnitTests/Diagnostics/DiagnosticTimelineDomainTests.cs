using HospitalManagement.Modules.Diagnostics.Application;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class DiagnosticTimelineDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G10")]
    public void TimelineParameterDtoStoresCorrectInterpretationAndCriticalFlag()
    {
        var param = new TimelineParameterDto(
            "POTASSIUM",
            "6.8",
            "mmol/L",
            "3.5 - 5.0",
            "CriticalHigh",
            true);

        Assert.Equal("POTASSIUM", param.Name);
        Assert.Equal("6.8", param.Value);
        Assert.True(param.IsCritical);
        Assert.Equal("CriticalHigh", param.Interpretation);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G10")]
    public void PatientPortalSummaryFlagsPendingDoctorReviewOnCriticalNotification()
    {
        var summary = new PatientPortalResultSummaryDto(
            Guid.NewGuid(),
            "Laboratory",
            "Biyokimya Paneli",
            "FinalApproved",
            DateTime.UtcNow,
            "Biyokimya",
            true, // IsPendingDoctorReview
            "Kritik değer hekim değerlendirmesi aşamasındadır.",
            null);

        Assert.True(summary.IsPendingDoctorReview);
        Assert.Contains("hekim değerlendirmesi", summary.FinalReportDiagnosis);
    }
}
