using System.Security.Claims;

using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.BuildingBlocks.Authorization;
using HospitalManagement.Modules.Diagnostics.Application;
using HospitalManagement.Modules.Diagnostics.Domain;
using HospitalManagement.Modules.Diagnostics.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.Diagnostics.Infrastructure;

public sealed class LabCatalogService : ILabCatalogService
{
    private readonly DiagnosticsDbContext _dbContext;
    private readonly IAuditEventPublisher _auditPublisher;
    private readonly TimeProvider _timeProvider;

    public LabCatalogService(
        DiagnosticsDbContext dbContext,
        IAuditEventPublisher auditPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditPublisher = auditPublisher ?? throw new ArgumentNullException(nameof(auditPublisher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<LabCatalogSummaryDto>> SearchAsync(
        string? query = null,
        string? category = null,
        bool? isActive = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var dbQuery = _dbContext.LabCatalogItems
            .Include(c => c.Parameters)
            .AsNoTracking();

        if (isActive.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmedCat = category.Trim();
            dbQuery = dbQuery.Where(c => EF.Functions.ILike(c.Category, trimmedCat));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmedQuery = query.Trim();
            dbQuery = dbQuery.Where(c =>
                EF.Functions.ILike(c.Code, $"%{trimmedQuery}%") ||
                EF.Functions.ILike(c.Name, $"%{trimmedQuery}%") ||
                EF.Functions.ILike(c.Category, $"%{trimmedQuery}%") ||
                EF.Functions.ILike(c.SpecimenType, $"%{trimmedQuery}%"));
        }

        var items = await dbQuery
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .Take(Math.Clamp(maxResults, 1, 100))
            .Select(c => new LabCatalogSummaryDto(
                c.Id,
                c.Code,
                c.Name,
                c.Category,
                c.SpecimenType,
                c.ContainerType,
                c.IsPanel,
                c.TurnaroundMinutes,
                c.IsActive,
                c.Parameters.Count))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<LabCatalogItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.LabCatalogItems
            .Include(c => c.Parameters.OrderBy(p => p.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return item is null ? null : MapToDetailDto(item);
    }

    public async Task<LabCatalogItemDto?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var cleanCode = code.Trim().ToUpperInvariant();
        var item = await _dbContext.LabCatalogItems
            .Include(c => c.Parameters.OrderBy(p => p.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == cleanCode, cancellationToken);

        return item is null ? null : MapToDetailDto(item);
    }

    public async Task<ImportLabCatalogResultDto> ImportCatalogAsync(
        ClaimsPrincipal actor,
        ImportLabCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        int totalAdded = 0;
        int totalUpdated = 0;

        foreach (var itemCmd in command.Items)
        {
            var cleanCode = itemCmd.Code.Trim().ToUpperInvariant();
            var existingItem = await _dbContext.LabCatalogItems
                .Include(c => c.Parameters)
                .FirstOrDefaultAsync(c => c.Code == cleanCode, cancellationToken);

            if (existingItem is null)
            {
                var newItem = LabCatalogItem.Create(
                    Guid.NewGuid(),
                    cleanCode,
                    itemCmd.Name,
                    itemCmd.Category,
                    itemCmd.SpecimenType,
                    itemCmd.ContainerType,
                    itemCmd.IsPanel,
                    itemCmd.TurnaroundMinutes,
                    command.CatalogVersion,
                    itemCmd.Description,
                    nowUtc);

                foreach (var paramCmd in itemCmd.Parameters)
                {
                    newItem.AddParameter(LabCatalogParameter.Create(
                        Guid.NewGuid(),
                        newItem.Id,
                        paramCmd.Code,
                        paramCmd.Name,
                        paramCmd.Unit,
                        paramCmd.ReferenceRangeLow,
                        paramCmd.ReferenceRangeHigh,
                        paramCmd.CriticalLow,
                        paramCmd.CriticalHigh,
                        paramCmd.ValueType,
                        paramCmd.SortOrder));
                }

                _dbContext.LabCatalogItems.Add(newItem);
                totalAdded++;
            }
            else
            {
                existingItem.SetActive(true, nowUtc);
                totalUpdated++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

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
            Action: "Diagnostics.LabCatalogImport",
            TargetResourceType: "LabCatalog",
            TargetResourceId: command.CatalogVersion,
            Outcome: AuditOutcome.Success,
            Reason: $"Laboratuvar kataloğu içe aktarıldı: {command.CatalogVersion} (Eklenen: {totalAdded}, Güncellenen: {totalUpdated})",
            CorrelationId: Guid.NewGuid().ToString("D"),
            DetailsJson: null);

        await _auditPublisher.PublishAsync(auditEvent, cancellationToken);

        return new ImportLabCatalogResultDto(
            command.CatalogVersion,
            command.Items.Count,
            totalAdded,
            totalUpdated,
            nowUtc);
    }

    private static LabCatalogItemDto MapToDetailDto(LabCatalogItem c) =>
        new(
            c.Id,
            c.Code,
            c.Name,
            c.Category,
            c.SpecimenType,
            c.ContainerType,
            c.IsPanel,
            c.TurnaroundMinutes,
            c.IsActive,
            c.CatalogVersion,
            c.Description,
            c.CreatedAtUtc,
            c.Parameters.Select(p => new LabCatalogParameterDto(
                p.Id,
                p.LabCatalogItemId,
                p.Code,
                p.Name,
                p.Unit,
                p.ReferenceRangeLow,
                p.ReferenceRangeHigh,
                p.CriticalLow,
                p.CriticalHigh,
                p.ValueType,
                p.SortOrder)).ToList());
}
