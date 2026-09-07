using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public sealed class MedicationStockService : IMedicationStockService
{
    private readonly PharmacyDbContext _dbContext;
    private readonly IAuditEventPublisher _auditEventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ICareRelationshipEvaluator _careRelationshipEvaluator;

    public MedicationStockService(
        PharmacyDbContext dbContext,
        IAuditEventPublisher auditEventPublisher,
        TimeProvider timeProvider,
        ICareRelationshipEvaluator careRelationshipEvaluator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditEventPublisher = auditEventPublisher ?? throw new ArgumentNullException(nameof(auditEventPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _careRelationshipEvaluator = careRelationshipEvaluator ?? throw new ArgumentNullException(nameof(careRelationshipEvaluator));
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<MedicationStockItemDto>>> GetStockOverviewAsync(
        ClaimsPrincipal actor,
        Guid? medicationCatalogItemId = null,
        string? location = null,
        bool? onlyLowStock = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.InventoryPharmacyView)
            || !TryExtractActorPersonId(actor, out var actorPersonId))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<MedicationStockItemDto>>(
                "Stok bilgilerini görüntüleme yetkiniz bulunmamaktadır.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var query = from stock in _dbContext.MedicationStockItems.AsNoTracking()
                    join med in _dbContext.MedicationCatalogItems.AsNoTracking()
                    on stock.MedicationCatalogItemId equals med.Id
                    select new
                    {
                        stock,
                        med
                    };

        if (medicationCatalogItemId.HasValue && medicationCatalogItemId.Value != Guid.Empty)
        {
            query = query.Where(x => x.stock.MedicationCatalogItemId == medicationCatalogItemId.Value);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim();
            query = query.Where(x => x.stock.Location.Contains(loc));
        }

        if (onlyLowStock == true)
        {
            query = query.Where(x => x.stock.QuantityOnHand <= x.stock.ReorderLevel);
        }

        var candidates = await query
            .OrderBy(x => x.med.BrandName)
            .ThenBy(x => x.stock.ExpirationDateUtc)
            .ToListAsync(cancellationToken);

        var allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(
            actorPersonId,
            candidates.Select(candidate => candidate.stock.DepartmentId),
            cancellationToken);

        var results = candidates
            .Where(candidate => allowedDepartmentIds.Contains(candidate.stock.DepartmentId))
            .ToList();

        var dtos = results.Select(r => new MedicationStockItemDto(
            r.stock.Id,
            r.stock.DepartmentId,
            r.stock.Location,
            r.stock.MedicationCatalogItemId,
            r.med.Code,
            r.med.BrandName,
            r.med.GenericName,
            r.stock.LotNumber,
            r.stock.ExpirationDateUtc,
            r.stock.QuantityOnHand,
            r.stock.QuantityReserved,
            r.stock.QuantityAvailable,
            r.stock.ReorderLevel,
            r.stock.IsExpired(nowUtc),
            r.stock.IsLowStock,
            r.stock.CreatedAtUtc,
            r.stock.Version)).ToList();

        await PublishAuditAsync(
            actor,
            "Pharmacy.StockView",
            "StockOverview",
            AuditOutcome.Success,
            $"Stok durumu sorgulandı. Sonuç adedi: {dtos.Count}",
            cancellationToken);

        return PrescriptionOperationResult.Success<IReadOnlyList<MedicationStockItemDto>>(dtos);
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<FefoCandidateDto>>> GetFefoCandidatesAsync(
        ClaimsPrincipal actor,
        Guid medicationCatalogItemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.InventoryPharmacyView)
            || !TryExtractActorPersonId(actor, out var actorPersonId))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<FefoCandidateDto>>(
                "FEFO stok adaylarını görüntüleme yetkiniz bulunmamaktadır.");
        }

        if (medicationCatalogItemId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<IReadOnlyList<FefoCandidateDto>>(
                "medicationCatalogItemId", "İlaç katalog kimliği zorunludur.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var availableLots = await _dbContext.MedicationStockItems
            .AsNoTracking()
            .Where(s => s.MedicationCatalogItemId == medicationCatalogItemId &&
                        s.QuantityOnHand - s.QuantityReserved > 0 &&
                        s.ExpirationDateUtc > nowUtc)
            .OrderBy(s => s.ExpirationDateUtc) // FEFO (First-Expired-First-Out)
            .ToListAsync(cancellationToken);

        var allowedDepartmentIds = await GetAllowedDepartmentIdsAsync(
            actorPersonId,
            availableLots.Select(stock => stock.DepartmentId),
            cancellationToken);

        var dtos = availableLots
            .Where(stock => allowedDepartmentIds.Contains(stock.DepartmentId))
            .Select(s => new FefoCandidateDto(
            s.Id,
            s.Location,
            s.LotNumber,
            s.ExpirationDateUtc,
            s.QuantityAvailable,
            Math.Max(0, (int)(s.ExpirationDateUtc - nowUtc).TotalDays),
            s.Version)).ToList();

        return PrescriptionOperationResult.Success<IReadOnlyList<FefoCandidateDto>>(dtos);
    }

    public async Task<PrescriptionOperationResult<MedicationStockItemDto>> AdjustStockAsync(
        ClaimsPrincipal actor,
        AdjustStockCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.InventoryPharmacyAdjust)
            || !TryExtractActorPersonId(actor, out var actorPersonId))
        {
            return PrescriptionOperationResult.Forbidden<MedicationStockItemDto>(
                "Stok düzeltme yetkiniz bulunmamaktadır.");
        }

        if (command.StockItemId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<MedicationStockItemDto>(
                "stockItemId", "Stok kalem kimliği zorunludur.");
        }

        if (command.NewQuantity < 0)
        {
            return PrescriptionOperationResult.Validation<MedicationStockItemDto>(
                "newQuantity", "Stok miktarı negatif olamaz.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length < 5)
        {
            return PrescriptionOperationResult.Validation<MedicationStockItemDto>(
                "reason", "Stok düzeltme gerekçesi zorunludur (en az 5 karakter).");
        }

        var stockItem = await _dbContext.MedicationStockItems
            .FirstOrDefaultAsync(s => s.Id == command.StockItemId, cancellationToken);

        if (stockItem is null)
        {
            return PrescriptionOperationResult.NotFound<MedicationStockItemDto>("Stok kalemi bulunamadı.");
        }

        if (!await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                stockItem.DepartmentId,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<MedicationStockItemDto>(
                "Bu stok bölümünde düzeltme yapma yetkiniz bulunmamaktadır.");
        }

        if (command.ExpectedVersion <= 0 || command.ExpectedVersion != stockItem.Version)
        {
            return PrescriptionOperationResult.Conflict<MedicationStockItemDto>(
                "Stok kaydı başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        var med = await _dbContext.MedicationCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == stockItem.MedicationCatalogItemId, cancellationToken);

        var prevQty = stockItem.QuantityOnHand;
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var diff = command.NewQuantity - prevQty;

        stockItem.AdjustStock(command.NewQuantity, nowUtc);

        var txType = diff >= 0 ? StockTransactionType.AdjustmentIn : StockTransactionType.AdjustmentOut;
        var tx = MedicationStockTransaction.Create(
            Guid.NewGuid(),
            stockItem.Id,
            txType,
            Math.Abs(diff),
            prevQty,
            command.NewQuantity,
            referenceId: "ADJUSTMENT",
            notes: command.Reason.Trim(),
            performedByUserId: ExtractActorUserId(actor),
            performedAtUtc: nowUtc);

        _dbContext.MedicationStockTransactions.Add(tx);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PrescriptionOperationResult.Conflict<MedicationStockItemDto>(
                "Stok kaydı başka bir işlem tarafından değiştirildi. Güncel veriyi yükleyip yeniden deneyiniz.");
        }

        await PublishAuditAsync(
            actor,
            "Pharmacy.StockAdjustment",
            stockItem.Id.ToString(),
            AuditOutcome.Success,
            $"Stok düzeltildi. Önceki: {prevQty}, Yeni: {command.NewQuantity}, Gerekçe: {command.Reason.Trim()}",
            cancellationToken);

        var dto = new MedicationStockItemDto(
            stockItem.Id,
            stockItem.DepartmentId,
            stockItem.Location,
            stockItem.MedicationCatalogItemId,
            med?.Code ?? string.Empty,
            med?.BrandName ?? string.Empty,
            med?.GenericName ?? string.Empty,
            stockItem.LotNumber,
            stockItem.ExpirationDateUtc,
            stockItem.QuantityOnHand,
            stockItem.QuantityReserved,
            stockItem.QuantityAvailable,
            stockItem.ReorderLevel,
            stockItem.IsExpired(nowUtc),
            stockItem.IsLowStock,
            stockItem.CreatedAtUtc,
            stockItem.Version);

        return PrescriptionOperationResult.Success(dto);
    }

    public async Task<PrescriptionOperationResult<IReadOnlyList<MedicationStockTransactionDto>>> GetStockTransactionsAsync(
        ClaimsPrincipal actor,
        Guid stockItemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!HasPermission(actor, HospitalPermissions.Pharmacy.InventoryPharmacyView)
            || !TryExtractActorPersonId(actor, out var actorPersonId))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<MedicationStockTransactionDto>>(
                "Stok hareketlerini görüntüleme yetkiniz bulunmamaktadır.");
        }

        if (stockItemId == Guid.Empty)
        {
            return PrescriptionOperationResult.Validation<IReadOnlyList<MedicationStockTransactionDto>>(
                "stockItemId", "Stok kalem kimliği zorunludur.");
        }

        var stockDepartmentId = await _dbContext.MedicationStockItems
            .AsNoTracking()
            .Where(stock => stock.Id == stockItemId)
            .Select(stock => (Guid?)stock.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

        if (!stockDepartmentId.HasValue)
        {
            return PrescriptionOperationResult.NotFound<IReadOnlyList<MedicationStockTransactionDto>>(
                "Stok kalemi bulunamadı.");
        }

        if (!await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                actorPersonId,
                stockDepartmentId.Value,
                cancellationToken))
        {
            return PrescriptionOperationResult.Forbidden<IReadOnlyList<MedicationStockTransactionDto>>(
                "Bu stok bölümünün hareketlerini görüntüleme yetkiniz bulunmamaktadır.");
        }

        var txs = await _dbContext.MedicationStockTransactions
            .AsNoTracking()
            .Where(t => t.StockItemId == stockItemId)
            .OrderByDescending(t => t.PerformedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = txs.Select(t => new MedicationStockTransactionDto(
            t.Id,
            t.StockItemId,
            t.TransactionType,
            t.Quantity,
            t.PreviousQuantityOnHand,
            t.NewQuantityOnHand,
            t.ReferenceId,
            t.Notes,
            t.PerformedAtUtc)).ToList();

        return PrescriptionOperationResult.Success<IReadOnlyList<MedicationStockTransactionDto>>(dtos);
    }

    private static bool HasPermission(ClaimsPrincipal actor, string permission) =>
        actor.Identity?.IsAuthenticated == true
        && actor.HasClaim(HospitalClaimTypes.Permission, permission);

    private static bool TryExtractActorPersonId(ClaimsPrincipal actor, out Guid personId) =>
        Guid.TryParse(actor.FindFirst(HospitalClaimTypes.PersonId)?.Value, out personId)
        && personId != Guid.Empty;

    private async Task<HashSet<Guid>> GetAllowedDepartmentIdsAsync(
        Guid actorPersonId,
        IEnumerable<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        var allowed = new HashSet<Guid>();
        foreach (var departmentId in departmentIds.Distinct())
        {
            if (await _careRelationshipEvaluator.IsAssignedToDepartmentAsync(
                    actorPersonId,
                    departmentId,
                    cancellationToken))
            {
                allowed.Add(departmentId);
            }
        }

        return allowed;
    }

    private static Guid? ExtractActorUserId(ClaimsPrincipal actor)
    {
        var uidClaim = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(uidClaim, out var id) ? id : null;
    }

    private static Guid? ExtractActorPersonId(ClaimsPrincipal actor)
    {
        var pidClaim = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value;
        return Guid.TryParse(pidClaim, out var id) ? id : null;
    }

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string targetResourceId,
        AuditOutcome outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent(
            Id: Guid.NewGuid(),
            CreatedAtUtc: _timeProvider.GetUtcNow().UtcDateTime,
            ActorUserId: ExtractActorUserId(actor),
            ActorPersonId: ExtractActorPersonId(actor),
            ActorRole: actor.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
            ActorIpAddress: null,
            ActorUserAgent: null,
            Action: action,
            TargetResourceType: "MedicationStock",
            TargetResourceId: targetResourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
