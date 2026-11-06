using Bank.Domain.Enums;

namespace Bank.Application.Audit;

public record AuditLogQuery(
    Guid? ActorUserId = null,
    string? ActorRole = null,
    string? EntityType = null,
    string? EntityId = null,
    string? Action = null,
    AuditOutcome? Outcome = null,
    string? CorrelationId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 50,
    string? Cursor = null);
