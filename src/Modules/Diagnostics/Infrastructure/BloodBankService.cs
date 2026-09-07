using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class BloodBankService : IBloodBankService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly IDiagnosticsAccessContext _accessContext;

    public BloodBankService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider,
        IDiagnosticsAccessContext accessContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _accessContext = accessContext ?? throw new ArgumentNullException(nameof(accessContext));
    }

    public async Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> CreateCrossmatchRequestAsync(
        ClaimsPrincipal actor,
        Guid orderId,
        Guid orderItemId,
        BloodGroup patientBloodGroup,
        BloodProductType requestedProductType,
        int unitsRequested,
        DateTime? requiredByUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (unitsRequested <= 0)
        {
            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("UnitsRequested", "Talep edilen ünite adedi en az 1 olmalıdır.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Tanısal istem kaydı bulunamadı.");
        }

        if (order.OrderType != DiagnosticOrderType.BloodBank)
        {
            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("OrderId", "Yalnızca kan bankası istemlerinden crossmatch talebi oluşturulabilir.");
        }

        if (order.Status is not (DiagnosticOrderStatus.Placed or DiagnosticOrderStatus.InProgress))
        {
            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("OrderId", "Crossmatch yalnızca aktif bir kan bankası istemi için oluşturulabilir.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.DiagnosticOrderCreate, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<CrossmatchDetailDto>("Bu kan bankası istemi için crossmatch talebi oluşturma yetkiniz yok.");
        }

        var orderItem = order.Items.FirstOrDefault(i => i.Id == orderItemId);
        if (orderItem is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("İstem kalemi bulunamadı.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var request = CrossmatchRequest.Create(
            Guid.NewGuid(),
            orderId,
            orderItemId,
            order.PatientId,
            patientBloodGroup,
            requestedProductType,
            unitsRequested,
            requiredByUtc,
            nowUtc);

        _dbContext.CrossmatchRequests.Add(request);

        if (orderItem.Status == DiagnosticOrderItemStatus.Pending)
        {
            orderItem.UpdateStatus(DiagnosticOrderItemStatus.InAnalysis, nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Diagnostics.BloodBankCrossmatchRequest",
            "CrossmatchRequest",
            request.Id.ToString(),
            $"Kan bankası crossmatch ve ürün talebi oluşturuldu: {requestedProductType} ({unitsRequested} Ünite) - Hasta Grubu: {patientBloodGroup}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToCrossmatchDetailDto(request));
    }

    public async Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> PerformCrossmatchTestAsync(
        ClaimsPrincipal actor,
        Guid crossmatchRequestId,
        Guid bloodUnitId,
        string? technicianNotes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var request = await _dbContext.CrossmatchRequests
            .FirstOrDefaultAsync(r => r.Id == crossmatchRequestId, cancellationToken);

        if (request is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Crossmatch istem kaydı bulunamadı.");
        }

        var order = await _dbContext.DiagnosticOrders
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.DiagnosticOrderId, cancellationToken);
        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Crossmatch istemine bağlı tanısal istem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.LaboratoryResultFinalize, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<CrossmatchDetailDto>("Bu crossmatch kaydını sonuçlandırma yetkiniz yok.");
        }

        var unit = await _dbContext.BloodUnits
            .FirstOrDefaultAsync(u => u.Id == bloodUnitId, cancellationToken);

        if (unit is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Seçilen kan ürünü ünitesi bulunamadı.");
        }

        if (unit.Status != BloodUnitStatus.Available && unit.ReservedForPatientId != request.PatientId)
        {
            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("BloodUnitId", $"Seçilen ünite test için müsait değil. Durum: {unit.Status}");
        }

        var actorUserId = GetActorUserId(actor);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        // Deterministic Blood Compatibility Check
        var isCompatible = BloodCompatibilityMatrix.IsCompatible(unit.BloodGroup, request.PatientBloodGroup, request.RequestedProductType);

        if (!isCompatible)
        {
            request.RecordTestResult(
                actorUserId,
                BloodCompatibilityStatus.Incompatible,
                null,
                string.IsNullOrWhiteSpace(technicianNotes) ? "Kan grubu ve antikor uyumsuzluğu (Deterministik Mock Simülasyonu)" : technicianNotes,
                nowUtc);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await PublishAuditAsync(
                actor,
                "Diagnostics.BloodBankCrossmatchIncompatible",
                "CrossmatchRequest",
                request.Id.ToString(),
                $"Crossmatch testi UYUMSUZ sonuçlandı: Ünite No {unit.UnitNumber} ({unit.BloodGroup}) != Hasta ({request.PatientBloodGroup})",
                nowUtc,
                cancellationToken);

            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("Compatibility", $"Kan ürünü uyumsuz (Incompatible): {unit.BloodGroup} ünite, {request.PatientBloodGroup} hasta için kullanılamaz.");
        }

        // Compatible: Reserve Unit and Complete Crossmatch
        try
        {
            unit.Reserve(request.PatientId, nowUtc.AddHours(48), nowUtc);
            request.RecordTestResult(
                actorUserId,
                BloodCompatibilityStatus.Compatible,
                unit.Id,
                technicianNotes ?? "Serolojik ve bilgisayarlı crossmatch tam uyumlu.",
                nowUtc);

            var orderItem = order.Items.FirstOrDefault(i => i.Id == request.DiagnosticOrderItemId);
            if (orderItem != null && orderItem.Status != DiagnosticOrderItemStatus.Reported)
            {
                orderItem.UpdateStatus(DiagnosticOrderItemStatus.Reported, nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<CrossmatchDetailDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<CrossmatchDetailDto>("Kayıt başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.BloodBankCrossmatchCompatible",
            "CrossmatchRequest",
            request.Id.ToString(),
            $"Crossmatch testi UYUMLU sonuçlandı ve ünite rezerve edildi: Ünite No {unit.UnitNumber} ({unit.BloodGroup}) -> Hasta ID {request.PatientId}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToCrossmatchDetailDto(request));
    }

    public async Task<DiagnosticOrderOperationResult<BloodUnitDto>> IssueBloodUnitAsync(
        ClaimsPrincipal actor,
        Guid bloodUnitId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var unit = await _dbContext.BloodUnits
            .FirstOrDefaultAsync(u => u.Id == bloodUnitId, cancellationToken);

        if (unit is null)
        {
            return DiagnosticOrderOperationResult.NotFound<BloodUnitDto>("Kan ürünü ünitesi bulunamadı.");
        }

        var crossmatch = await _dbContext.CrossmatchRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.AllocatedBloodUnitId == unit.Id, cancellationToken);
        if (crossmatch is null)
        {
            return DiagnosticOrderOperationResult.Validation<BloodUnitDto>("BloodUnitId", "Ünite, tamamlanmış bir crossmatch talebine tahsis edilmemiştir.");
        }

        var order = await _dbContext.DiagnosticOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == crossmatch.DiagnosticOrderId, cancellationToken);
        if (order is null)
        {
            return DiagnosticOrderOperationResult.NotFound<BloodUnitDto>("Üniteye bağlı tanısal istem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.LaboratorySpecimenTransition, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<BloodUnitDto>("Bu kan ürününü çıkış yapma yetkiniz yok.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            unit.Issue(nowUtc);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<BloodUnitDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<BloodUnitDto>("Ünite kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.BloodBankUnitIssue",
            "BloodUnit",
            unit.Id.ToString(),
            $"Kan ürünü kliniğe / ameliyathaneye çıkışı yapıldı: {unit.UnitNumber} ({unit.ProductType} - {unit.BloodGroup})",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToBloodUnitDto(unit));
    }

    public async Task<DiagnosticOrderOperationResult<BloodUnitDto>> RecordTransfusionAsync(
        ClaimsPrincipal actor,
        Guid bloodUnitId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var unit = await _dbContext.BloodUnits
            .FirstOrDefaultAsync(u => u.Id == bloodUnitId, cancellationToken);

        if (unit is null)
        {
            return DiagnosticOrderOperationResult.NotFound<BloodUnitDto>("Kan ürünü ünitesi bulunamadı.");
        }

        var allocatedRequest = await _dbContext.CrossmatchRequests
            .FirstOrDefaultAsync(request => request.AllocatedBloodUnitId == unit.Id, cancellationToken);
        if (allocatedRequest is null)
        {
            return DiagnosticOrderOperationResult.Validation<BloodUnitDto>("BloodUnitId", "Ünite, tamamlanmış bir crossmatch talebine tahsis edilmemiştir.");
        }

        var allocatedOrder = await _dbContext.DiagnosticOrders
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == allocatedRequest.DiagnosticOrderId, cancellationToken);
        if (allocatedOrder is null)
        {
            return DiagnosticOrderOperationResult.NotFound<BloodUnitDto>("Üniteye bağlı tanısal istem bulunamadı.");
        }

        if (!await CanAccessOrderAsync(actor, allocatedOrder, HospitalPermissions.Diagnostics.BloodTransfusionRecord, cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<BloodUnitDto>("Bu kan ürünü için transfüzyon kaydı oluşturma yetkiniz yok.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            unit.RecordTransfusion(nowUtc);

            if (allocatedOrder.Items.All(i => i.Status == DiagnosticOrderItemStatus.Reported || i.Status == DiagnosticOrderItemStatus.Cancelled))
            {
                allocatedOrder.Complete(nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return DiagnosticOrderOperationResult.Validation<BloodUnitDto>("Status", ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DiagnosticOrderOperationResult.Conflict<BloodUnitDto>("Ünite kaydı başka bir işlem tarafından güncellendi.");
        }

        await PublishAuditAsync(
            actor,
            "Diagnostics.BloodBankTransfusionRecord",
            "BloodUnit",
            unit.Id.ToString(),
            $"Kan transfüzyonu hastaya başarıyla uygulandı ve kaydedildi: {unit.UnitNumber}",
            nowUtc,
            cancellationToken);

        return DiagnosticOrderOperationResult.Success(MapToBloodUnitDto(unit));
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<BloodUnitDto>>> GetInventoryAsync(
        ClaimsPrincipal actor,
        BloodProductType? productType = null,
        BloodGroup? bloodGroup = null,
        BloodUnitStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!await _accessContext.CanAccessDiagnosticAreaAsync(
                actor,
                DiagnosticOrderType.BloodBank,
                HospitalPermissions.Diagnostics.LaboratoryWorklistView,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<BloodUnitDto>>("Kan bankası envanterini görüntüleme yetkiniz yok.");
        }

        var query = _dbContext.BloodUnits
            .AsNoTracking();

        if (productType.HasValue)
            query = query.Where(u => u.ProductType == productType.Value);
        if (bloodGroup.HasValue)
            query = query.Where(u => u.BloodGroup == bloodGroup.Value);
        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        var list = await query
            .OrderBy(u => u.ExpiryDateUtc)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(MapToBloodUnitDto).ToList();
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<BloodUnitDto>>(dtos);
    }

    public async Task<DiagnosticOrderOperationResult<BloodInventorySummaryDto>> GetInventorySummaryAsync(
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!await _accessContext.CanAccessDiagnosticAreaAsync(
                actor,
                DiagnosticOrderType.BloodBank,
                HospitalPermissions.Diagnostics.LaboratoryWorklistView,
                cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<BloodInventorySummaryDto>("Kan bankası envanter özetini görüntüleme yetkiniz yok.");
        }

        var list = await _dbContext.BloodUnits
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var total = list.Count;
        var available = list.Count(u => u.Status == BloodUnitStatus.Available);
        var reserved = list.Count(u => u.Status == BloodUnitStatus.Reserved);
        var issued = list.Count(u => u.Status == BloodUnitStatus.Issued);
        var transfused = list.Count(u => u.Status == BloodUnitStatus.Transfused);
        var discarded = list.Count(u => u.Status == BloodUnitStatus.Discarded);

        var byGroup = list.GroupBy(u => u.BloodGroup.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byType = list.GroupBy(u => u.ProductType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var summary = new BloodInventorySummaryDto(
            total,
            available,
            reserved,
            issued,
            transfused,
            discarded,
            byGroup,
            byType);

        return DiagnosticOrderOperationResult.Success(summary);
    }

    public async Task<DiagnosticOrderOperationResult<IReadOnlyList<CrossmatchSummaryDto>>> GetCrossmatchWorklistAsync(
        ClaimsPrincipal actor,
        CrossmatchStatus? status = null,
        Guid? patientId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (IsPatient(actor))
        {
            if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn))
            {
                return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<CrossmatchSummaryDto>>("Kan bankası sonuçlarını görüntüleme yetkiniz yok.");
            }
        }
        else if (!await _accessContext.CanAccessDiagnosticAreaAsync(
                     actor,
                     DiagnosticOrderType.BloodBank,
                     HospitalPermissions.Diagnostics.LaboratoryWorklistView,
                     cancellationToken))
        {
            return DiagnosticOrderOperationResult.Forbidden<IReadOnlyList<CrossmatchSummaryDto>>("Kan bankası iş listesini görüntüleme yetkiniz yok.");
        }

        var query = _dbContext.CrossmatchRequests
            .AsNoTracking();

        if (IsPatient(actor))
        {
            var actorPatientId = GetActorPatientId(actor);
            query = query.Where(r => r.PatientId == actorPatientId && r.Status == CrossmatchStatus.Completed);
        }
        else
        {
            if (patientId.HasValue && patientId.Value != Guid.Empty)
                query = query.Where(r => r.PatientId == patientId.Value);
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(MapToCrossmatchSummaryDto).ToList();
        return DiagnosticOrderOperationResult.Success<IReadOnlyList<CrossmatchSummaryDto>>(dtos);
    }

    public async Task<DiagnosticOrderOperationResult<CrossmatchDetailDto>> GetCrossmatchByIdAsync(
        ClaimsPrincipal actor,
        Guid crossmatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var request = await _dbContext.CrossmatchRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == crossmatchId, cancellationToken);

        if (request is null)
        {
            return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Crossmatch istem kaydı bulunamadı.");
        }

        if (IsPatient(actor))
        {
            var patientId = GetActorPatientId(actor);
            if (!HasPermission(actor, HospitalPermissions.Diagnostics.DiagnosticResultViewFinalOwn)
                || request.PatientId != patientId
                || request.Status != CrossmatchStatus.Completed)
            {
                return DiagnosticOrderOperationResult.Forbidden<CrossmatchDetailDto>("Yalnızca tamamlanmış kendi kan bankası sonuçlarınızı görüntüleyebilirsiniz.");
            }
        }
        else
        {
            var order = await _dbContext.DiagnosticOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == request.DiagnosticOrderId, cancellationToken);
            if (order is null)
            {
                return DiagnosticOrderOperationResult.NotFound<CrossmatchDetailDto>("Crossmatch kaydına bağlı tanısal istem bulunamadı.");
            }

            if (!await CanAccessOrderAsync(actor, order, HospitalPermissions.Diagnostics.LaboratoryWorklistView, cancellationToken))
            {
                return DiagnosticOrderOperationResult.Forbidden<CrossmatchDetailDto>("Bu crossmatch kaydını görüntüleme yetkiniz yok.");
            }
        }

        return DiagnosticOrderOperationResult.Success(MapToCrossmatchDetailDto(request));
    }

    private static BloodUnitDto MapToBloodUnitDto(BloodUnit u) =>
        new(
            u.Id,
            u.UnitNumber,
            u.ProductType,
            u.BloodGroup,
            u.VolumeMl,
            u.DonationDateUtc,
            u.ExpiryDateUtc,
            u.StorageLocation,
            u.Status,
            u.ReservedForPatientId,
            u.ReservedUntilUtc,
            u.CreatedAtUtc,
            u.UpdatedAtUtc,
            u.Version);

    private static CrossmatchDetailDto MapToCrossmatchDetailDto(CrossmatchRequest r) =>
        new(
            r.Id,
            r.DiagnosticOrderId,
            r.DiagnosticOrderItemId,
            r.PatientId,
            r.PatientBloodGroup,
            r.RequestedProductType,
            r.UnitsRequested,
            r.RequiredByUtc,
            r.Status,
            r.CompatibilityResult,
            r.TechnicianNotes,
            r.TestedAtUtc,
            r.TestedByUserId,
            r.AllocatedBloodUnitId,
            r.CancellationReason,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            r.Version);

    private static CrossmatchSummaryDto MapToCrossmatchSummaryDto(CrossmatchRequest r) =>
        new(
            r.Id,
            r.DiagnosticOrderId,
            r.PatientId,
            r.PatientBloodGroup,
            r.RequestedProductType,
            r.UnitsRequested,
            r.Status,
            r.CompatibilityResult,
            r.AllocatedBloodUnitId,
            r.CreatedAtUtc);

    private static bool IsPatient(ClaimsPrincipal actor) =>
        actor.IsInRole(HospitalRoles.Patient);

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.Identity?.IsAuthenticated == true
        && actor.HasClaim(HospitalClaimTypes.Permission, permission);

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

    private static Guid GetActorUserId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var uid) ? uid : Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid GetActorPatientId(ClaimsPrincipal actor)
    {
        var claim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value
            ?? actor.FindFirst("PatientId")?.Value
            ?? actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var pid) ? pid : Guid.Empty;
    }

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
}
