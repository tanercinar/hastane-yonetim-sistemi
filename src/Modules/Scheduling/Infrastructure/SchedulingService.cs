using System.Globalization;
using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Scheduling.Application;
using HospitalManagement.Modules.Scheduling.Domain;
using HospitalManagement.Modules.Scheduling.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Scheduling.Infrastructure;

public sealed class SchedulingService(
    SchedulingDbContext dbContext,
    IAuditEventPublisher auditPublisher,
    TimeProvider timeProvider) : ISchedulingService
{
    private readonly SchedulingDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IAuditEventPublisher _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public async Task<SchedulingOperationResult<DoctorScheduleDto>> CreateDoctorScheduleAsync(
        ClaimsPrincipal actor,
        CreateDoctorScheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!CanManageDoctorSchedule(actor, command.DoctorId))
        {
            return SchedulingOperationResult.Forbidden<DoctorScheduleDto>(
                "Başka bir doktorun çalışma takvimini yönetemezsiniz.");
        }

        if (command.StartTime >= command.EndTime)
        {
            return SchedulingOperationResult.Validation<DoctorScheduleDto>(
                "startTime", "Çalışma başlangıç saati bitiş saatinden önce olmalıdır.");
        }

        if (command.SlotDurationMinutes is < 5 or > 120)
        {
            return SchedulingOperationResult.Validation<DoctorScheduleDto>(
                "slotDurationMinutes", "Randevu süresi 5 ile 120 dakika arasında olmalıdır.");
        }

        var existingOnDay = await _dbContext.DoctorSchedules
            .Include(s => s.Breaks)
            .AnyAsync(s =>
                s.DoctorId == command.DoctorId &&
                s.DayOfWeek == command.DayOfWeek &&
                s.IsActive,
                cancellationToken);

        if (existingOnDay)
        {
            return SchedulingOperationResult.Conflict<DoctorScheduleDto>(
                $"{command.DayOfWeek} günü için doktorun zaten aktif bir çalışma takvimi bulunmaktadır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var schedule = DoctorSchedule.Create(
            Guid.NewGuid(),
            command.DoctorId,
            command.DepartmentId,
            command.DayOfWeek,
            command.StartTime,
            command.EndTime,
            command.SlotDurationMinutes,
            now);

        if (command.Breaks is not null)
        {
            foreach (var b in command.Breaks)
            {
                schedule.AddBreak(Guid.NewGuid(), b.StartTime, b.EndTime, b.Reason);
            }
        }

        _dbContext.DoctorSchedules.Add(schedule);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return SchedulingOperationResult.Conflict<DoctorScheduleDto>(
                $"{command.DayOfWeek} günü için doktorun zaten aktif bir çalışma takvimi bulunmaktadır.");
        }

        await PublishAuditAsync(
            actor,
            "Scheduling.ScheduleCreate",
            schedule.Id.ToString(),
            AuditOutcome.Success,
            $"Doktor çalışma takvimi oluşturuldu: DoctorId={command.DoctorId}, Day={command.DayOfWeek}",
            cancellationToken);

        return SchedulingOperationResult.Success(MapToDto(schedule));
    }

    public async Task<SchedulingOperationResult<IReadOnlyList<DoctorScheduleDto>>> GetDoctorSchedulesAsync(
        ClaimsPrincipal actor,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!CanManageDoctorSchedule(actor, doctorId))
        {
            return SchedulingOperationResult.Forbidden<IReadOnlyList<DoctorScheduleDto>>(
                "Başka bir doktorun çalışma takvimini görüntüleyemezsiniz.");
        }

        var schedules = await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Include(s => s.Breaks)
            .Where(s => s.DoctorId == doctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return SchedulingOperationResult.Success<IReadOnlyList<DoctorScheduleDto>>(
            schedules.Select(MapToDto).ToList());
    }

    public async Task<SchedulingOperationResult<DoctorLeaveBlockDto>> CreateDoctorLeaveBlockAsync(
        ClaimsPrincipal actor,
        CreateDoctorLeaveBlockCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!CanManageDoctorSchedule(actor, command.DoctorId))
        {
            return SchedulingOperationResult.Forbidden<DoctorLeaveBlockDto>(
                "Başka bir doktor için izin veya blokaj oluşturamazsınız.");
        }

        if (command.StartUtc >= command.EndUtc)
        {
            return SchedulingOperationResult.Validation<DoctorLeaveBlockDto>(
                "startUtc", "İzin başlangıç zamanı bitiş zamanından önce olmalıdır.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var leaveBlock = DoctorLeaveBlock.Create(
            Guid.NewGuid(),
            command.DoctorId,
            command.StartUtc,
            command.EndUtc,
            command.Reason,
            now);

        _dbContext.DoctorLeaveBlocks.Add(leaveBlock);

        var affectedSlots = await _dbContext.AppointmentSlots
            .Where(s =>
                s.DoctorId == command.DoctorId &&
                s.StartUtc < command.EndUtc &&
                s.EndUtc > command.StartUtc &&
                s.Status == SlotStatus.Available)
            .ToListAsync(cancellationToken);

        foreach (var slot in affectedSlots)
        {
            slot.Block(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Scheduling.LeaveBlockCreate",
            leaveBlock.Id.ToString(),
            AuditOutcome.Success,
            $"Doktor izin/blokaj kaydı oluşturuldu: DoctorId={command.DoctorId}, {command.StartUtc:s} - {command.EndUtc:s}",
            cancellationToken);

        return SchedulingOperationResult.Success(new DoctorLeaveBlockDto(
            leaveBlock.Id,
            leaveBlock.DoctorId,
            leaveBlock.StartUtc,
            leaveBlock.EndUtc,
            leaveBlock.Reason,
            leaveBlock.IsActive));
    }

    public async Task<SchedulingOperationResult<int>> GenerateDoctorSlotsAsync(
        ClaimsPrincipal actor,
        GenerateDoctorSlotsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!CanManageDoctorSchedule(actor, command.DoctorId))
        {
            return SchedulingOperationResult.Forbidden<int>(
                "Başka bir doktor için slot üretemezsiniz.");
        }

        if (command.StartDate > command.EndDate)
        {
            return SchedulingOperationResult.Validation<int>(
                "startDate", "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(command.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return SchedulingOperationResult.Validation<int>(
                "timeZoneId", "Geçerli bir saat dilimi belirtilmelidir.");
        }
        catch (InvalidTimeZoneException)
        {
            return SchedulingOperationResult.Validation<int>(
                "timeZoneId", "Geçerli bir saat dilimi belirtilmelidir.");
        }

        var schedules = await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Include(s => s.Breaks)
            .Where(s => s.DoctorId == command.DoctorId && s.IsActive)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return SchedulingOperationResult.NotFound<int>("Doktora ait aktif bir çalışma takvimi bulunamadı.");
        }

        var leaveBlocks = await _dbContext.DoctorLeaveBlocks
            .AsNoTracking()
            .Where(l => l.DoctorId == command.DoctorId && l.IsActive)
            .ToListAsync(cancellationToken);

        var startDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(
            command.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZone);
        var endDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(
            command.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Unspecified),
            timeZone);

        var existingSlotStartTimesUtc = (await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == command.DoctorId &&
                s.StartUtc >= startDateTimeUtc &&
                s.StartUtc <= endDateTimeUtc)
            .Select(s => s.StartUtc)
            .ToListAsync(cancellationToken)).ToHashSet();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var allNewSlots = new List<AppointmentSlot>();

        foreach (var schedule in schedules)
        {
            var slots = SlotGenerationEngine.GenerateSlotsForDateRange(
                schedule,
                leaveBlocks,
                existingSlotStartTimesUtc,
                command.StartDate,
                command.EndDate,
                timeZone,
                now);

            allNewSlots.AddRange(slots);
            foreach (var slot in slots)
            {
                existingSlotStartTimesUtc.Add(slot.StartUtc);
            }
        }

        if (allNewSlots.Count > 0)
        {
            _dbContext.AppointmentSlots.AddRange(allNewSlots);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await PublishAuditAsync(
            actor,
            "Scheduling.SlotsGenerate",
            command.DoctorId.ToString(),
            AuditOutcome.Success,
            $"{allNewSlots.Count} adet randevu slotu üretildi ({command.StartDate} - {command.EndDate})",
            cancellationToken);

        return SchedulingOperationResult.Success(allNewSlots.Count);
    }

    public async Task<SchedulingOperationResult<IReadOnlyList<AvailabilityDayDto>>> GetDoctorAvailabilityAsync(
        GetDoctorAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.StartDate > query.EndDate)
        {
            return SchedulingOperationResult.Validation<IReadOnlyList<AvailabilityDayDto>>(
                "startDate", "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(query.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return SchedulingOperationResult.Validation<IReadOnlyList<AvailabilityDayDto>>(
                "timeZoneId", "Geçerli bir saat dilimi belirtilmelidir.");
        }
        catch (InvalidTimeZoneException)
        {
            return SchedulingOperationResult.Validation<IReadOnlyList<AvailabilityDayDto>>(
                "timeZoneId", "Geçerli bir saat dilimi belirtilmelidir.");
        }

        var startDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(
            query.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZone);
        var endDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(
            query.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Unspecified),
            timeZone);

        var slots = await _dbContext.AppointmentSlots
            .AsNoTracking()
            .Where(s =>
                s.DoctorId == query.DoctorId &&
                s.StartUtc >= startDateTimeUtc &&
                s.StartUtc <= endDateTimeUtc &&
                s.Status == SlotStatus.Available)
            .OrderBy(s => s.StartUtc)
            .ToListAsync(cancellationToken);

        var days = slots
            .GroupBy(s => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(s.StartUtc, timeZone)))
            .OrderBy(g => g.Key)
            .Select(g => new AvailabilityDayDto(
                g.Key,
                g.Select(s => new AppointmentSlotDto(
                    s.Id,
                    s.DoctorId,
                    s.DepartmentId,
                    s.StartUtc,
                    s.EndUtc,
                    s.Status,
                    s.HeldByPersonId,
                    s.Version)).ToList()))
            .ToList();

        return SchedulingOperationResult.Success<IReadOnlyList<AvailabilityDayDto>>(days);
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> BookAppointmentAsync(
        ClaimsPrincipal actor,
        BookAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!CanManageAppointments(actor)
            && (!TryGetActorPersonId(actor, out var actorPersonId) || actorPersonId != command.PatientId))
        {
            return SchedulingOperationResult.Forbidden<AppointmentDto>(
                "Yalnız kendi adınıza randevu oluşturabilirsiniz.");
        }

        var slot = await _dbContext.AppointmentSlots
            .FirstOrDefaultAsync(s => s.Id == command.SlotId, cancellationToken);

        if (slot is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu slotu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            slot.Book(command.PatientId, now);

            var appointment = Appointment.Create(
                Guid.NewGuid(),
                slot.Id,
                command.PatientId,
                slot.DoctorId,
                slot.DepartmentId,
                slot.StartUtc,
                command.ReasonForVisit,
                now);

            _dbContext.Appointments.Add(appointment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Appointment.Book",
                appointment.Id.ToString(),
                AuditOutcome.Success,
                $"Randevu oluşturuldu: PatientId={command.PatientId}, SlotId={slot.Id}",
                cancellationToken);

            return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(
                "Seçilen randevu slotu başka bir kullanıcı tarafından rezerve edildi.");
        }
        catch (DbUpdateException)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(
                "Seçilen randevu slotu başka bir kullanıcı tarafından rezerve edildi.");
        }
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> CancelAppointmentAsync(
        ClaimsPrincipal actor,
        CancelAppointmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        if (!CanManageAppointments(actor)
            && (!TryGetActorPersonId(actor, out var actorPersonId) || appointment.PatientId != actorPersonId))
        {
            return SchedulingOperationResult.Forbidden<AppointmentDto>(
                "Başka bir hastanın randevusunu iptal edemezsiniz.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            appointment.Cancel(command.Reason, now);

            var slot = await _dbContext.AppointmentSlots
                .FirstOrDefaultAsync(s => s.Id == appointment.SlotId, cancellationToken);

            if (slot is not null)
            {
                slot.Cancel(now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Appointment.Cancel",
                appointment.Id.ToString(),
                AuditOutcome.Success,
                $"Randevu iptal edildi: PatientId={appointment.PatientId}",
                cancellationToken);

            return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(ex.Message);
        }
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> CheckInAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var dayStartUtc = appointment.AppointmentTimeUtc.Date;
            var dayEndUtc = dayStartUtc.AddDays(1);
            var queueLockKey = CreateQueueLockKey(appointment.DoctorId, dayStartUtc);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({queueLockKey})",
                cancellationToken);

            var nextQueueNumber = await _dbContext.Appointments
                .CountAsync(
                    a => a.DoctorId == appointment.DoctorId
                        && a.AppointmentTimeUtc >= dayStartUtc
                        && a.AppointmentTimeUtc < dayEndUtc
                        && a.QueueNumber.HasValue,
                    cancellationToken) + 1;

            appointment.CheckIn(nextQueueNumber, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Appointment.CheckIn",
                appointment.Id.ToString(),
                AuditOutcome.Success,
                $"Randevu check-in yapıldı: PatientId={appointment.PatientId}, QueueNumber={nextQueueNumber}",
                cancellationToken);

            return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(
                "Randevu başka bir işlem tarafından güncellendi. Lütfen yeniden deneyin.");
        }
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> CompleteAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            appointment.Complete(now);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Appointment.Complete",
                appointment.Id.ToString(),
                AuditOutcome.Success,
                $"Randevu tamamlandı: PatientId={appointment.PatientId}",
                cancellationToken);

            return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(ex.Message);
        }
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> MarkNoShowAppointmentAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            appointment.MarkNoShow(now);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Appointment.NoShow",
                appointment.Id.ToString(),
                AuditOutcome.Success,
                $"Randevu no-show olarak işaretlendi: PatientId={appointment.PatientId}",
                cancellationToken);

            return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
        }
        catch (InvalidOperationException ex)
        {
            return SchedulingOperationResult.Conflict<AppointmentDto>(ex.Message);
        }
    }

    public async Task<SchedulingOperationResult<AppointmentDto>> GetAppointmentByIdAsync(
        ClaimsPrincipal actor,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var appointment = await _dbContext.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        if (!CanManageAppointments(actor)
            && (!TryGetActorPersonId(actor, out var actorPersonId) || appointment.PatientId != actorPersonId))
        {
            return SchedulingOperationResult.NotFound<AppointmentDto>("Randevu bulunamadı.");
        }

        return SchedulingOperationResult.Success(MapToAppointmentDto(appointment));
    }

    public async Task<SchedulingOperationResult<IReadOnlyList<AppointmentDto>>> GetPatientAppointmentsAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!CanManageAppointments(actor)
            && (!TryGetActorPersonId(actor, out var actorPersonId) || patientId != actorPersonId))
        {
            return SchedulingOperationResult.Forbidden<IReadOnlyList<AppointmentDto>>(
                "Başka bir hastanın randevularını görüntüleyemezsiniz.");
        }

        var appointments = await _dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentTimeUtc)
            .ToListAsync(cancellationToken);

        return SchedulingOperationResult.Success<IReadOnlyList<AppointmentDto>>(
            appointments.Select(MapToAppointmentDto).ToList());
    }

    public async Task<SchedulingOperationResult<IReadOnlyList<AppointmentDto>>> GetDailyAppointmentsAsync(
        ClaimsPrincipal actor,
        DateOnly appointmentDate,
        Guid? doctorId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var dayStartUtc = appointmentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var query = _dbContext.Appointments.AsNoTracking()
            .Where(a => a.AppointmentTimeUtc >= dayStartUtc && a.AppointmentTimeUtc < dayEndUtc);

        if (doctorId.HasValue && doctorId.Value != Guid.Empty)
        {
            query = query.Where(a => a.DoctorId == doctorId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(a => a.DepartmentId == departmentId.Value);
        }

        var appointments = await query
            .OrderBy(a => a.AppointmentTimeUtc)
            .ToListAsync(cancellationToken);

        var items = appointments.Select(MapToAppointmentDto).ToList();

        await PublishAuditAsync(
            actor,
            "Appointment.DailyList",
            appointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AuditOutcome.Success,
            $"Günlük randevu listesi sorgulandı: Date={appointmentDate:O}, DoctorId={doctorId}, Count={items.Count}",
            cancellationToken);

        return SchedulingOperationResult.Success<IReadOnlyList<AppointmentDto>>(items);
    }

    private static DoctorScheduleDto MapToDto(DoctorSchedule s) =>
        new(
            s.Id,
            s.DoctorId,
            s.DepartmentId,
            s.DayOfWeek,
            s.StartTime,
            s.EndTime,
            s.SlotDurationMinutes,
            s.IsActive,
            s.Breaks.Select(b => new BreakItemDto(b.StartTime, b.EndTime, b.Reason)).ToList());

    private static AppointmentDto MapToAppointmentDto(Appointment a) =>
        new(
            a.Id,
            a.SlotId,
            a.PatientId,
            a.DoctorId,
            a.DepartmentId,
            a.AppointmentTimeUtc,
            a.Status,
            a.ReasonForVisit,
            a.CancellationReason,
            a.CancelledAtUtc,
            a.CheckedInAtUtc,
            a.CompletedAtUtc,
            a.QueueNumber,
            a.Version);

    private static bool TryGetActorPersonId(ClaimsPrincipal actor, out Guid personId) =>
        Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out personId);

    private static bool CanManageAppointments(ClaimsPrincipal actor) =>
        actor.HasClaim(
            HospitalClaimTypes.Permission,
            HospitalPermissions.Appointment.Manage);

    private static bool CanManageDoctorSchedule(ClaimsPrincipal actor, Guid doctorId)
    {
        if (actor.IsInRole(HospitalRoles.SystemAdministrator)
            || actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)
            || actor.IsInRole(HospitalRoles.RegistrationStaff))
        {
            return true;
        }

        return actor.IsInRole(HospitalRoles.Doctor)
            && TryGetActorPersonId(actor, out var actorPersonId)
            && actorPersonId == doctorId;
    }

    private static long CreateQueueLockKey(Guid doctorId, DateTime dayStartUtc)
    {
        var bytes = doctorId.ToByteArray();
        var doctorKey = BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
        return doctorKey ^ dayStartUtc.Date.Ticks;
    }

    private Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        Guid? actorUserId = null;
        Guid? actorPersonId = null;

        var userIdStr = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var uid))
        {
            actorUserId = uid;
        }

        var personIdStr = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        if (Guid.TryParse(personIdStr, out var pid))
        {
            actorPersonId = pid;
        }

        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "Scheduling",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        return _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
