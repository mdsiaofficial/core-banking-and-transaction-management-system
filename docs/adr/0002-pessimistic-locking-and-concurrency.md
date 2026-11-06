# ADR 0002: Pessimistic Row Locking and Idempotency Strategy

## Status
Accepted

## Context
High-concurrency transfers between shared accounts can lead to race conditions, lost updates, and deadlocks if accounts are locked inconsistently. In addition, duplicate requests across transient network retries risk double-charging.

## Decision
1. **Ordered Pessimistic Locking**: All transfer workflows lock debit and credit account balance rows via `SELECT ... FOR UPDATE` ordered strictly by `account_id` (lexicographical UUID order). This guarantees deadlock-free concurrent executions.
2. **Idempotency Keys**: All mutating endpoints require an `Idempotency-Key` header (UUIDv7 or UUIDv4). Results are cached in an `idempotency_keys` table within the same transaction to guarantee exactly-once processing.

## Consequences
- Guaranteed zero lost updates and deadlock immunity under parallel load.
- Consistent idempotency guarantees for client retries without financial duplicate postings.
