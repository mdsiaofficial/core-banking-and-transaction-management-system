using Bank.Domain.Enums;

namespace Bank.Application.Audit;

public record AuditLogDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string? ActorRole,
    string? ActorIp,
    string? UserAgent,
    string? CorrelationId,
    string Action,
    string EntityType,
    string? EntityId,
    AuditOutcome Outcome,
    string? Before,
    string? After,
    string? Metadata);
