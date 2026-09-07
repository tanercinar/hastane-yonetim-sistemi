using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class RadiologyStudyDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G06")]
    public void CreateInitializesOrderedStudy()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var study = RadiologyStudy.Create(
            id,
            orderId,
            orderItemId,
            patientId,
            "DEMO-ACC-20260830-123456",
            RadiologyModality.XR,
            "DEMO-RAD-CHEST-XR",
            "Akciğer Grafisi (PA)",
            "Toraks",
            now);

        Assert.Equal(id, study.Id);
        Assert.Equal(orderId, study.DiagnosticOrderId);
        Assert.Equal(orderItemId, study.DiagnosticOrderItemId);
        Assert.Equal(patientId, study.PatientId);
        Assert.Equal("DEMO-ACC-20260830-123456", study.AccessionNumber);
        Assert.Equal(RadiologyModality.XR, study.Modality);
        Assert.Equal(RadiologyStudyStatus.Ordered, study.Status);
        Assert.Equal(1, study.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G06")]
    public void ScheduleAndCompleteAcquisitionTransitionsStatus()
    {
        var now = DateTime.UtcNow;
        var study = CreateSampleStudy(now);

        var scheduledTime = now.AddHours(2);
        study.Schedule(scheduledTime, now);

        Assert.Equal(RadiologyStudyStatus.Scheduled, study.Status);
        Assert.Equal(scheduledTime, study.ScheduledAtUtc);
        Assert.Equal(2, study.Version);

        var techId = Guid.NewGuid();
        study.CompleteAcquisition(techId, "PA pozisyonunda kaliteli çekim", now.AddHours(2));

        Assert.Equal(RadiologyStudyStatus.Acquired, study.Status);
        Assert.Equal(techId, study.TechnicianUserId);
        Assert.Equal("PA pozisyonunda kaliteli çekim", study.TechnicianNotes);
        Assert.Equal(3, study.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G06")]
    public void DraftReportAndFinalizeReportFollowsClinicalImmutability()
    {
        var now = DateTime.UtcNow;
        var study = CreateSampleStudy(now);
        var techId = Guid.NewGuid();
        study.Schedule(now, now);
        study.CompleteAcquisition(techId, null, now);

        var radiologistId = Guid.NewGuid();
        study.DraftReport(radiologistId, "Akciğer parankim alanlarında aktif infiltrasyon izlenmedi.", "Normal PA grafi", now.AddMinutes(10));

        Assert.Equal(RadiologyStudyStatus.ReportDrafted, study.Status);
        Assert.Equal(radiologistId, study.RadiologistUserId);
        Assert.Equal("Akciğer parankim alanlarında aktif infiltrasyon izlenmedi.", study.ReportText);

        study.FinalizeReport(radiologistId, "Akciğer parankim alanlarında aktif infiltrasyon saptanmamıştır. Kostodiafragmatik sinüsler açıktır.", "Normal sınırlarda PA Akciğer Grafisi", now.AddMinutes(15));

        Assert.Equal(RadiologyStudyStatus.ReportFinalized, study.Status);
        Assert.NotNull(study.ReportFinalizedAtUtc);

        // Cannot finalize again
        Assert.Throws<InvalidOperationException>(() =>
            study.FinalizeReport(radiologistId, "Yeni metin", "Yeni kanaat", now.AddMinutes(20)));

        // Cannot draft after final
        Assert.Throws<InvalidOperationException>(() =>
            study.DraftReport(radiologistId, "Taslak", null, now.AddMinutes(20)));

        // Cannot cancel after final
        Assert.Throws<InvalidOperationException>(() =>
            study.Cancel("İptal", now.AddMinutes(20)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G06")]
    public void AddAddendumAppendsAddendumToFinalizedReport()
    {
        var now = DateTime.UtcNow;
        var study = CreateSampleStudy(now);
        var techId = Guid.NewGuid();
        var radiologistId = Guid.NewGuid();
        study.Schedule(now, now);
        study.CompleteAcquisition(techId, null, now);
        study.FinalizeReport(radiologistId, "İlk rapor metni", "Normal", now);

        study.AddAddendum(radiologistId, "Klinisyenin talebi üzerine hilus yapıları tekrar değerlendirildi, vasküler dolgunluk izlenmedi.", now.AddHours(1));

        Assert.Equal(RadiologyStudyStatus.AddendumAdded, study.Status);
        Assert.Contains("vasküler dolgunluk izlenmedi.", study.AddendumText, StringComparison.Ordinal);
        Assert.Equal(radiologistId, study.AddendumByUserId);
        Assert.NotNull(study.AddendumAddedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G06")]
    public void AddAddendumOnNonFinalizedStudyThrowsInvalidOperationException()
    {
        var now = DateTime.UtcNow;
        var study = CreateSampleStudy(now);
        var radiologistId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            study.AddAddendum(radiologistId, "Ek not", now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-REVIEW")]
    public void CompleteAcquisitionBeforeSchedulingIsRejected()
    {
        var study = CreateSampleStudy(DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            study.CompleteAcquisition(Guid.NewGuid(), null, DateTime.UtcNow));

        Assert.Contains("randevulanmış", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(RadiologyStudyStatus.Ordered, study.Status);
    }

    private static RadiologyStudy CreateSampleStudy(DateTime now) =>
        RadiologyStudy.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO-ACC-20260830-999111",
            RadiologyModality.CT,
            "DEMO-RAD-THORAX-CT",
            "Toraks Yüksek Çözünürlüklü BT (HRCT)",
            "Toraks",
            now);
}
