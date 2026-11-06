using Bank.Domain.Enums;

namespace Bank.Application.Audit;

public record AuditLogEntry(
    string Action,
    string EntityType,
    string? EntityId = null,
    Guid? ActorUserId = null,
    string? ActorRole = null,
    string? ActorIp = null,
    string? UserAgent = null,
    string? CorrelationId = null,
    AuditOutcome Outcome = AuditOutcome.Success,
    string? Before = null,
    string? After = null,
    string? Metadata = null);
