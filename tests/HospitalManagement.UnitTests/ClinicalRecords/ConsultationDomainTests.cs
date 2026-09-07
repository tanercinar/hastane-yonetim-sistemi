using HospitalManagement.Modules.ClinicalRecords.Domain;

namespace HospitalManagement.UnitTests.ClinicalRecords;

public sealed class ConsultationDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void CreateConsultationInitializesPropertiesAndSetsStatusRequested()
    {
        var id = Guid.NewGuid();
        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestingDocId = Guid.NewGuid();
        var targetDeptId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var consultation = ConsultationRequest.Create(
            id,
            encounterId,
            patientId,
            requestingDocId,
            targetDeptId,
            targetPractitionerId: null,
            ConsultationUrgency.Urgent,
            "Göğüs ağrısı ayırıcı tanısı",
            "Troponin yüksekliği ve EKG değişikliği açısından değerlendiriniz.",
            nowUtc);

        Assert.Equal(id, consultation.Id);
        Assert.Equal(encounterId, consultation.EncounterId);
        Assert.Equal(patientId, consultation.PatientId);
        Assert.Equal(requestingDocId, consultation.RequestingPractitionerId);
        Assert.Equal(targetDeptId, consultation.TargetDepartmentId);
        Assert.Null(consultation.TargetPractitionerId);
        Assert.Equal(ConsultationUrgency.Urgent, consultation.Urgency);
        Assert.Equal(ConsultationStatus.Requested, consultation.Status);
        Assert.Equal("Göğüs ağrısı ayırıcı tanısı", consultation.ReasonForConsultation);
        Assert.Equal("Troponin yüksekliği ve EKG değişikliği açısından değerlendiriniz.", consultation.ClinicalQuestion);
        Assert.Equal(nowUtc, consultation.RequestedAtUtc);
        Assert.Equal(1, consultation.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void AcceptConsultationTransitionsStatusToAcceptedAndSetsAssignedPractitioner()
    {
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        var consultingDoctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        consultation.Accept(consultingDoctorId, nowUtc);

        Assert.Equal(ConsultationStatus.Accepted, consultation.Status);
        Assert.Equal(consultingDoctorId, consultation.AssignedPractitionerId);
        Assert.Equal(nowUtc, consultation.AcceptedAtUtc);
        Assert.Equal(2, consultation.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void CompleteConsultationTransitionsStatusToCompletedAndSetsReport()
    {
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        var consultingDoctorId = Guid.NewGuid();
        consultation.Accept(consultingDoctorId, DateTime.UtcNow);

        var completeTimeUtc = DateTime.UtcNow;
        consultation.Complete(
            consultingDoctorId,
            "EKG'de ST elevasyonu saptanmadı. EKO'da EF %60, segmental duvar hareket kusuru yok.",
            "Poliklinik kontrolü önerilir.",
            completeTimeUtc);

        Assert.Equal(ConsultationStatus.Completed, consultation.Status);
        Assert.Equal("EKG'de ST elevasyonu saptanmadı. EKO'da EF %60, segmental duvar hareket kusuru yok.", consultation.ConsultationReport);
        Assert.Equal("Poliklinik kontrolü önerilir.", consultation.Recommendation);
        Assert.Equal(completeTimeUtc, consultation.CompletedAtUtc);
        Assert.Equal(3, consultation.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void CompleteWithoutAcceptanceThrowsInvalidOperationException()
    {
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            consultation.Complete(Guid.NewGuid(), "Rapor", null, DateTime.UtcNow));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void DeclineConsultationTransitionsStatusToDeclined()
    {
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        var consultingDoctorId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        consultation.Decline(consultingDoctorId, "İlgili vaka nöroloji branşı kapsamındadır.", nowUtc);

        Assert.Equal(ConsultationStatus.Declined, consultation.Status);
        Assert.Equal("İlgili vaka nöroloji branşı kapsamındadır.", consultation.DeclineReason);
        Assert.Equal(2, consultation.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void CancelConsultationTransitionsStatusToCancelled()
    {
        var requestingDocId = Guid.NewGuid();
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            requestingDocId,
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        var nowUtc = DateTime.UtcNow;
        consultation.Cancel(requestingDocId, "Hasta acil taburcu edildi.", nowUtc);

        Assert.Equal(ConsultationStatus.Cancelled, consultation.Status);
        Assert.Equal("Hasta acil taburcu edildi.", consultation.CancellationReason);
        Assert.Equal(2, consultation.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F04-G06")]
    public void MarkEnteredInErrorTransitionsStatusAndSetsReason()
    {
        var consultation = ConsultationRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            ConsultationUrgency.Routine,
            "Gerekçe",
            "Soru",
            DateTime.UtcNow);

        var nowUtc = DateTime.UtcNow;
        consultation.MarkEnteredInError(Guid.NewGuid(), "Yanlış karşılaşmaya açılan konsültasyon", nowUtc);

        Assert.Equal(ConsultationStatus.EnteredInError, consultation.Status);
        Assert.Equal("Yanlış karşılaşmaya açılan konsültasyon", consultation.EnteredInErrorReason);
        Assert.Equal(2, consultation.Version);
    }
}
