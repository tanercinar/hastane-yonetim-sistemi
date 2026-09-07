using HospitalManagement.BuildingBlocks.Audit;
using HospitalManagement.Modules.AuditPrivacy.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HospitalManagement.Modules.AuditPrivacy.Infrastructure.Persistence;

public sealed partial class AuditLogService(
    AuditPrivacyDbContext dbContext,
    ILogger<AuditLogService> logger) : IAuditEventPublisher, IAuditLogReader
{
    private readonly AuditPrivacyDbContext _dbContext = dbContext;
    private readonly ILogger<AuditLogService> _logger = logger;

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Denetim kaydi kaydedilirken hata olustu. Action: {Action}, Resource: {Resource}")]
    private static partial void LogAuditPersistenceError(
        ILogger logger,
        Exception ex,
        string action,
        string resource);

    public async Task PublishAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        try
        {
            if (_dbContext.Database.IsNpgsql())
            {
                await PublishWithPostgreSqlChainLockAsync(auditEvent, cancellationToken);
            }
            else
            {
                await AppendEntryAsync(auditEvent, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAuditPersistenceError(_logger, ex, auditEvent.Action, auditEvent.TargetResourceType);
            throw;
        }
    }

    private async Task PublishWithPostgreSqlChainLockAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // A transaction-scoped advisory lock serializes the read-last-hash + insert
        // operation across all application instances without exposing audit content.
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(482979341);",
            cancellationToken);

        await AppendEntryAsync(auditEvent, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task AppendEntryAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        var lastEntry = await _dbContext.AuditLogs
            .OrderByDescending(log => log.ChainPosition)
            .Select(log => new { log.RecordHash, log.ChainPosition })
            .FirstOrDefaultAsync(cancellationToken);

        var entry = AuditLogEntry.FromEvent(
            auditEvent,
            lastEntry?.RecordHash,
            checked((lastEntry?.ChainPosition ?? 0) + 1));
        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> QueryLogsAsync(
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (filter.ActorUserId.HasValue)
        {
            query = query.Where(l => l.ActorUserId == filter.ActorUserId.Value);
        }

        if (filter.ActorPersonId.HasValue)
        {
            query = query.Where(l => l.ActorPersonId == filter.ActorPersonId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(l => l.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetResourceType))
        {
            query = query.Where(l => l.TargetResourceType == filter.TargetResourceType);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetResourceId))
        {
            query = query.Where(l => l.TargetResourceId == filter.TargetResourceId);
        }

        if (filter.Outcome.HasValue)
        {
            var outcomeString = filter.Outcome.Value.ToString();
            query = query.Where(l => l.Outcome == outcomeString);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc <= filter.ToUtc.Value);
        }

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var entries = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return entries.Select(e => new AuditEvent(
            e.Id,
            e.CreatedAtUtc,
            e.ActorUserId,
            e.ActorPersonId,
            e.ActorRole,
            e.ActorIpAddress,
            e.ActorUserAgent,
            e.Action,
            e.TargetResourceType,
            e.TargetResourceId,
            Enum.TryParse<AuditOutcome>(e.Outcome, true, out var outcome) ? outcome : AuditOutcome.Failed,
            e.Reason,
            e.CorrelationId,
            e.DetailsJson)).ToList();
    }

    public async Task<bool> VerifyTamperIntegrityAsync(
        Guid logId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);

        return entry is not null && entry.VerifyHashIntegrity();
    }
}
