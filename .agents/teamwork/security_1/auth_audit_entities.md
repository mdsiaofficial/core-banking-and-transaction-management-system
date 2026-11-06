# Entity Specifications: RefreshToken and AuditLog

## 1. RefreshToken Entity (`Bank.Domain.Entities.RefreshToken` or `Bank.Infrastructure.Auth.RefreshToken`)

```csharp
namespace Bank.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty; // SHA-256 hex string (64 chars)
    public Guid FamilyId { get; set; } // Identifies the rotation chain
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public Guid? ReplacedById { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

### EF Core Configuration for `RefreshToken`
- Table: `refresh_tokens`
- Columns:
  - `id`: PK, uuid
  - `user_id`: uuid, FK to `users(id)`
  - `token_hash`: text / varchar(64), required, unique/indexed
  - `family_id`: uuid, indexed
  - `expires_at`: timestamptz, required
  - `created_at`: timestamptz, required
  - `created_by_ip`: text / varchar(45)
  - `user_agent`: text
  - `revoked_at`: timestamptz, nullable
  - `revoked_by_ip`: text / varchar(45)
  - `replaced_by_id`: uuid, nullable, self-referencing FK

---

## 2. AuditLog Entity (`Bank.Domain.Entities.AuditLog` or `Bank.Infrastructure.Audit.AuditLog`)

```csharp
namespace Bank.Domain.Entities;

public enum AuditOutcome
{
    Success,
    Denied,
    Failed
}

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
```

### EF Core Configuration for `AuditLog`
- Table: `audit_logs`
- Immutability trigger:
  ```sql
  CREATE OR REPLACE FUNCTION prevent_audit_log_modification()
  RETURNS TRIGGER AS $$
  BEGIN
      RAISE EXCEPTION 'UPDATE and DELETE operations are not permitted on audit_logs table (append-only).'
      USING ERRCODE = '55000';
  END;
  $$ LANGUAGE plpgsql;

  CREATE TRIGGER trg_audit_logs_immutable
  BEFORE UPDATE OR DELETE ON audit_logs
  FOR EACH ROW
  EXECUTE FUNCTION prevent_audit_log_modification();
  ```
- Indexes:
  - `(entity_type, entity_id, occurred_at DESC)`
  - `(actor_user_id, occurred_at DESC)`
  - `(correlation_id)`
  - `(occurred_at DESC)`
