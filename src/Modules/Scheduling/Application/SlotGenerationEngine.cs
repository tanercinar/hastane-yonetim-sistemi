using HospitalManagement.Modules.Scheduling.Domain;

namespace HospitalManagement.Modules.Scheduling.Application;

public static class SlotGenerationEngine
{
    public static IReadOnlyList<AppointmentSlot> GenerateSlotsForDateRange(
        DoctorSchedule schedule,
        IReadOnlyList<DoctorLeaveBlock> leaveBlocks,
        IReadOnlyCollection<DateTime> existingSlotStartTimesUtc,
        DateOnly startDate,
        DateOnly endDate,
        TimeZoneInfo timeZone,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(leaveBlocks);
        ArgumentNullException.ThrowIfNull(existingSlotStartTimesUtc);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (startDate > endDate)
        {
            return [];
        }

        var generatedSlots = new List<AppointmentSlot>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != schedule.DayOfWeek)
            {
                continue;
            }

            var slotDuration = TimeSpan.FromMinutes(schedule.SlotDurationMinutes);
            var currentStartTime = schedule.StartTime;

            while (currentStartTime.Add(slotDuration) <= schedule.EndTime)
            {
                var currentEndTime = currentStartTime.Add(slotDuration);

                // Check if slot falls into any break
                var overlapsBreak = schedule.Breaks.Any(b =>
                    currentStartTime < b.EndTime && currentEndTime > b.StartTime);

                if (!overlapsBreak)
                {
                    // Convert local Date + TimeOnly to DateTime in the specified timezone
                    var localStartDateTime = date.ToDateTime(currentStartTime, DateTimeKind.Unspecified);
                    var localEndDateTime = date.ToDateTime(currentEndTime, DateTimeKind.Unspecified);

                    var slotStartUtc = TimeZoneInfo.ConvertTimeToUtc(localStartDateTime, timeZone);
                    var slotEndUtc = TimeZoneInfo.ConvertTimeToUtc(localEndDateTime, timeZone);

                    // Slot must be in future (or now)
                    if (slotStartUtc >= nowUtc)
                    {
                        // Check if doctor is on leave during this slot
                        var overlapsLeave = leaveBlocks.Any(l =>
                            l.IsActive && slotStartUtc < l.EndUtc && slotEndUtc > l.StartUtc);

                        if (!overlapsLeave)
                        {
                            // Check if slot already exists in DB
                            if (!existingSlotStartTimesUtc.Contains(slotStartUtc))
                            {
                                generatedSlots.Add(AppointmentSlot.Create(
                                    Guid.NewGuid(),
                                    schedule.DoctorId,
                                    schedule.DepartmentId,
                                    schedule.Id,
                                    slotStartUtc,
                                    slotEndUtc,
                                    nowUtc));
                            }
                        }
                    }
                }

                currentStartTime = currentStartTime.Add(slotDuration);
            }
        }

        return generatedSlots;
    }
}
