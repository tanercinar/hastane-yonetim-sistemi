using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Pharmacy.Application;
using HospitalManagement.Modules.Pharmacy.Domain;
using HospitalManagement.Modules.Pharmacy.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Pharmacy.Infrastructure;

public sealed class MedicationCatalogService : IMedicationCatalogService
{
    private readonly PharmacyDbContext _dbContext;
    private readonly IAuditEventPublisher _auditEventPublisher;
    private readonly TimeProvider _timeProvider;

    public MedicationCatalogService(
        PharmacyDbContext dbContext,
        IAuditEventPublisher auditEventPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditEventPublisher = auditEventPublisher ?? throw new ArgumentNullException(nameof(auditEventPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<MedicationCatalogItemDto>> SearchMedicationsAsync(
        string? query,
        MedicationRoute? route = null,
        MedicationForm? form = null,
        bool? isActive = true,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(maxResults, 1, 100);

        var dbQuery = _dbContext.MedicationCatalogItems.AsNoTracking();

        if (isActive.HasValue)
        {
            dbQuery = dbQuery.Where(m => m.IsActive == isActive.Value);
        }

        if (route.HasValue)
        {
            dbQuery = dbQuery.Where(m => m.Route == route.Value);
        }

        if (form.HasValue)
        {
            dbQuery = dbQuery.Where(m => m.Form == form.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            dbQuery = dbQuery.Where(m =>
                EF.Functions.ILike(m.BrandName, $"%{trimmed}%") ||
                EF.Functions.ILike(m.GenericName, $"%{trimmed}%") ||
                EF.Functions.ILike(m.Code, $"%{trimmed}%") ||
                (m.AtcCode != null && EF.Functions.ILike(m.AtcCode, $"%{trimmed}%")));
        }

        var items = await dbQuery
            .OrderBy(m => m.BrandName)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<MedicationCatalogItemDto?> GetMedicationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var item = await _dbContext.MedicationCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public async Task<MedicationCatalogItemDto?> GetMedicationByCodeAsync(
        string code,
        string? catalogVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmedCode = code.Trim();
        var dbQuery = _dbContext.MedicationCatalogItems.AsNoTracking().Where(m => m.Code == trimmedCode);

        if (!string.IsNullOrWhiteSpace(catalogVersion))
        {
            var trimmedVersion = catalogVersion.Trim();
            dbQuery = dbQuery.Where(m => m.CatalogVersion == trimmedVersion);
        }

        var item = await dbQuery
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public async Task<MedicationCatalogImportResultDto> ImportCatalogAsync(
        ClaimsPrincipal actor,
        IEnumerable<MedicationCatalogItemImportDto> items,
        string catalogVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(items);

        if (string.IsNullOrWhiteSpace(catalogVersion))
        {
            throw new ArgumentException("Katalog sürümü boş olamaz.", nameof(catalogVersion));
        }

        var version = catalogVersion.Trim();
        var itemList = items.ToList();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var existingItems = await _dbContext.MedicationCatalogItems
            .Where(m => m.CatalogVersion == version)
            .ToDictionaryAsync(m => m.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var itemDto in itemList)
        {
            if (string.IsNullOrWhiteSpace(itemDto.Code) ||
                string.IsNullOrWhiteSpace(itemDto.BrandName) ||
                string.IsNullOrWhiteSpace(itemDto.GenericName))
            {
                continue;
            }

            if (existingItems.TryGetValue(itemDto.Code.Trim(), out var existingItem))
            {
                existingItem.Update(
                    itemDto.BrandName,
                    itemDto.GenericName,
                    itemDto.Form,
                    itemDto.StrengthValue,
                    itemDto.StrengthUnit,
                    itemDto.Route,
                    itemDto.AtcCode,
                    itemDto.Description,
                    version,
                    nowUtc);
                updatedCount++;
            }
            else
            {
                var newItem = MedicationCatalogItem.Create(
                    Guid.NewGuid(),
                    itemDto.Code,
                    itemDto.BrandName,
                    itemDto.GenericName,
                    itemDto.Form,
                    itemDto.StrengthValue,
                    itemDto.StrengthUnit,
                    itemDto.Route,
                    itemDto.AtcCode,
                    itemDto.Description,
                    version,
                    nowUtc);

                _dbContext.MedicationCatalogItems.Add(newItem);
                insertedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await PublishAuditAsync(
            actor,
            "Pharmacy.MedicationCatalogImport",
            version,
            AuditOutcome.Success,
            $"İlaç kataloğu içe aktarıldı. Sürüm: {version}, Eklenen: {insertedCount}, Güncellenen: {updatedCount}",
            cancellationToken);

        return new MedicationCatalogImportResultDto(version, itemList.Count, insertedCount, updatedCount);
    }

    private static MedicationCatalogItemDto MapToDto(MedicationCatalogItem entity) =>
        new(
            entity.Id,
            entity.Code,
            entity.BrandName,
            entity.GenericName,
            entity.Form,
            entity.StrengthValue,
            entity.StrengthUnit,
            entity.Route,
            entity.AtcCode,
            entity.Description,
            entity.CatalogVersion,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private async Task PublishAuditAsync(
        ClaimsPrincipal actor,
        string action,
        string resourceId,
        AuditOutcome outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actorUserId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value is { } uid && Guid.TryParse(uid, out var userGuid)
            ? userGuid
            : (Guid?)null;

        var actorPersonId = actor.FindFirst(HospitalClaimTypes.PersonId)?.Value is { } pid && Guid.TryParse(pid, out var personGuid)
            ? personGuid
            : (Guid?)null;

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
            TargetResourceType: "MedicationCatalog",
            TargetResourceId: resourceId,
            Outcome: outcome,
            Reason: reason,
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditEventPublisher.PublishAsync(auditEvent, cancellationToken);
    }
}
