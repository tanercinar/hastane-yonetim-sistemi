using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class SpecimenService : ISpecimenService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public SpecimenService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        IDiagnosticsAccessContext accessContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> CollectAsync(
        ClaimsPrincipal actor,
        CollectSpecimenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var actorUserId = GetActorUserId(actor);
        var actorRole = GetActorRole(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var order = await _dbContext.DiagnosticOrders
            .FirstOrDefaultAsync(o => o.Id == command.DiagnosticOrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Tanısal tetkik istemi bulunamadı.");
        }

        if (order.OrderType is not DiagnosticOrderType.Laboratory and not DiagnosticOrderType.Pathology)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>(
                "DiagnosticOrder",
                "Yalnızca laboratuvar veya patoloji istemi için numune toplanabilir.");
        }

        if (!await CanAccessOrderAsync(
                actor,
                order,
                HospitalPermissions.Diagnostics.LaboratorySpecimenTransition,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>(
                "Bu istem için numune işlemi yapma atamanız bulunmamaktadır.");
        }

        if (order.Status == DiagnosticOrderStatus.Cancelled || order.Status == DiagnosticOrderStatus.EnteredInError)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>(
                "DiagnosticOrder",
                "İptal edilmiş veya hatalı girilmiş istemler için numune toplanamaz.");
        }

        if (order.PatientId != command.PatientId)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>(
                "PatientId",
                "İstem hasta kimliği ile numune hasta kimliği uyuşmuyor (Yanlış hasta koruması).");
        }

        var barcode = await GenerateUniqueBarcodeAsync(nowUtc, cancellationToken);

        var specimen = Specimen.Collect(
            Guid.NewGuid(),
            barcode,
            command.DiagnosticOrderId,
            command.PatientId,
            command.SpecimenType,
            command.ContainerType,
            actorUserId,
            actorRole,
            command.CollectionLocation,
            command.CollectionNotes,
            nowUtc);

        _dbContext.Specimens.Add(specimen);

        // Also if order was in Placed state, advance to InProgress
        if (order.Status == DiagnosticOrderStatus.Placed)
        {
            order.StartProcessing(nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.SpecimenCollect",
            "Specimen",
            specimen.Id.ToString(),
            $"Numune alındı ve barkod üretildi: {specimen.Barcode} ({specimen.SpecimenType})",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen));
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> MarkInTransitAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        TransitSpecimenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var actorUserId = GetActorUserId(actor);
        var actorRole = GetActorRole(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var specimen = await _dbContext.Specimens
            .Include(s => s.Transitions)
            .FirstOrDefaultAsync(s => s.Id == specimenId, cancellationToken);

        if (specimen is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Numune bulunamadı.");
        }

        if (!await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratorySpecimenTransition, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>();
        }

        try
        {
            var transition = specimen.MarkInTransit(actorUserId, actorRole, command.Location, command.Notes, nowUtc);
            _dbContext.SpecimenTransitionEvents.Add(transition);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<SpecimenDetailDto>("Numune kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.SpecimenTransit",
            "Specimen",
            specimen.Id.ToString(),
            $"Numune taşımaya verildi: {specimen.Barcode} (Konum: {command.Location})",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen));
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> ReceiveAtLabAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        ReceiveSpecimenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var actorUserId = GetActorUserId(actor);
        var actorRole = GetActorRole(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var specimen = await _dbContext.Specimens
            .Include(s => s.Transitions)
            .FirstOrDefaultAsync(s => s.Id == specimenId, cancellationToken);

        if (specimen is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Numune bulunamadı.");
        }

        if (!await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratorySpecimenTransition, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>();
        }

        try
        {
            var transition = specimen.ReceiveAtLab(actorUserId, actorRole, command.Location, command.Notes, nowUtc);
            _dbContext.SpecimenTransitionEvents.Add(transition);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<SpecimenDetailDto>("Numune kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.SpecimenReceive",
            "Specimen",
            specimen.Id.ToString(),
            $"Numune laboratuvarda kabul edildi: {specimen.Barcode}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen));
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> RejectAsync(
        ClaimsPrincipal actor,
        Guid specimenId,
        RejectSpecimenCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>("RejectionReason", "Numune red gerekçesi zorunludur.");
        }

        var actorUserId = GetActorUserId(actor);
        var actorRole = GetActorRole(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var specimen = await _dbContext.Specimens
            .Include(s => s.Transitions)
            .FirstOrDefaultAsync(s => s.Id == specimenId, cancellationToken);

        if (specimen is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Numune bulunamadı.");
        }

        if (!await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratorySpecimenTransition, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>();
        }

        try
        {
            var transition = specimen.Reject(actorUserId, actorRole, command.RejectionReason, command.Location, command.Notes, nowUtc);
            _dbContext.SpecimenTransitionEvents.Add(transition);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<SpecimenDetailDto>("Numune kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.SpecimenReject",
            "Specimen",
            specimen.Id.ToString(),
            $"Numune reddedildi: {specimen.Barcode} (Gerekçe: {command.RejectionReason})",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen));
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var specimen = await _dbContext.Specimens
            .Include(s => s.Transitions.OrderBy(t => t.TransitionedAtUtc))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (specimen is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Numune bulunamadı.");
        }

        return await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratoryWorklistView, cancellationToken)
            ? DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen))
            : DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>();
    }

    public async Task<DiagnosticOrderOperationResult<SpecimenDetailDto>> GetByBarcodeAsync(
        ClaimsPrincipal actor,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return DiagnosticOrderOperationResult.Validation<SpecimenDetailDto>("barcode", "Barkod zorunludur.");
        }

        var clean = barcode.Trim().ToUpperInvariant();
        var specimen = await _dbContext.Specimens
            .Include(s => s.Transitions.OrderBy(t => t.TransitionedAtUtc))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Barcode == clean, cancellationToken);

        if (specimen is null)
        {
            return DiagnosticOrderOperationResult.NotFound<SpecimenDetailDto>("Barkoda ait numune bulunamadı.");
        }

        return await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratoryWorklistView, cancellationToken)
            ? DiagnosticOrderOperationResult.Success(MapToDetailDto(specimen))
            : DiagnosticOrderOperationResult.Forbidden<SpecimenDetailDto>();
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<SpecimenSummaryDto>>> GetByOrderIdAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId, cancellationToken);
        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<IReadOnlyList<SpecimenSummaryDto>>("Tanısal istem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.LaboratoryWorklistView, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<SpecimenSummaryDto>>();
        }

        var specimens = await _dbContext.Specimens
            .Where(s => s.DiagnosticOrderId == orderId)
            .OrderBy(s => s.CreatedAtUtc)
            .AsNoTracking()
            .Select(s => new SpecimenSummaryDto(
                s.Id,
                s.Barcode,
                s.DiagnosticOrderId,
                s.PatientId,
                s.SpecimenType,
                s.ContainerType,
                s.Status,
                s.CollectedAtUtc,
                s.ReceivedAtUtc,
                s.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<SpecimenSummaryDto>>(specimens);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<SpecimenSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        SpecimenStatus? status = null,
        string? barcode = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Specimens.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(s => s.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var cleanBarcode = barcode.Trim();
            query = query.Where(s => EF.Functions.ILike(s.Barcode, $"%{cleanBarcode}%"));
        }

        var specimens = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(Math.Clamp(maxResults * 3, 1, 300))
            .ToListAsync(cancellationToken);

        var visible = new List<SpecimenSummaryDto>();
        foreach (var specimen in specimens)
        {
            if (await CanAccessSpecimenAsync(actor, specimen, HospitalPermissions.Diagnostics.LaboratoryWorklistView, cancellationToken))
            {
                visible.Add(MapToSummaryDto(specimen));
                if (visible.Count >= Math.Clamp(maxResults, 1, 100))
                {
                    break;
                }
            }
        }

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<SpecimenSummaryDto>>(visible);
    }

    private async Task<string> GenerateUniqueBarcodeAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var datePart = nowUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        while (true)
        {
            var rand = RandomNumberGenerator.GetInt32(100000, 999999);
            var barcode = $"DEMO-SMP-{datePart}-{rand}";
            var exists = await _dbContext.Specimens.AnyAsync(s => s.Barcode == barcode, cancellationToken);
            if (!exists)
            {
                return barcode;
            }
        }
    }

    private static Guid GetActorUserId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static string GetActorRole(ClaimsPrincipal actor) =>
        actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

    private async Task<bool> CanAccessSpecimenAsync(
        ClaimsPrincipal actor,
        Specimen specimen,
        string permission,
        CancellationToken cancellationToken)
    {
        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == specimen.DiagnosticOrderId, cancellationToken);
        return order is not null && await CanAccessOrderAsync(actor, order, permission, cancellationToken);
    }

    private Task<bool> CanAccessOrderAsync(
        ClaimsPrincipal actor,
        DiagnosticOrder order,
        string permission,
        CancellationToken cancellationToken) =>
        _accessContext.CanAccessResourceAsync(
            actor,
            new DiagnosticResourceContext(order.EncounterId, order.PatientId, order.DepartmentId, order.OrderType),
            permission,
            false,
            cancellationToken);

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
        var actorPersonId = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value;

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: nowUtc,
            ActorUserId: Guid.TryParse(actorUserId, out var uid) ? uid : null,
            ActorPersonId: Guid.TryParse(actorPersonId, out var personId) ? personId : null,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: targetResourceType,
            TargetResourceId: targetResourceId,
            Outcome: AuditOutcome.Success,
            Reason: null,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }

    private static SpecimenDetailDto MapToDetailDto(Specimen s) =>
        new(
            s.Id,
            s.Barcode,
            s.DiagnosticOrderId,
            s.PatientId,
            s.SpecimenType,
            s.ContainerType,
            s.Status,
            s.CollectionNotes,
            s.RejectionReason,
            s.CollectedAtUtc,
            s.CollectedByUserId,
            s.ReceivedAtUtc,
            s.ReceivedByUserId,
            s.RejectedAtUtc,
            s.RejectedByUserId,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.Version,
            s.Transitions.Select(t => new SpecimenTransitionEventDto(
                t.Id,
                t.SpecimenId,
                t.FromStatus,
                t.ToStatus,
                t.TransitionedAtUtc,
                t.ActorUserId,
                t.ActorRole,
                t.Location,
                t.Notes)).ToList());

    private static SpecimenSummaryDto MapToSummaryDto(Specimen s) =>
        new(
            s.Id,
            s.Barcode,
            s.DiagnosticOrderId,
            s.PatientId,
            s.SpecimenType,
            s.ContainerType,
            s.Status,
            s.CollectedAtUtc,
            s.ReceivedAtUtc,
            s.CreatedAtUtc);
}
