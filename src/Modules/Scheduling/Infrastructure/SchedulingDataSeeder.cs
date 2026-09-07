using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.Scheduling.Infrastructure;

public sealed partial class SchedulingDataSeeder(
    SchedulingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<SchedulingDataSeeder> logger) : ISchedulingDataSeeder
{
    private readonly SchedulingDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILogger<SchedulingDataSeeder> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Deterministik DEMO doktor takvimleri ve slotları başarıyla seed edildi.")]
    private static partial void LogSeedCompleted(ILogger logger);

    public static readonly Guid DemoDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid DemoDepartmentId = Guid.Parse("30000000-0000-0000-0000-000000000003");

    private static readonly Guid LegacyDemoDepartmentId =
        Guid.Parse("00000000-0000-0000-0000-000000000301");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        await MigrateLegacyDemoDepartmentReferencesAsync(cancellationToken);

        var existingSchedules = await _dbContext.DoctorSchedules.AnyAsync(cancellationToken);
        if (!existingSchedules)
        {
            var days = new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
            };

            foreach (var day in days)
            {
                var schedule = DoctorSchedule.Create(
                    Guid.NewGuid(),
                    DemoDoctorId,
                    DemoDepartmentId,
                    day,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0),
                    slotDurationMinutes: 20,
                    now);

                schedule.AddBreak(
                    Guid.NewGuid(),
                    new TimeOnly(12, 30),
                    new TimeOnly(13, 30),
                    "Öğle Molası");

                _dbContext.DoctorSchedules.Add(schedule);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Generate slots for next 14 days if none exist
        var existingSlots = await _dbContext.AppointmentSlots.AnyAsync(cancellationToken);
        if (!existingSlots)
        {
            var schedules = await _dbContext.DoctorSchedules
                .Include(s => s.Breaks)
                .Where(s => s.DoctorId == DemoDoctorId && s.IsActive)
                .ToListAsync(cancellationToken);

            var today = DateOnly.FromDateTime(now);
            var endDate = today.AddDays(14);
            var tz = TimeZoneInfo.Utc;

            var existingSlotStartTimes = new HashSet<DateTime>();
            var newSlots = new List<AppointmentSlot>();

            foreach (var schedule in schedules)
            {
                var slots = SlotGenerationEngine.GenerateSlotsForDateRange(
                    schedule,
                    [],
                    existingSlotStartTimes,
                    today,
                    endDate,
                    tz,
                    now);

                newSlots.AddRange(slots);
                foreach (var s in slots)
                {
                    existingSlotStartTimes.Add(s.StartUtc);
                }
            }

            if (newSlots.Count > 0)
            {
                _dbContext.AppointmentSlots.AddRange(newSlots);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        LogSeedCompleted(_logger);
    }

    private async Task MigrateLegacyDemoDepartmentReferencesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.DoctorSchedules
            .Where(schedule => schedule.DoctorId == DemoDoctorId
                && schedule.DepartmentId == LegacyDemoDepartmentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    schedule => schedule.DepartmentId,
                    DemoDepartmentId),
                cancellationToken);

        await _dbContext.AppointmentSlots
            .Where(slot => slot.DoctorId == DemoDoctorId
                && slot.DepartmentId == LegacyDemoDepartmentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    slot => slot.DepartmentId,
                    DemoDepartmentId),
                cancellationToken);

        await _dbContext.Appointments
            .Where(appointment => appointment.DoctorId == DemoDoctorId
                && appointment.DepartmentId == LegacyDemoDepartmentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    appointment => appointment.DepartmentId,
                    DemoDepartmentId),
                cancellationToken);
    }
}
