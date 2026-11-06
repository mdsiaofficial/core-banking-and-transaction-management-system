using Bank.Domain.Enums;

namespace Bank.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public string? ActorIp { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "AUTH_LOGIN_SUCCESS", "TRANSFER_POSTED"
    public string EntityType { get; set; } = string.Empty; // e.g., "Account", "Transaction", "User"
    public string? EntityId { get; set; }
    public AuditOutcome Outcome { get; set; } = AuditOutcome.Success;
    public string? Before { get; set; } // JSON snapshot (jsonb)
    public string? After { get; set; }  // JSON snapshot (jsonb)
    public string? Metadata { get; set; } // JSON metadata (jsonb)
    public string? PrevHash { get; set; } // For hash chaining
    public string? RowHash { get; set; }
}
