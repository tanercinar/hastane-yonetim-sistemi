using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class SpecimenDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G03")]
    public void CollectInitializesCollectedStatusAndFirstTransitionEvent()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var nowUtc = DateTime.UtcNow;

        var specimen = Specimen.Collect(
            id,
            "DEMO-SMP-20260830-123456",
            orderId,
            patientId,
            "Venöz Tam Kan",
            "Mor Kapaklı EDTA Tüp",
            userId,
            "Nurse",
            "Kan Alma Ünitesi",
            "Hasta aç karnına geldi.",
            nowUtc);

        Assert.Equal(id, specimen.Id);
        Assert.Equal("DEMO-SMP-20260830-123456", specimen.Barcode);
        Assert.Equal(SpecimenStatus.Collected, specimen.Status);
        Assert.Equal(nowUtc, specimen.CollectedAtUtc);
        Assert.Equal(userId, specimen.CollectedByUserId);
        Assert.Single(specimen.Transitions);

        var firstEvent = specimen.Transitions.First();
        Assert.Equal(SpecimenStatus.Collected, firstEvent.FromStatus);
        Assert.Equal(SpecimenStatus.Collected, firstEvent.ToStatus);
        Assert.Equal("Kan Alma Ünitesi", firstEvent.Location);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G03")]
    public void FullCustodyLifecycleCollectTransitReceiveCompleteDisposeTransitionsProperly()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var nurseId = Guid.NewGuid();
        var courierId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // 1. Collect
        var specimen = Specimen.Collect(
            id, "DEMO-SMP-20260830-123456", orderId, patientId,
            "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", nurseId, "Nurse", "Kan Alma Ünitesi", null, now);

        // 2. Transit
        specimen.MarkInTransit(courierId, "Courier", "Pnömatik Hat", "Numune kuryeye verildi", now.AddMinutes(5));
        Assert.Equal(SpecimenStatus.InTransit, specimen.Status);

        // 3. Receive
        specimen.ReceiveAtLab(techId, "LabTechnician", "Merkez Laboratuvar", "Barkod okutuldu", now.AddMinutes(15));
        Assert.Equal(SpecimenStatus.Received, specimen.Status);
        Assert.Equal(techId, specimen.ReceivedByUserId);

        // 4. Start Processing
        specimen.StartProcessing(techId, "LabTechnician", "Analizör 1", "Cihaza yüklendi", now.AddMinutes(20));
        Assert.Equal(SpecimenStatus.Processing, specimen.Status);

        // 5. Complete
        specimen.Complete(techId, "LabTechnician", "Merkez Laboratuvar", "Analiz bitti", now.AddMinutes(40));
        Assert.Equal(SpecimenStatus.Completed, specimen.Status);

        // 6. Dispose
        specimen.Dispose(techId, "LabTechnician", "Tıbbi Atık", "Saklama süresi bitti", now.AddDays(7));
        Assert.Equal(SpecimenStatus.Disposed, specimen.Status);

        Assert.Equal(6, specimen.Transitions.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G03")]
    public void RejectSetsRejectionReasonAndRecordsCustodyEvent()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var specimen = Specimen.Collect(
            id, "DEMO-SMP-20260830-123456", orderId, patientId,
            "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", techId, "Nurse", "Kan Alma Ünitesi", null, now);

        specimen.Reject(techId, "LabTechnician", "Hemolizli numune (Analize uygun değil)", "Merkez Lab", "Tüp hemolizli", now.AddMinutes(10));

        Assert.Equal(SpecimenStatus.Rejected, specimen.Status);
        Assert.Equal("Hemolizli numune (Analize uygun değil)", specimen.RejectionReason);
        Assert.Equal(techId, specimen.RejectedByUserId);
        Assert.Equal(2, specimen.Transitions.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G03")]
    public void InvalidTransitionThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var techId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var specimen = Specimen.Collect(
            id, "DEMO-SMP-20260830-123456", orderId, patientId,
            "Venöz Tam Kan", "Mor Kapaklı EDTA Tüp", techId, "Nurse", "Kan Alma Ünitesi", null, now);

        // Cannot start processing directly from Collected (must Receive first)
        Assert.Throws<InvalidOperationException>(() =>
            specimen.StartProcessing(techId, "LabTechnician", null, null, now));
    }
}
