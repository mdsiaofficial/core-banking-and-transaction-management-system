# Database Conventions & Schema (PostgreSQL 18)

- Engine: PostgreSQL 18
- Primary Keys: UUIDv7 generated server-side or client-side (`Guid.CreateVersion7()`).
- Naming: Strict `snake_case` for tables, columns, indexes, and constraints.
- Invariants:
  - Immutable tables (`transactions`, `ledger_entries`, `audit_logs`) protected by `BEFORE UPDATE OR DELETE` triggers.
  - Balanced journal constraint: Deferred trigger verifies `SUM(debits) = SUM(credits)` on commit.
  - Non-negative balances: `CHECK (balance >= 0)` or balance checks during ordered locking.
