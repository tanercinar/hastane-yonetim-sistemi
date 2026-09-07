using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.SpecialtyCare.Application;
using HospitalManagement.Modules.SpecialtyCare.Domain.HomeHealth;
using HospitalManagement.Modules.SpecialtyCare.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HospitalManagement.Modules.SpecialtyCare.Infrastructure;

public sealed class HomeHealthCareService : IHomeHealthCareService
{
    private const string EncounterUniqueConstraint = "UX_HomeHealthVisits_EncounterId";
    private readonly SpecialtyCareDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public HomeHealthCareService(
        SpecialtyCareDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SpecialtyOperationResult<HomeHealthVisitDto>> RequestVisitAsync(
        RequestHomeHealthVisitDto dto,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.PatientId == Guid.Empty)
        {
            return SpecialtyOperationResult.Validation<HomeHealthVisitDto>("PatientId", "Hasta seçilmelidir.");
        }

        if (string.IsNullOrWhiteSpace(dto.City) || string.IsNullOrWhiteSpace(dto.District) || string.IsNullOrWhiteSpace(dto.AddressDetail))
        {
            return SpecialtyOperationResult.Validation<HomeHealthVisitDto>("Address", "İl, ilçe ve açık adres bilgisi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(dto.ContactPhone))
        {
            return SpecialtyOperationResult.Validation<HomeHealthVisitDto>("ContactPhone", "İletişim telefonu zorunludur.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        HomeHealthVisit visit;
        try
        {
            visit = HomeHealthVisit.Request(
                Guid.NewGuid(),
                dto.PatientId,
                dto.ServiceType,
                dto.Priority,
                dto.City,
                dto.District,
                dto.AddressDetail,
                dto.ContactPhone,
                staffId,
                dto.InitialNotes,
                now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Validation<HomeHealthVisitDto>("HomeHealthVisit", ex.Message);
        }

        _dbContext.HomeHealthVisits.Add(visit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.HomeHealthVisitRequest",
            visit.Id.ToString(),
            staffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(visit));
    }

    public async Task<SpecialtyOperationResult<HomeHealthVisitDto>> AssignTeamAsync(
        Guid visitId,
        Guid assignedStaffId,
        DateTime scheduledDateUtc,
        Guid actorStaffId,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.HomeHealthVisits
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

        if (visit is null)
        {
            return SpecialtyOperationResult.NotFound<HomeHealthVisitDto>("Evde sağlık ziyareti bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            visit.AssignTeam(assignedStaffId, scheduledDateUtc, now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Conflict<HomeHealthVisitDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.HomeHealthVisitAssign",
            visit.Id.ToString(),
            actorStaffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(visit));
    }

    public async Task<SpecialtyOperationResult<HomeHealthVisitDto>> StartVisitAsync(
        Guid visitId,
        Guid actorStaffId,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.HomeHealthVisits
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

        if (visit is null)
        {
            return SpecialtyOperationResult.NotFound<HomeHealthVisitDto>("Evde sağlık ziyareti bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            visit.StartVisit(now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Conflict<HomeHealthVisitDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.HomeHealthVisitStart",
            visit.Id.ToString(),
            actorStaffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(visit));
    }

    public async Task<SpecialtyOperationResult<HomeHealthVisitDto>> CompleteVisitAsync(
        Guid visitId,
        string clinicalNotes,
        string? vitalsSummaryNotes,
        Guid encounterId,
        Guid actorStaffId,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.HomeHealthVisits
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

        if (visit is null)
        {
            return SpecialtyOperationResult.NotFound<HomeHealthVisitDto>("Evde sağlık ziyareti bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            visit.CompleteVisit(clinicalNotes, vitalsSummaryNotes, encounterId, now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Conflict<HomeHealthVisitDto>(ex.Message);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsEncounterConflict(ex))
        {
            return SpecialtyOperationResult.Conflict<HomeHealthVisitDto>(
                "Klinik karşılaşma başka bir evde sağlık ziyaretine bağlanmıştır.");
        }

        await PublishAuditAsync(
            "Specialty.HomeHealthVisitComplete",
            visit.Id.ToString(),
            actorStaffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(visit));
    }

    public async Task<SpecialtyOperationResult<HomeHealthVisitDto>> CancelVisitAsync(
        Guid visitId,
        string reason,
        Guid actorStaffId,
        CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.HomeHealthVisits
            .FirstOrDefaultAsync(v => v.Id == visitId, cancellationToken);

        if (visit is null)
        {
            return SpecialtyOperationResult.NotFound<HomeHealthVisitDto>("Evde sağlık ziyareti bulunamadı.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            visit.Cancel(reason, now);
        }
        catch (Exception ex)
        {
            return SpecialtyOperationResult.Conflict<HomeHealthVisitDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            "Specialty.HomeHealthVisitCancel",
            visit.Id.ToString(),
            actorStaffId,
            now,
            cancellationToken);

        return SpecialtyOperationResult.Success(MapToDto(visit));
    }

    public async Task<HomeHealthVisitDto?> GetVisitByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var visit = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return visit is null ? null : MapToDto(visit);
    }

    public async Task<List<HomeHealthVisitDto>> GetActiveVisitsAsync(CancellationToken cancellationToken = default)
    {
        var visits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .Where(v => v.Status == HomeVisitStatus.Requested ||
                        v.Status == HomeVisitStatus.Approved ||
                        v.Status == HomeVisitStatus.Assigned ||
                        v.Status == HomeVisitStatus.InProgress)
            .OrderBy(v => v.Priority)
            .ThenBy(v => v.RequestedDateUtc)
            .ToListAsync(cancellationToken);

        return visits.Select(MapToDto).ToList();
    }

    public async Task<List<HomeHealthVisitDto>> GetVisitsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var visits = await _dbContext.HomeHealthVisits
            .AsNoTracking()
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RequestedDateUtc)
            .ToListAsync(cancellationToken);

        return visits.Select(MapToDto).ToList();
    }

    private static HomeHealthVisitDto MapToDto(HomeHealthVisit v) =>
        new(
            v.Id,
            v.PatientId,
            v.EncounterId,
            v.ProtocolNumber,
            v.ServiceType,
            v.Priority,
            v.Status,
            v.RequestedDateUtc,
            v.ScheduledDateUtc,
            v.VisitStartedAtUtc,
            v.VisitCompletedAtUtc,
            v.City,
            v.District,
            v.AddressDetail,
            v.ContactPhone,
            v.RequestedByStaffId,
            v.AssignedStaffId,
            v.ClinicalNotes,
            v.VitalsSummaryNotes,
            v.CreatedAtUtc,
            v.UpdatedAtUtc);

    private async Task PublishAuditAsync(
        string action,
        string targetResourceId,
        Guid actorUserId,
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
            TargetResourceType: "HomeHealth",
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: "Uzmanlık klinik kayıt eylemi tamamlandı.",
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static bool IsEncounterConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: EncounterUniqueConstraint,
        };
}
