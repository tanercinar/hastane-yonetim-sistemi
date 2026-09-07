using HospitalManagement.Modules.Scheduling.Domain;

namespace HospitalManagement.UnitTests;

public sealed class AppointmentDomainUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G04")]
    public void AppointmentCreationAndValidLifecycleTransitionsWork()
    {
        var now = DateTime.UtcNow;
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddDays(1),
            "Baş Ağrısı",
            now);

        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.Equal("Baş Ağrısı", appointment.ReasonForVisit);
        Assert.Equal(1, appointment.Version);

        // CheckIn
        var checkInTime = now.AddDays(1).AddMinutes(-5);
        appointment.CheckIn(1, checkInTime);
        Assert.Equal(AppointmentStatus.CheckedIn, appointment.Status);
        Assert.Equal(1, appointment.QueueNumber);
        Assert.Equal(checkInTime, appointment.CheckedInAtUtc);
        Assert.Equal(2, appointment.Version);

        // Complete
        var completeTime = now.AddDays(1).AddMinutes(20);
        appointment.Complete(completeTime);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal(completeTime, appointment.CompletedAtUtc);
        Assert.Equal(3, appointment.Version);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G04")]
    public void InvalidStateTransitionsThrowInvalidOperationException()
    {
        var now = DateTime.UtcNow;
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddDays(1),
            "Rutin Kontrol",
            now);

        // Cannot complete before check-in
        Assert.Throws<InvalidOperationException>(() => appointment.Complete(now));

        // Cancel
        appointment.Cancel("Hasta acil durumu", now);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Hasta acil durumu", appointment.CancellationReason);

        // Cannot check in cancelled appointment
        Assert.Throws<InvalidOperationException>(() => appointment.CheckIn(1, now));

        // Cannot cancel already cancelled appointment
        Assert.Throws<InvalidOperationException>(() => appointment.Cancel("İkinci iptal", now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G04")]
    public void CompletedAppointmentCannotBeCancelled()
    {
        var now = DateTime.UtcNow;
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            "Göz Muayenesi",
            now);

        appointment.CheckIn(1, now);
        appointment.Complete(now);

        Assert.Throws<InvalidOperationException>(() => appointment.Cancel("İptal denemesi", now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G06")]
    public void CheckInRequiresPositiveQueueNumberAndConfirmedStatus()
    {
        var now = DateTime.UtcNow;
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(2),
            "Kontrol",
            now);

        Assert.Throws<ArgumentOutOfRangeException>(() => appointment.CheckIn(0, now));
        Assert.Throws<ArgumentOutOfRangeException>(() => appointment.CheckIn(-5, now));

        appointment.CheckIn(42, now);
        Assert.Equal(42, appointment.QueueNumber);

        // Cannot check-in again
        Assert.Throws<InvalidOperationException>(() => appointment.CheckIn(43, now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G06")]
    public void MarkNoShowTransitionsToNoShowWhenConfirmed()
    {
        var now = DateTime.UtcNow;
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(1),
            "Kontrol",
            now);

        appointment.MarkNoShow(now);
        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);

        // Cannot check-in or complete no-show appointment
        Assert.Throws<InvalidOperationException>(() => appointment.CheckIn(1, now));
        Assert.Throws<InvalidOperationException>(() => appointment.Complete(now));
    }
}
