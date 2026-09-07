using System.Security.Claims;
using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class BedManagementService : IBedManagementService
{
    private readonly InpatientDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public BedManagementService(
        InpatientDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<InpatientOperationResult<IReadOnlyList<WardDto>>> GetWardsAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        var wards = await query.OrderBy(w => w.Name).ToListAsync(cancellationToken);

        var list = wards.Select(MapToWardDto).ToList();
        return InpatientOperationResult.Success<IReadOnlyList<WardDto>>(list);
    }

    public async Task<InpatientOperationResult<WardDto>> GetWardByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return InpatientOperationResult.Validation<WardDto>("Id", "Geçerli bir servis kimliği belirtilmelidir.");
        }

        var ward = await _dbContext.Wards
            .AsNoTracking()
            .Include(w => w.Rooms)
                .ThenInclude(r => r.Beds)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (ward is null)
        {
            return InpatientOperationResult.NotFound<WardDto>($"'{id}' kimlikli servis bulunamadı.");
        }

        return InpatientOperationResult.Success(MapToWardDto(ward));
    }

    public async Task<InpatientOperationResult<IReadOnlyList<RoomDto>>> GetRoomsByWardIdAsync(
        Guid wardId,
        CancellationToken cancellationToken = default)
    {
        if (wardId == Guid.Empty)
        {
            return InpatientOperationResult.Validation<IReadOnlyList<RoomDto>>("WardId", "Geçerli bir servis kimliği belirtilmelidir.");
        }

        var rooms = await _dbContext.Rooms
            .AsNoTracking()
            .Include(r => r.Beds)
            .Where(r => r.WardId == wardId)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync(cancellationToken);

        var list = rooms.Select(MapToRoomDto).ToList();
        return InpatientOperationResult.Success<IReadOnlyList<RoomDto>>(list);
    }

    public async Task<InpatientOperationResult<IReadOnlyList<BedDto>>> GetBedsAsync(
        Guid? wardId = null,
        Guid? roomId = null,
        BedStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Beds
            .AsNoTracking()
            .AsQueryable();

        if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            query = query.Where(b => b.WardId == wardId.Value);
        }

        if (roomId.HasValue && roomId.Value != Guid.Empty)
        {
            query = query.Where(b => b.RoomId == roomId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        var beds = await query.OrderBy(b => b.BedNumber).ToListAsync(cancellationToken);
        var list = beds.Select(MapToBedDto).ToList();
        return InpatientOperationResult.Success<IReadOnlyList<BedDto>>(list);
    }

    public async Task<InpatientOperationResult<BedDto>> GetBedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return InpatientOperationResult.Validation<BedDto>("Id", "Geçerli bir yatak kimliği belirtilmelidir.");
        }

        var bed = await _dbContext.Beds
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (bed is null)
        {
            return InpatientOperationResult.NotFound<BedDto>($"'{id}' kimlikli yatak bulunamadı.");
        }

        return InpatientOperationResult.Success(MapToBedDto(bed));
    }

    public async Task<InpatientOperationResult<BedDto>> UpdateBedStatusAsync(
        ClaimsPrincipal actor,
        Guid bedId,
        BedStatus newStatus,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (bedId == Guid.Empty)
        {
            return InpatientOperationResult.Validation<BedDto>("BedId", "Geçerli bir yatak kimliği belirtilmelidir.");
        }

        var bed = await _dbContext.Beds
            .SingleOrDefaultAsync(b => b.Id == bedId, cancellationToken);

        if (bed is null)
        {
            return InpatientOperationResult.NotFound<BedDto>($"'{bedId}' kimlikli yatak bulunamadı.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var oldStatus = bed.Status;
        string auditAction = "Inpatient.BedStatusChange";

        try
        {
            switch (newStatus)
            {
                case BedStatus.Available when oldStatus == BedStatus.Cleaning:
                    bed.CompleteCleaning(nowUtc);
                    auditAction = "Inpatient.BedCleaningComplete";
                    break;

                case BedStatus.Available when oldStatus == BedStatus.Maintenance:
                    bed.RestoreFromMaintenance(nowUtc);
                    auditAction = "Inpatient.BedMaintenanceRestore";
                    break;

                case BedStatus.Maintenance:
                    bed.MarkUnderMaintenance(reason ?? "Rutin bakım ve onarım", nowUtc);
                    auditAction = "Inpatient.BedMaintenance";
                    break;

                case BedStatus.Cleaning when oldStatus == BedStatus.Available:
                    bed.Reserve(Guid.NewGuid(), Guid.NewGuid(), nowUtc); // Temporary transition
                    bed.ReleaseBed(nowUtc, requireCleaning: true);
                    auditAction = "Inpatient.BedCleaningStart";
                    break;

                default:
                    return InpatientOperationResult.Validation<BedDto>(
                        "Status",
                        $"Geçersiz durum geçişi: '{oldStatus}' -> '{newStatus}'.");
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InpatientOperationResult.Conflict<BedDto>(
                "Yatak durumu başka bir işlem tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyiniz.");
        }
        catch (InvalidOperationException ex)
        {
            return InpatientOperationResult.Validation<BedDto>("Status", ex.Message);
        }

        await PublishAuditAsync(
            actor,
            auditAction,
            "Bed",
            bed.Id.ToString(),
            $"Yatak durumu '{oldStatus}' -> '{newStatus}' olarak güncellendi. Gerekçe: {reason ?? "-"}",
            nowUtc,
            cancellationToken);

        return InpatientOperationResult.Success(MapToBedDto(bed));
    }

    public async Task<InpatientOperationResult<BedOccupancySummaryDto>> GetOccupancySummaryAsync(
        Guid? wardId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Beds
            .AsNoTracking()
            .Where(b => b.IsActive)
            .AsQueryable();

        if (wardId.HasValue && wardId.Value != Guid.Empty)
        {
            query = query.Where(b => b.WardId == wardId.Value);
        }

        var beds = await query.ToListAsync(cancellationToken);

        var total = beds.Count;
        var available = beds.Count(b => b.Status == BedStatus.Available);
        var occupied = beds.Count(b => b.Status == BedStatus.Occupied);
        var cleaning = beds.Count(b => b.Status == BedStatus.Cleaning);
        var maintenance = beds.Count(b => b.Status == BedStatus.Maintenance);
        var reserved = beds.Count(b => b.Status == BedStatus.Reserved);

        double rate = total > 0 ? Math.Round((double)occupied / total * 100.0, 2) : 0.0;

        var dto = new BedOccupancySummaryDto(total, available, occupied, cleaning, maintenance, reserved, rate);
        return InpatientOperationResult.Success(dto);
    }

    private static WardDto MapToWardDto(Ward w)
    {
        var allBeds = w.Rooms.SelectMany(r => r.Beds).ToList();
        return new WardDto(
            w.Id,
            w.Code,
            w.Name,
            w.DepartmentId,
            w.Building,
            w.Floor,
            w.WardType,
            w.IsActive,
            w.Rooms.Count,
            allBeds.Count,
            allBeds.Count(b => b.Status == BedStatus.Available));
    }

    private static RoomDto MapToRoomDto(Room r) =>
        new(
            r.Id,
            r.WardId,
            r.RoomNumber,
            r.GenderConstraint,
            r.IsolationType,
            r.IsNegativePressure,
            r.IsActive,
            r.Beds.Select(MapToBedDto).ToList());

    private static BedDto MapToBedDto(Bed b) =>
        new(
            b.Id,
            b.WardId,
            b.RoomId,
            b.BedNumber,
            b.Status,
            b.CurrentAdmissionId,
            b.CurrentPatientId,
            b.GenderConstraint,
            b.IsolationType,
            b.HasTelemetry,
            b.HasOxygen,
            b.HasVentilator,
            b.MaintenanceReason,
            b.IsActive,
            b.Version);

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceType,
        string targetResourceId,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var actorUserId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value;

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: Guid.TryParse(actorUserId, out var uid) ? uid : null,
            ActorPersonId: null,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: targetResourceType,
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
