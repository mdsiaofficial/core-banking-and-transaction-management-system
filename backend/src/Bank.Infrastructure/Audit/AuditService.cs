using Bank.Application.Audit;
using Bank.Domain.Entities;
using Bank.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Audit;

public class AuditService : IAuditService
{
    private readonly BankDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(BankDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                Id = Guid.CreateVersion7(),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorUserId = entry.ActorUserId,
                ActorRole = entry.ActorRole,
                ActorIp = entry.ActorIp,
                UserAgent = entry.UserAgent,
                CorrelationId = entry.CorrelationId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Outcome = entry.Outcome,
                Before = entry.Before,
                After = entry.After,
                Metadata = entry.Metadata
            };

            await _db.AuditLogs.AddAsync(log, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry for action {Action} on {EntityType}:{EntityId}",
                entry.Action, entry.EntityType, entry.EntityId);
            // Non-blocking for callers unless re-thrown; in banking, we ensure exceptions are logged
        }
    }

    public async Task<IReadOnlyList<AuditLogDto>> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var q = _db.AuditLogs.AsNoTracking();

        if (query.ActorUserId.HasValue)
        {
            q = q.Where(a => a.ActorUserId == query.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ActorRole))
        {
            q = q.Where(a => a.ActorRole == query.ActorRole);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            q = q.Where(a => a.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            q = q.Where(a => a.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            q = q.Where(a => a.Action == query.Action);
        }

        if (query.Outcome.HasValue)
        {
            q = q.Where(a => a.Outcome == query.Outcome.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            q = q.Where(a => a.CorrelationId == query.CorrelationId);
        }

        if (query.From.HasValue)
        {
            q = q.Where(a => a.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            q = q.Where(a => a.OccurredAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Cursor) && DateTimeOffset.TryParse(query.Cursor, out var cursorDate))
        {
            q = q.Where(a => a.OccurredAt < cursorDate);
        }

        var limit = Math.Clamp(query.Limit, 1, 100);

        var list = await q
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Take(limit)
            .Select(a => new AuditLogDto(
                a.Id,
                a.OccurredAt,
                a.ActorUserId,
                a.ActorRole,
                a.ActorIp,
                a.UserAgent,
                a.CorrelationId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Outcome,
                a.Before,
                a.After,
                a.Metadata))
            .ToListAsync(ct);

        return list;
    }
}
