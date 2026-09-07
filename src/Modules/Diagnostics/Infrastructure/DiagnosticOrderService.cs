using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class DiagnosticOrderService : IDiagnosticOrderService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly IDiagnosticsAccessContext _accessContext;
    private readonly TimeProvider _timeProvider;

    public DiagnosticOrderService(
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

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> CreateDraftAsync(
        ClaimsPrincipal actor,
        CreateDiagnosticOrderDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticOrderCreate))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "İstem oluşturma yetkiniz bulunmamaktadır.");
        }

        if (command.PatientId == Guid.Empty)
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "patientId", "Hasta kimliği zorunludur.");
        }

        if (command.EncounterId == Guid.Empty)
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "encounterId", "Karşılaşma kimliği zorunludur.");
        }

        if (command.DepartmentId == Guid.Empty)
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "departmentId", "Bölüm kimliği zorunludur.");
        }

        var doctorId = ExtractActorPersonId(actor);
        if (doctorId == Guid.Empty)
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Oturumda geçerli bir sağlık çalışanı kimliği bulunamadı.");
        }

        var encounter = await _accessContext.FindEncounterAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("Karşılaşma bulunamadı.");
        }

        if (encounter.PatientId != command.PatientId || encounter.DepartmentId != command.DepartmentId)
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "encounterId",
                "Karşılaşmanın hasta veya bölüm bilgisi istem bağlamıyla uyuşmuyor.");
        }

        if (!encounter.IsOpenForClinicalEntry)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(
                "Yalnızca devam eden bir karşılaşmada tanısal istem oluşturulabilir.");
        }

        if (!await _accessContext.CanAccessEncounterAsync(
                actor,
                encounter,
                HospitalPermissions.Diagnostics.DiagnosticOrderCreate,
                false,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Bu karşılaşmada istem oluşturma bakım ilişkiniz veya bölüm kapsamınız bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var orderNumber = GenerateOrderNumber(command.OrderType, nowUtc);
        var orderId = Guid.NewGuid();

        var order = DiagnosticOrder.CreateDraft(
            orderId,
            orderNumber,
            command.PatientId,
            command.EncounterId,
            doctorId,
            command.DepartmentId,
            command.OrderType,
            command.Priority,
            command.ClinicalIndication,
            command.OrderNotes,
            nowUtc);

        if (command.Items != null)
        {
            foreach (var itemCmd in command.Items)
            {
                if (string.IsNullOrWhiteSpace(itemCmd.CatalogCode) || string.IsNullOrWhiteSpace(itemCmd.CatalogItemName))
                {
                    return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                        "items", "Test katalog kodu ve adı zorunludur.");
                }

                order.AddItem(
                    Guid.NewGuid(),
                    itemCmd.CatalogCode,
                    itemCmd.CatalogItemName,
                    itemCmd.Category,
                    itemCmd.SpecialInstructions,
                    nowUtc);
            }
        }

        _dbContext.DiagnosticOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.OrderCreateDraft",
            order.Id.ToString(),
            AuditOutcome.Success,
            $"İstem taslağı oluşturuldu: {order.OrderNumber} ({order.OrderType})",
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> UpdateDraftAsync(
        ClaimsPrincipal actor,
        UpdateDiagnosticOrderDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticOrderCreate))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "İstem düzenleme yetkiniz bulunmamaktadır.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("İstem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.DiagnosticOrderCreate, false, cancellationToken)
            || !CanModifyPlacedByActor(actor, order))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Yalnızca kendi bakım ilişkinizdeki kendi istem taslağınızı düzenleyebilirsiniz.");
        }

        if (order.Status != DiagnosticOrderStatus.Draft)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(
                $"Yalnızca taslak durumundaki istemler düzenlenebilir. Mevcut durum: {order.Status}");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var existingItemIds = order.Items.Select(i => i.Id).ToList();
        foreach (var id in existingItemIds)
        {
            order.RemoveItem(id, nowUtc);
        }

        if (command.Items != null)
        {
            foreach (var itemCmd in command.Items)
            {
                order.AddItem(
                    Guid.NewGuid(),
                    itemCmd.CatalogCode,
                    itemCmd.CatalogItemName,
                    itemCmd.Category,
                    itemCmd.SpecialInstructions,
                    nowUtc);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> PlaceAsync(
        ClaimsPrincipal actor,
        PlaceDiagnosticOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticOrderCreate))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "İstem onaylama ve iletme yetkiniz bulunmamaktadır.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("İstem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.DiagnosticOrderCreate, false, cancellationToken)
            || !CanModifyPlacedByActor(actor, order))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Yalnızca kendi bakım ilişkinizdeki kendi istem taslağınızı kesinleştirebilirsiniz.");
        }

        if (order.Status != DiagnosticOrderStatus.Draft)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(
                $"Yalnızca taslak durumundaki istemler onaylanabilir. Mevcut durum: {order.Status}");
        }

        var doctorId = ExtractActorPersonId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            order.Place(doctorId, nowUtc);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.OrderPlace",
            order.Id.ToString(),
            AuditOutcome.Success,
            $"İstem onaylandı ve laboratuvar/radyoloji birimine iletildi: {order.OrderNumber}",
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> CancelAsync(
        ClaimsPrincipal actor,
        CancelDiagnosticOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticOrderCreate))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "İstem iptal etme yetkiniz bulunmamaktadır.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "reason", "İptal gerekçesi zorunludur.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("İstem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.DiagnosticOrderCreate, false, cancellationToken)
            || !CanModifyPlacedByActor(actor, order))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Bu istemi iptal etme kaynak yetkiniz bulunmamaktadır.");
        }

        var doctorId = ExtractActorPersonId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            order.Cancel(doctorId, command.Reason, nowUtc);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.OrderCancel",
            order.Id.ToString(),
            AuditOutcome.Success,
            $"İstem iptal edildi: {order.OrderNumber}. Gerekçe: {command.Reason}",
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> MarkEnteredInErrorAsync(
        ClaimsPrincipal actor,
        MarkDiagnosticOrderEnteredInErrorCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return DiagnosticOrderOperationResult.Validation<DiagnosticOrderDetailDto>(
                "reason", "Hatalı giriş gerekçesi zorunludur.");
        }

        if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticOrderCreate))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Hatalı giriş işaretleme yetkiniz bulunmamaktadır.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("İstem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.DiagnosticOrderCreate, false, cancellationToken)
            || !CanModifyPlacedByActor(actor, order))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Bu istemi hatalı giriş olarak işaretleme kaynak yetkiniz bulunmamaktadır.");
        }

        var doctorId = ExtractActorPersonId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            order.MarkEnteredInError(doctorId, command.Reason, nowUtc);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Conflict<DiagnosticOrderDetailDto>(ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.OrderEnteredInError",
            order.Id.ToString(),
            AuditOutcome.Success,
            $"İstem hatalı giriş olarak işaretlendi: {order.OrderNumber}",
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<DiagnosticOrderDetailDto>> GetByIdAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<DiagnosticOrderDetailDto>("İstem bulunamadı.");
        }

        if (!await CanViewOrderAsync(actor, order, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>(
                "Bu isteme erişim için gerekli izin, kaynak kapsamı veya bakım ilişkisi bulunmamaktadır.");
        }

        if (IsPatientActor(actor))
        {
            var patientPersonId = ExtractActorPersonId(actor);
            if (patientPersonId != order.PatientId || order.Status == DiagnosticOrderStatus.Draft || order.Status == DiagnosticOrderStatus.EnteredInError)
            {
                return DiagnosticOrderOperationResult.Forbidden<DiagnosticOrderDetailDto>("Bu isteme erişim yetkiniz bulunmamaktadır.");
            }
        }

        return DiagnosticOrderOperationResult.Success(MapToDetailDto(order));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetByEncounterAsync(
        ClaimsPrincipal actor,
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var orders = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .Where(o => o.EncounterId == encounterId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var visible = new List<DiagnosticOrderSummaryDto>();
        foreach (var order in orders)
        {
            if (await CanViewOrderAsync(actor, order, cancellationToken))
            {
                visible.Add(MapToSummaryDto(order));
            }
        }

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<DiagnosticOrderSummaryDto>>(visible);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetByPatientAsync(
        ClaimsPrincipal actor,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var orders = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var visible = new List<DiagnosticOrderSummaryDto>();
        foreach (var order in orders)
        {
            if (await CanViewOrderAsync(actor, order, cancellationToken))
            {
                visible.Add(MapToSummaryDto(order));
            }
        }

        if (visible.Count == 0 && IsPatientActor(actor) && ExtractActorPersonId(actor) != patientId)
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<DiagnosticOrderSummaryDto>>(
                "Başka bir hastanın tetkik ve istem kayıtlarına erişemezsiniz.");
        }

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<DiagnosticOrderSummaryDto>>(visible);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<DiagnosticOrderSummaryDto>>> GetWorklistAsync(
        ClaimsPrincipal actor,
        DiagnosticOrderType? orderType = null,
        DiagnosticOrderStatus? status = null,
        string? orderNumber = null,
        Guid? patientId = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (IsPatientActor(actor))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<DiagnosticOrderSummaryDto>>(
                "İş listesi görüntüleme yetkiniz bulunmamaktadır.");
        }

        var query = _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .AsNoTracking();

        if (orderType.HasValue)
        {
            query = query.Where(o => o.OrderType == orderType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }
        else
        {
            query = query.Where(o => o.Status == DiagnosticOrderStatus.Placed || o.Status == DiagnosticOrderStatus.InProgress);
        }

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            query = query.Where(o => o.OrderNumber.Contains(orderNumber.Trim()));
        }

        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            query = query.Where(o => o.PatientId == patientId.Value);
        }

        var candidates = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(Math.Clamp(maxResults * 3, 1, 300))
            .ToListAsync(cancellationToken);

        var orders = new List<DiagnosticOrderSummaryDto>();
        foreach (var order in candidates)
        {
            if (await CanViewOrderAsync(actor, order, cancellationToken))
            {
                orders.Add(MapToSummaryDto(order));
                if (orders.Count >= Math.Clamp(maxResults, 1, 100))
                {
                    break;
                }
            }
        }

        return DiagnosticOrderOperationResult.Success<IReadOnlyList<DiagnosticOrderSummaryDto>>(orders);
    }

    private static DiagnosticOrderDetailDto MapToDetailDto(DiagnosticOrder o) =>
        new(
            o.Id,
            o.OrderNumber,
            o.PatientId,
            o.EncounterId,
            o.PlacingDoctorId,
            o.DepartmentId,
            o.OrderType,
            o.Priority,
            o.Status,
            o.ClinicalIndication,
            o.OrderNotes,
            o.CancellationReason,
            o.EnteredInErrorReason,
            o.PlacedAtUtc,
            o.CompletedAtUtc,
            o.CancelledAtUtc,
            o.CreatedAtUtc,
            o.UpdatedAtUtc,
            o.Version,
            o.Items.Select(i => new DiagnosticOrderItemDto(
                i.Id,
                i.DiagnosticOrderId,
                i.CatalogCode,
                i.CatalogItemName,
                i.Category,
                i.Status,
                i.SpecialInstructions,
                i.CreatedAtUtc,
                i.UpdatedAtUtc)).ToList());

    private static DiagnosticOrderSummaryDto MapToSummaryDto(DiagnosticOrder o) =>
        new(
            o.Id,
            o.OrderNumber,
            o.PatientId,
            o.EncounterId,
            o.PlacingDoctorId,
            o.DepartmentId,
            o.OrderType,
            o.Priority,
            o.Status,
            o.Items.Count,
            o.PlacedAtUtc,
            o.CreatedAtUtc);

    private static string GenerateOrderNumber(DiagnosticOrderType type, DateTime nowUtc)
    {
        var prefix = type switch
        {
            DiagnosticOrderType.Laboratory => "LAB",
            DiagnosticOrderType.Radiology => "RAD",
            DiagnosticOrderType.Pathology => "PAT",
            DiagnosticOrderType.BloodBank => "BB",
            _ => "ORD",
        };
        var randomHex = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"DEMO-{prefix}-{nowUtc:yyyyMMdd}-{randomHex}";
    }

    private async Task<bool> CanViewOrderAsync(
        ClaimsPrincipal actor,
        DiagnosticOrder order,
        CancellationToken cancellationToken)
    {
        if (IsPatientActor(actor))
        {
            return order.Status is not DiagnosticOrderStatus.Draft and not DiagnosticOrderStatus.EnteredInError
                && await CanAccessOrderAsync(
                    actor,
                    order,
                    HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn,
                    true,
                    cancellationToken);
        }

        var diagnosticWorklistPermission = order.OrderType == DiagnosticOrderType.Radiology
            ? HospitalPermissions.Diagnostics.RadiologyWorklistView
            : HospitalPermissions.Diagnostics.LaboratoryWorklistView;
        var permission = HasPermission(actor, diagnosticWorklistPermission)
            ? diagnosticWorklistPermission
            : HospitalPermissions.ClinicalRecords.EncounterView;

        return await CanAccessOrderAsync(actor, order, permission, false, cancellationToken);
    }

    private Task<bool> CanAccessOrderAsync(
        ClaimsPrincipal actor,
        DiagnosticOrder order,
        string permission,
        bool allowPatientOwnFinalResult,
        CancellationToken cancellationToken) =>
        _accessContext.CanAccessResourceAsync(
            actor,
            new DiagnosticResourceContext(
                order.EncounterId,
                order.PatientId,
                order.DepartmentId,
                order.OrderType),
            permission,
            allowPatientOwnFinalResult,
            cancellationToken);

    private static bool CanModifyPlacedByActor(ClaimsPrincipal actor, DiagnosticOrder order) =>
        actor.IsInRole(HospitalRoles.ChiefMedicalOfficer)
        || ExtractActorPersonId(actor) == order.PlacingDoctorId;

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.Identity?.IsAuthenticated == true
        && actor.HasClaim(HospitalClaimTypes.Permission, permission);

    private static bool IsPatientActor(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private static Guid ExtractActorPersonId(ClaimsPrincipal actor)
    {
        var personIdClaim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(personIdClaim, out var id) ? id : Guid.Empty;
    }

    private static Guid? ExtractActorUserId(ClaimsPrincipal actor)
    {
        var uid = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(uid, out var userGuid) ? userGuid : null;
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = ExtractActorUserId(actor);
        var actorPersonId = ExtractActorPersonId(actor);
        var actorRole = actor.FindFirst(ClaimTypes.Role)?.Value;

        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: actorUserId,
            ActorPersonId: actorPersonId == Guid.Empty ? null : actorPersonId,
            ActorRole: actorRole,
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "DiagnosticOrder",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: null,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
