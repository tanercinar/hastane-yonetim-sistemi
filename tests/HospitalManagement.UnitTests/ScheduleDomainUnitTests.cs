using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;

namespace HospitalManagement.UnitTests;

public sealed class ScheduleDomainUnitTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G03")]
    public void DoctorScheduleCreationAndBreaksValidateCorrectly()
    {
        var now = DateTime.UtcNow;
        var doctorId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        var schedule = DoctorSchedule.Create(
            Guid.NewGuid(),
            doctorId,
            departmentId,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            slotDurationMinutes: 15,
            now);

        Assert.Equal(DayOfWeek.Monday, schedule.DayOfWeek);
        Assert.Equal(15, schedule.SlotDurationMinutes);
        Assert.True(schedule.IsActive);

        // Add valid break
        schedule.AddBreak(Guid.NewGuid(), new TimeOnly(12, 0), new TimeOnly(13, 0), "Öğle Molası");
        Assert.Single(schedule.Breaks);

        // Break outside window throws
        Assert.Throws<InvalidOperationException>(() =>
            schedule.AddBreak(Guid.NewGuid(), new TimeOnly(8, 0), new TimeOnly(8, 30), "Erken"));

        // Overlapping break throws
        Assert.Throws<InvalidOperationException>(() =>
            schedule.AddBreak(Guid.NewGuid(), new TimeOnly(12, 30), new TimeOnly(13, 30), "Çakışan Mola"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G03")]
    public void AppointmentSlotHoldingAndBookingLifecycleWorks()
    {
        var now = DateTime.UtcNow;
        var patientPersonId = Guid.NewGuid();
        var anotherPersonId = Guid.NewGuid();

        var slot = AppointmentSlot.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now.AddHours(2),
            now.AddHours(2).AddMinutes(20),
            now);

        Assert.Equal(SlotStatus.Available, slot.Status);
        Assert.Equal(1, slot.Version);

        // Hold slot
        slot.Hold(patientPersonId, TimeSpan.FromMinutes(10), now);
        Assert.Equal(SlotStatus.Held, slot.Status);
        Assert.Equal(patientPersonId, slot.HeldByPersonId);
        Assert.Equal(2, slot.Version);

        // Another person cannot book held slot
        Assert.Throws<InvalidOperationException>(() =>
            slot.Book(anotherPersonId, now));

        // Same person can book their held slot
        slot.Book(patientPersonId, now);
        Assert.Equal(SlotStatus.Booked, slot.Status);
        Assert.Equal(3, slot.Version);

        // Cannot book already booked slot
        Assert.Throws<InvalidOperationException>(() =>
            slot.Book(patientPersonId, now));
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F03-G03")]
    public void SlotGenerationEngineGeneratesSlotsAndExcludesBreaksAndLeaves()
    {
        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var doctorId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();

        // Schedule: Monday 09:00 - 12:00 with 30 min slots (6 slots: 09:00, 09:30, 10:00, 10:30, 11:00, 11:30)
        var mondaySchedule = DoctorSchedule.Create(
            Guid.NewGuid(),
            doctorId,
            departmentId,
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            slotDurationMinutes: 30,
            now);

        // Break: 10:00 - 10:30 (eliminates the 10:00 slot -> 5 slots remaining)
        mondaySchedule.AddBreak(Guid.NewGuid(), new TimeOnly(10, 0), new TimeOnly(10, 30), "Kahve Molası");

        // Monday 2026-09-07
        var testMonday = new DateOnly(2026, 9, 7);
        Assert.Equal(DayOfWeek.Monday, testMonday.DayOfWeek);

        // Leave: 2026-09-07 11:00 to 12:00 UTC (eliminates 11:00 and 11:30 slots -> 3 slots remaining: 09:00, 09:30, 10:30)
        var leaveBlock = DoctorLeaveBlock.Create(
            Guid.NewGuid(),
            doctorId,
            new DateTime(2026, 9, 7, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc),
            "Eğitim",
            now);

        var tz = TimeZoneInfo.Utc;
        var existingStartTimes = new HashSet<DateTime>();

        var slots = SlotGenerationEngine.GenerateSlotsForDateRange(
            mondaySchedule,
            [leaveBlock],
            existingStartTimes,
            testMonday,
            testMonday,
            tz,
            now);

        Assert.Equal(3, slots.Count);
        Assert.Equal(new DateTime(2026, 9, 7, 9, 0, 0, DateTimeKind.Utc), slots[0].StartUtc);
        Assert.Equal(new DateTime(2026, 9, 7, 9, 30, 0, DateTimeKind.Utc), slots[1].StartUtc);
        Assert.Equal(new DateTime(2026, 9, 7, 10, 30, 0, DateTimeKind.Utc), slots[2].StartUtc);

        // Running slot generation again with existing start times produces 0 new slots (idempotency)
        foreach (var s in slots)
        {
            existingStartTimes.Add(s.StartUtc);
        }

        var secondRunSlots = SlotGenerationEngine.GenerateSlotsForDateRange(
            mondaySchedule,
            [leaveBlock],
            existingStartTimes,
            testMonday,
            testMonday,
            tz,
            now);

        Assert.Empty(secondRunSlots);
    }
}
