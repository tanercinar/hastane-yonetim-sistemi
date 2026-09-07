using System.Text.Json;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SurgeryCriticalCare.Application;
using HospitalManagement.Modules.SurgeryCriticalCare.Domain;
using HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.SurgeryCriticalCare.Infrastructure;

public sealed class SurgeryPlanningService : ISurgeryPlanningService
{
    private static readonly SurgeryBookingStatus[] ActiveStatuses =
    [
        SurgeryBookingStatus.Scheduled,
        SurgeryBookingStatus.PreOpCleared,
        SurgeryBookingStatus.InProgress,
    ];

    private readonly SurgeryDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public SurgeryPlanningService(
        SurgeryDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<List<OperatingRoomDto>> GetOperatingRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _dbContext.OperatingRooms
            .AsNoTracking()
            .OrderBy(r => r.RoomCode)
            .ToListAsync(cancellationToken);

        return rooms.Select(r => new OperatingRoomDto(
            r.Id,
            r.RoomCode,
            r.RoomName,
            r.IsActive,
            r.Capacity,
            r.SpecialtyDepartmentId)).ToList();
    }

    public async Task<SurgeryOperationResult<SurgeryBookingDto>> CreateBookingAsync(
        CreateSurgeryBookingDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (dto.DepartmentId == Guid.Empty || string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("Department", "Cerrahi anabilim dalı / bölüm seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.ProcedureName))
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("ProcedureName", "Ameliyat / işlem adı boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(dto.ProcedureCode))
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("ProcedureCode", "İşlem kodu boş olamaz.");
        }

        if (dto.OperatingRoomId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("OperatingRoomId", "Ameliyathane salonu seçilmelidir.");
        }

        if (dto.LeadSurgeonDoctorId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("LeadSurgeonDoctorId", "Sorumlu cerrah seçilmelidir.");
        }

        if (dto.AnesthesiologistDoctorId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("AnesthesiologistDoctorId", "Anestezi hekimi seçilmelidir.");
        }

        if (dto.LeadSurgeonDoctorId == dto.AnesthesiologistDoctorId)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("AnesthesiologistDoctorId", "Sorumlu cerrah ve anestezi hekimi aynı kişi olamaz.");
        }

        if (dto.ScheduledStartTimeUtc >= dto.ScheduledEndTimeUtc)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("ScheduledStartTimeUtc", "Ameliyat başlangıç zamanı bitiş zamanından önce olmalıdır.");
        }

        var room = await _dbContext.OperatingRooms.FirstOrDefaultAsync(r => r.Id == dto.OperatingRoomId, cancellationToken);
        if (room is null || !room.IsActive)
        {
            return SurgeryOperationResult.NotFound<SurgeryBookingDto>("Seçilen ameliyathane salonu bulunamadı veya aktif değil.");
        }

        // --- ATOMIC CONFLICT CHECKS ---
        // 1. Room Conflict Check
        var roomConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.OperatingRoomId == dto.OperatingRoomId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (roomConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Seçilen ameliyathanede ({room.RoomCode} - {room.RoomName}) bu zaman aralığında planlanmış başka bir aktif ameliyat ({roomConflict.BookingProtocolNumber} - {roomConflict.ProcedureName}) bulunmaktadır.");
        }

        // 2. Lead Surgeon Conflict Check
        var surgeonConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.LeadSurgeonDoctorId == dto.LeadSurgeonDoctorId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (surgeonConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Sorumlu cerrah hekim bu zaman aralığında başka bir ameliyatta ({surgeonConflict.BookingProtocolNumber} - {surgeonConflict.ProcedureName}) görevlidir.");
        }

        // 3. Anesthesiologist Conflict Check
        var anesthesiologistConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.AnesthesiologistDoctorId == dto.AnesthesiologistDoctorId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (anesthesiologistConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Anestezi hekimi bu zaman aralığında başka bir ameliyatta ({anesthesiologistConflict.BookingProtocolNumber} - {anesthesiologistConflict.ProcedureName}) görevlidir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var booking = SurgeryBooking.Create(
            Guid.NewGuid(),
            dto.PatientId,
            dto.EncounterId,
            dto.DepartmentId,
            dto.DepartmentName,
            dto.ProcedureName,
            dto.ProcedureCode,
            dto.Urgency,
            dto.OperatingRoomId,
            dto.LeadSurgeonDoctorId,
            dto.AnesthesiologistDoctorId,
            dto.OperatingNurseStaffId,
            dto.ScheduledStartTimeUtc,
            dto.ScheduledEndTimeUtc,
            dto.ClinicalNotes,
            now);

        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.BookingCreate",
            booking.Id.ToString(),
            "Ameliyat planlandı",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                RoomId = booking.OperatingRoomId,
                StartTime = booking.ScheduledStartTimeUtc,
                EndTime = booking.ScheduledEndTimeUtc,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(booking));
    }

    public async Task<SurgeryOperationResult<SurgeryBookingDto>> RescheduleBookingAsync(
        Guid bookingId,
        RescheduleSurgeryBookingDto dto,
        Guid requestingStaffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.OperatingRoomId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("OperatingRoomId", "Ameliyathane salonu seçilmelidir.");
        }

        if (dto.ScheduledStartTimeUtc >= dto.ScheduledEndTimeUtc)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("ScheduledStartTimeUtc", "Ameliyat başlangıç zamanı bitiş zamanından önce olmalıdır.");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return SurgeryOperationResult.NotFound<SurgeryBookingDto>("Ameliyat randevusu bulunamadı.");
        }

        if (booking.Status is SurgeryBookingStatus.InProgress or SurgeryBookingStatus.Completed or SurgeryBookingStatus.Cancelled)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>($"'{booking.Status}' durumundaki ameliyat yeniden planlanamaz.");
        }

        var room = await _dbContext.OperatingRooms.FirstOrDefaultAsync(r => r.Id == dto.OperatingRoomId, cancellationToken);
        if (room is null || !room.IsActive)
        {
            return SurgeryOperationResult.NotFound<SurgeryBookingDto>("Seçilen ameliyathane salonu bulunamadı veya aktif değil.");
        }

        // Conflict check excluding current booking
        var roomConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Id != bookingId
                        && b.OperatingRoomId == dto.OperatingRoomId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (roomConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Seçilen ameliyathanede ({room.RoomCode}) bu zaman aralığında çakışan başka bir aktif ameliyat ({roomConflict.BookingProtocolNumber}) bulunmaktadır.");
        }

        var surgeonConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Id != bookingId
                        && b.LeadSurgeonDoctorId == booking.LeadSurgeonDoctorId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (surgeonConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Sorumlu cerrah hekim bu zaman aralığında başka bir ameliyatta görevlidir.");
        }

        var anesthesiologistConflict = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Id != bookingId
                        && b.AnesthesiologistDoctorId == booking.AnesthesiologistDoctorId
                        && ActiveStatuses.Contains(b.Status)
                        && dto.ScheduledStartTimeUtc < b.ScheduledEndTimeUtc
                        && dto.ScheduledEndTimeUtc > b.ScheduledStartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (anesthesiologistConflict is not null)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(
                $"Anestezi hekimi bu zaman aralığında başka bir ameliyatta görevlidir.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            booking.Reschedule(dto.OperatingRoomId, dto.ScheduledStartTimeUtc, dto.ScheduledEndTimeUtc, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("Reschedule", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.BookingReschedule",
            booking.Id.ToString(),
            "Ameliyat yeniden planlandı",
            requestingStaffId,
            JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                RoomId = booking.OperatingRoomId,
                NewStart = booking.ScheduledStartTimeUtc,
                NewEnd = booking.ScheduledEndTimeUtc,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(booking));
    }

    public async Task<SurgeryOperationResult<SurgeryBookingDto>> RecordPreOpChecklistAsync(
        Guid bookingId,
        RecordPreOpChecklistDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (staffId == Guid.Empty)
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("StaffId", "İşlemi yapan personel seçilmelidir.");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return SurgeryOperationResult.NotFound<SurgeryBookingDto>("Ameliyat randevusu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var checklistInfo = new PreOpChecklistInfo(
            dto.ConsentSigned,
            dto.AnesthesiaClearance,
            dto.NpoConfirmed,
            dto.BloodProductsReserved,
            dto.SiteMarked,
            dto.AllergyChecked,
            staffId,
            now,
            dto.Notes);

        try
        {
            booking.RecordPreOpChecklist(checklistInfo, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.PreOpChecklistComplete",
            booking.Id.ToString(),
            "Ameliyat öncesi kontrol listesi kaydedildi",
            staffId,
            JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
                IsFullyCleared = checklistInfo.IsFullyCleared,
                ConsentSigned = dto.ConsentSigned,
                AnesthesiaClearance = dto.AnesthesiaClearance,
                NpoConfirmed = dto.NpoConfirmed,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(booking));
    }

    public async Task<SurgeryOperationResult<SurgeryBookingDto>> CancelBookingAsync(
        Guid bookingId,
        string reason,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SurgeryOperationResult.Validation<SurgeryBookingDto>("Reason", "İptal gerekçesi boş olamaz.");
        }

        var booking = await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return SurgeryOperationResult.NotFound<SurgeryBookingDto>("Ameliyat randevusu bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            booking.Cancel(reason, now);
        }
        catch (Exception ex)
        {
            return SurgeryOperationResult.Conflict<SurgeryBookingDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Surgery.BookingCancel",
            booking.Id.ToString(),
            "Ameliyat randevusu iptal edildi",
            staffId,
            JsonSerializer.Serialize(new
            {
                BookingId = booking.Id,
            }),
            now,
            cancellationToken);

        return SurgeryOperationResult.Success(MapToDto(booking));
    }

    public async Task<SurgeryBookingDto?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _dbContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        return booking is null ? null : MapToDto(booking);
    }

    public async Task<List<SurgeryBookingDto>> GetBookingsAsync(
        Guid? operatingRoomId = null,
        Guid? leadSurgeonId = null,
        DateTime? fromDateUtc = null,
        DateTime? toDateUtc = null,
        SurgeryBookingStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Bookings.AsNoTracking().AsQueryable();

        if (operatingRoomId.HasValue)
        {
            query = query.Where(b => b.OperatingRoomId == operatingRoomId.Value);
        }

        if (leadSurgeonId.HasValue)
        {
            query = query.Where(b => b.LeadSurgeonDoctorId == leadSurgeonId.Value);
        }

        if (fromDateUtc.HasValue)
        {
            query = query.Where(b => b.ScheduledEndTimeUtc >= fromDateUtc.Value);
        }

        if (toDateUtc.HasValue)
        {
            query = query.Where(b => b.ScheduledStartTimeUtc <= toDateUtc.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var list = await query
            .OrderBy(b => b.ScheduledStartTimeUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    private static SurgeryBookingDto MapToDto(SurgeryBooking b) =>
        new(
            b.Id,
            b.BookingProtocolNumber,
            b.PatientId,
            b.EncounterId,
            b.DepartmentId,
            b.DepartmentName,
            b.ProcedureName,
            b.ProcedureCode,
            b.Urgency,
            b.OperatingRoomId,
            b.LeadSurgeonDoctorId,
            b.AnesthesiologistDoctorId,
            b.OperatingNurseStaffId,
            b.ScheduledStartTimeUtc,
            b.ScheduledEndTimeUtc,
            b.Status,
            b.PreOpChecklist is null ? null : new PreOpChecklistDto(
                b.PreOpChecklist.ConsentSigned,
                b.PreOpChecklist.AnesthesiaClearance,
                b.PreOpChecklist.NpoConfirmed,
                b.PreOpChecklist.BloodProductsReserved,
                b.PreOpChecklist.SiteMarked,
                b.PreOpChecklist.AllergyChecked,
                b.PreOpChecklist.IsFullyCleared,
                b.PreOpChecklist.CompletedByStaffId,
                b.PreOpChecklist.CompletedAtUtc,
                b.PreOpChecklist.Notes),
            b.ClinicalNotes,
            b.CancellationReason,
            b.CreatedAtUtc,
            b.UpdatedAtUtc,
            b.Version);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        string reason,
        Guid actorUserId,
        string? detailsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: actorUserId != Guid.Empty ? actorUserId : null,
            ActorPersonId: null,
            ActorRole: null,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "SurgeryBooking",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: detailsJson);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
