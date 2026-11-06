# Dispatch Instructions for Forensic Auditor

## Role & Mission
You are the **Forensic Auditor** (`teamwork_auditor`).
Your mission is to perform an exhaustive, independent integrity forensic audit on the entire Core Banking & Transaction Management System built across `backend/`, `web/`, and `deploy/` per `PLAN.md` and `PROJECT.md`.

## Mandatory Integrity Audit Checks
Verify that the codebase implements functionality authentically without cheating, hardcoding, dummy facades, or shortcuts:
1. **Financial & Ledger Integrity Verification**:
   - Verify that double-entry ledger posting is genuine (every transaction generates at least two balanced ledger entries).
   - Inspect PostgreSQL migrations (`backend/src/Bank.Infrastructure/Persistence/Migrations/`) for:
     - `DEFERRABLE INITIALLY DEFERRED` constraint trigger verifying `sum(debits) = sum(credits)` per currency and line count >= 2.
     - Immutability trigger (`prevent_immutable_modification`) preventing UPDATE and DELETE on `transactions`, `ledger_entries`, and `audit_logs` (raising SQLSTATE 55000).
     - Overdraft CHECK constraint (`balance + overdraft_limit >= 0`).
     - Unique index on `(account_id, account_seq)` preventing lost updates.
   - Verify that account balance updates match the sum of debits and credits and are not hardcoded.
   - Verify that Admin Reversals create genuine linked compensating transactions without modifying original entries.
   - Verify that the automated reconciliation job performs real balance recalculations from entries against stored balances and trial balance equilibrium (`sum(debits) == sum(credits)`).
2. **Security & Concurrency Verification**:
   - Verify that row locking is deterministic (`ORDER BY id FOR UPDATE`) on transfer endpoints to ensure zero deadlocks under concurrency.
   - Verify that IdempotencyKey handling computes a real SHA-256 hash of the request payload, replays genuine stored responses on identical payload, and returns 422 on payload alteration.
   - Verify that JWT authentication is genuine, and refresh token rotation implements true family-based reuse detection (revoking family on reuse).
   - Verify that resource-based authorization (`account.CustomerId == currentUser.CustomerId`) genuinely blocks BOLA/IDOR foreign account access.
   - Verify that audit logging writes real structured records to `audit_logs`.
3. **Frontend Authenticity**:
   - Inspect `web/` components, forms, and interceptors (`AuthInterceptor`, `IdempotencyInterceptor`, `ErrorInterceptor`).
   - Confirm forms invoke actual API services without mocked bypasses.
4. **Test Suite Authenticity**:
   - Inspect test projects in `backend/tests/` (`Bank.Domain.Tests`, `Bank.Application.Tests`, `Bank.Api.IntegrationTests`, `Bank.ConcurrencyTests`, `Bank.Architecture.Tests`) and `web/e2e/`.
   - Confirm tests assert real business rules, invariants, and database errors rather than trivially passing assertions (`Assert.True(true)`).

## Deliverables
- Run verification checks and inspect code files.
- Report verdict: `CLEAN` or `INTEGRITY VIOLATION (with full evidence)`.
- Write your forensic report to `.agents/teamwork/auditor_1/handoff.md` and message the Orchestrator.

## 2026-11-06T07:06:05Z
You are the Forensic Auditor on the Core Banking and Transaction Management System project.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/auditor_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/auditor_1/DISPATCH.md.

Perform an exhaustive forensic audit on the codebase across backend/, web/, and deploy/:
1. Financial integrity: double-entry balance enforcement, PostgreSQL triggers (balanced journal trigger, immutability trigger with SQLSTATE 55000, overdraft checks, account_seq uniqueness).
2. Concurrency & Idempotency: deterministic row-locking (ORDER BY id FOR UPDATE), SHA-256 idempotency request hash check, replay behavior, 422 mismatch response.
3. Security: JWT token generation, refresh token single-use rotation with family reuse detection, IDOR/BOLA resource ownership checks, audit logging.
4. Test suites: verify test realism and assertions (no fake passes or empty asserts).
5. Output your verdict (CLEAN or INTEGRITY VIOLATION) in handoff.md and send a message with your findings to the Orchestrator.

