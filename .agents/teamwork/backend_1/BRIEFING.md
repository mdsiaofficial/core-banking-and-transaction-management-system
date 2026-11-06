# BRIEFING — 2026-11-06T06:59:00Z

## Mission
Implement the modular monolith core banking and double-entry ledger engine in .NET 10 / PostgreSQL 18 with genuine domain invariants, idempotent transaction processing, deadlock-free row locking, admin reversals, and automated reconciliation.

## 🔒 My Identity
- Archetype: Backend Engineer
- Roles: implementer, qa, specialist
- Working directory: /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/backend_1/
- Original parent: 11bda449-846f-4756-8ed7-1c02f0476b25
- Milestone: M1-M3 Backend Core

## 🔒 Key Constraints
- Pure domain model in `Bank.Domain` (no EF Core or infrastructure dependencies).
- Strict double-entry ledger: every transaction has >= 2 entries, sum(debits) = sum(credits) per currency.
- Money represented as decimal + Currency BDT; never float/double.
- PostgreSQL 18 database-level integrity: DEFERRABLE INITIALLY DEFERRED constraint trigger for balanced journal; BEFORE UPDATE OR DELETE immutability triggers on transactions and ledger_entries; CHECK for balance + overdraft_limit >= 0; UNIQUE(account_id, account_seq); UNIQUE(user_id, key).
- Concurrency: pessimistic row locking in deterministic account ID order (`SELECT ... FOR UPDATE ORDER BY id`) for deadlock freedom.
- Idempotency-Key validation with SHA256 request hashing, replay on match, 422 on payload change, 409 on in-progress.
- No dummy/facade implementations; genuine financial logic.

## Current Parent
- Conversation ID: 11bda449-846f-4756-8ed7-1c02f0476b25
- Updated: 2026-11-06T06:59:00Z

## Task Summary
- **What to build**:
  1. `Bank.Domain`: Pure domain entities, value objects (Money BDT, 13-digit Luhn check digit AccountNumber, IdempotencyKey, TransactionReference), domain services (PostingService, TransferPolicy, AccountNumberGenerator), domain exceptions, and enums.
  2. `Bank.Infrastructure`: EF Core 10 BankDbContext with snake_case naming conventions, UUIDv7 IDs, raw SQL migrations (001_schema.sql, 002_triggers.sql, 003_seed_chart_of_accounts.sql), database-level constraint triggers, immutability triggers, overdraft checks, account and payment services, outbox messages, and reconciliation engine.
  3. `Bank.Application` & `Bank.Api`: Idempotency filter/middleware (SHA-256 payload hashing, 400 on missing key, 422 on mismatch, 409 on in-flight, replay with header), money movement endpoints (/deposits, /withdrawals, /transfers), admin reversal (/transactions/{id}/reversal), reconciliation runs (/admin/reconciliation/runs), trial balance, keyset pagination, CSV export, and RFC 9457 ProblemDetails error handling.
  4. `Bank.Migrator`: Dedicated console CLI running SQL migrations and seeding initial chart of accounts and demo data.
- **Success criteria**: Zero compiler warnings/errors, 100% test pass rate, verified balanced journal and immutability triggers.
- **Interface contracts**: PROJECT.md and PLAN.md §4, §5, §6, §7, §8, §9.
- **Code layout**: `backend/src/Bank.Domain`, `backend/src/Bank.Application`, `backend/src/Bank.Infrastructure`, `backend/src/Bank.Api`, `backend/src/Bank.Migrator`.

## Key Decisions Made
- Wrapped user-initiated database transactions with EF Core `CreateExecutionStrategy().ExecuteAsync` to maintain full compatibility with `EnableRetryOnFailure`.
- Replaced EF Core `ExecuteSqlRawAsync` with direct ADO.NET `DbCommand.ExecuteNonQueryAsync` for executing DDL scripts containing curly braces (`{}`) in JSON and trigger syntax.
- Switched default `ConnectionStrings:DefaultConnection` host in `appsettings.json` to `localhost` for seamless local test execution while container environments receive `Host=postgres` via environment variables.

## Artifact Index
- `.agents/teamwork/backend_1/DISPATCH.md` — Dispatch task instructions
- `.agents/teamwork/backend_1/progress.md` — Liveness and task progress
- `.agents/teamwork/backend_1/handoff.md` — Final handoff report

## Change Tracker
- **Files modified**:
  - `backend/src/Bank.Domain/*`: Domain entities, value objects, domain services, exceptions, enums.
  - `backend/src/Bank.Infrastructure/*`: DbContext, configurations, SQL migrations, triggers, application services, migrator.
  - `backend/src/Bank.Api/*`: Minimal API endpoints, middleware, filters, security locator, Program.cs.
  - `backend/src/Bank.Migrator/*`: Migration CLI runner.
  - `backend/tests/Bank.Api.IntegrationTests/*`: Fixtures, Testcontainers configuration, test suites.
- **Build status**: PASS (0 Warnings, 0 Errors).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: PASS — 122 tests executed, 122 passed, 0 failed, 0 skipped.
- **Lint status**: PASS (`dotnet format --verify-no-changes` passed with zero violations).
- **Tests added/modified**: Full integration test coverage against real PostgreSQL 18 container including constraint triggers, immutability triggers, idempotency replay, and admin reversals.

## Loaded Skills
- None specified.
