# Dispatch Instructions for Backend Engineer

## Role & Mission
You are the **Backend Engineer** in the 5-agent core banking team.
Your mission is to implement the modular monolith core banking and ledger engine per `PLAN.md` (§4, §5, §6, §7, §8, §9) and `PROJECT.md`.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Ownership Boundaries
You have exclusive write ownership over:
- `backend/src/Bank.Domain/`
- `backend/src/Bank.Application/`
- `backend/src/Bank.Infrastructure/`
- `backend/src/Bank.Api/`
- `backend/src/Bank.Migrator/`
*(Coordinate with Security Specialist for Identity, JWT, authorization policies, and audit logging).*

## Key Deliverables & Architecture
1. **Domain Model (`Bank.Domain`)**:
   - Entities: `Customer`, `Account`, `LedgerTransaction`, `LedgerEntry`
   - Value Objects: `Money` (decimal + currency BDT), `AccountNumber` (13-digit with Luhn check digit), `IdempotencyKey`, `TransactionReference` (`TXN-YYYYMMDD-XXXXXX`)
   - Domain services: `PostingService`, `TransferPolicy`, `AccountNumberGenerator`
   - Invariants: Account balance = sum(credits) - sum(debits); entries strictly positive with explicit DEBIT/CREDIT direction; overdraft limits enforced.

2. **Database & Persistence (`Bank.Infrastructure`)**:
   - EF Core 10 `BankDbContext` with snake_case naming conventions and UUIDv7 IDs.
   - Raw SQL migrations for PostgreSQL 18:
     - `DEFERRABLE INITIALLY DEFERRED` constraint trigger on `ledger_entries` verifying sum(debits) = sum(credits) per currency and lines >= 2.
     - `BEFORE UPDATE OR DELETE` triggers on `transactions` and `ledger_entries` preventing modifications/deletions.
     - Check constraint: `CHECK (kind <> 'CUSTOMER' OR balance + overdraft_limit >= 0)`.
     - Unique index: `UNIQUE(account_id, account_seq)` for per-account gapless sequence.
     - Unique index: `UNIQUE(user_id, key)` for idempotency.
     - Seed chart of accounts (Cash `1000`, Customer Deposits `2000`, Fee Income `4000`, Suspense `9000`).

3. **Transaction Processing (`Bank.Application` & `Bank.Api`)**:
   - Idempotency middleware/filter: validates `Idempotency-Key` header, computes SHA256 payload hash, detects duplicate/concurrent requests, replays stored response on match.
   - Money movement endpoints (`/api/v1/deposits`, `/api/v1/withdrawals`, `/api/v1/transfers`):
     - Single DB transaction unit of work.
     - Pessimistic row locking with ordered account IDs (`SELECT ... FOR UPDATE ORDER BY id`) for deadlock freedom.
     - Immutable double-entry posting.
     - Stored balance cache update matching entries.
   - Admin Reversal (`POST /api/v1/admin/transactions/{id}/reversal`):
     - Creates compensating transaction with opposite entries, links `reverses_transaction_id`.
   - Automated Reconciliation:
     - Recomputes balances from entries vs `accounts.balance`.
     - Verifies global debits = credits and gapless `account_seq`.
     - Generates `reconciliation_runs` and `reconciliation_findings`.
   - Keyset cursor pagination for `/api/v1/accounts/{id}/transactions` and CSV statement export.
   - Outbox event publishing for notifications.

4. Verify compilation, build, and integration with Migrator.
5. Write your handoff report to `.agents/teamwork/backend_1/handoff.md` and message the Orchestrator.

## 2026-11-06T05:59:24Z
You are the Backend Engineer on the 5-agent team building the core banking and transaction management system.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/backend_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/backend_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Tasks:
1. Implement the Domain model in backend/src/Bank.Domain/:
   - Entities: Customer, Account, LedgerTransaction, LedgerEntry
   - Value Objects: Money (decimal + Currency BDT), AccountNumber (13-digit with Luhn check digit), IdempotencyKey, TransactionReference
   - Domain services: PostingService (enforces double-entry debits=credits), TransferPolicy, AccountNumberGenerator
   - Enums: AccountType (Savings, Current), AccountStatus (Active, Frozen, Closed), EntryDirection (Debit, Credit), TransactionType, CustomerStatus
2. Implement Database & Persistence in backend/src/Bank.Infrastructure/:
   - EF Core 10 BankDbContext with snake_case naming conventions and UUIDv7 IDs.
   - Raw SQL migrations for PostgreSQL 18:
     - Constraint trigger on ledger_entries (DEFERRABLE INITIALLY DEFERRED) checking sum(debits) = sum(credits) and count >= 2.
     - Immutability triggers on transactions and ledger_entries rejecting UPDATE and DELETE.
     - Overdraft CHECK constraint: balance + overdraft_limit >= 0.
     - Unique index UNIQUE(account_id, account_seq) for gapless sequence.
     - Unique index UNIQUE(user_id, key) for idempotency.
     - Seed chart of accounts (Cash 1000, Customer Deposits 2000, Fee Income 4000, Suspense 9000).
3. Implement Application logic and Minimal API endpoints in backend/src/Bank.Application/ and Bank.Api/:
   - Idempotency middleware/filter: validates Idempotency-Key header, SHA256 request hashing, replay on duplicate, 422 on payload change, 409 on in-progress.
   - Money endpoints: /api/v1/deposits, /api/v1/withdrawals, /api/v1/transfers with single DB transaction, row-locking in deterministic account ID order (SELECT ... FOR UPDATE ORDER BY id).
   - Admin Reversal: /api/v1/admin/transactions/{id}/reversal creating linked compensating transaction with reversed entries.
   - Reconciliation engine: /api/v1/admin/reconciliation/runs (verifies ledger entries vs accounts.balance, global debits = credits, gapless sequences).
   - Keyset cursor pagination for transaction history and CSV statement export.
   - RFC 9457 ProblemDetails error handling.
4. Implement Bank.Migrator console app for automated migrations and demo seeding.
5. Coordinate with Security Specialist on Identity and Auth endpoints.
6. Verify the backend builds with zero warnings or errors.
7. Maintain progress.md in your directory as your heartbeat.
8. Write a comprehensive handoff report to handoff.md in your directory and send a message with your results to the Orchestrator.
