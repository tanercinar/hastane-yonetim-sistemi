using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class CriticalResultNotificationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G05")]
    public void CreateInitializesActiveNotificationWithEscalationLevel1()
    {
        var id = Guid.NewGuid();
        var labResultId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notification = CriticalResultNotification.Create(
            id,
            labResultId,
            orderId,
            orderItemId,
            patientId,
            "K",
            "Potasyum",
            6.8m,
            null,
            "mmol/L",
            LabResultInterpretation.CriticalHigh,
            doctorId,
            now);

        Assert.Equal(id, notification.Id);
        Assert.Equal(CriticalNotificationStatus.Active, notification.Status);
        Assert.Equal(1, notification.EscalationLevel);
        Assert.Equal(doctorId, notification.ResponsibleDoctorUserId);
        Assert.Equal(LabResultInterpretation.CriticalHigh, notification.Flag);
        Assert.Equal(1, notification.Version);
        Assert.Null(notification.AcknowledgedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G05")]
    public void AcknowledgeTransitionsToAcknowledgedAndRecordsUserAndNotes()
    {
        var id = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notification = CriticalResultNotification.Create(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "GLU",
            "Glukoz",
            35m,
            null,
            "mg/dL",
            LabResultInterpretation.CriticalLow,
            doctorId,
            now);

        notification.Acknowledge(doctorId, "Hasta serviste görüldü, dekstroz infüzyonu başlandı.", now.AddMinutes(5));

        Assert.Equal(CriticalNotificationStatus.Acknowledged, notification.Status);
        Assert.Equal(doctorId, notification.AcknowledgedByUserId);
        Assert.Equal("Hasta serviste görüldü, dekstroz infüzyonu başlandı.", notification.AcknowledgmentNotes);
        Assert.Equal(2, notification.Version);
        Assert.NotNull(notification.AcknowledgedAtUtc);

        // Attempting to acknowledge again throws
        Assert.Throws<InvalidOperationException>(() =>
            notification.Acknowledge(doctorId, "Tekrar", now.AddMinutes(10)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G05")]
    public void EscalateIncrementsEscalationLevelAndTransitionsToEscalated()
    {
        var id = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notification = CriticalResultNotification.Create(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TROP-I",
            "Troponin-I",
            12.5m,
            null,
            "ng/mL",
            LabResultInterpretation.CriticalHigh,
            doctorId,
            now);

        // 1st escalation
        notification.Escalate("Birincil hekime 15 dakika ulaşılamadı", now.AddMinutes(15));

        Assert.Equal(CriticalNotificationStatus.Escalated, notification.Status);
        Assert.Equal(2, notification.EscalationLevel);
        Assert.Equal("Birincil hekime 15 dakika ulaşılamadı", notification.EscalationReason);
        Assert.Equal(2, notification.Version);
        Assert.NotNull(notification.EscalatedAtUtc);

        // Can still be acknowledged after escalation
        notification.Acknowledge(doctorId, "Nöbetçi uzman tarafından teslim alındı.", now.AddMinutes(20));
        Assert.Equal(CriticalNotificationStatus.Acknowledged, notification.Status);
        Assert.Equal(3, notification.Version);
    }
}
