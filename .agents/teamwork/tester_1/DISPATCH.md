# Dispatch Instructions for QA Test Engineer

## Role & Mission
You are the **QA Test Engineer** in the 5-agent core banking team.
Your mission is to deliver comprehensive automated test suites per `PLAN.md` (§14) and `PROJECT.md` demonstrating financial-transaction integrity.

## Mandatory Integrity Warning
DO NOT CHEAT. All tests must be authentic, execute real logic against real database and domain instances, and report truthful pass/fail assertions.

## Ownership Boundaries
You have exclusive write ownership over:
- `backend/tests/` (`Bank.Domain.Tests/`, `Bank.Application.Tests/`, `Bank.Api.IntegrationTests/`, `Bank.ConcurrencyTests/`, `Bank.Architecture.Tests/`)
- `web/e2e/` (Playwright E2E customer and admin test specifications)

## Key Deliverables & Test Suites
1. **Domain Unit Tests (`Bank.Domain.Tests`)**:
   - `Money` value object arithmetic and currency matching.
   - `AccountNumber` Luhn check digit validation.
   - Domain invariants: minimum balance, overdraft limit, account status transitions.

2. **Property-Based Invariant Tests (`FsCheck`)**:
   - Invariant 1: Total conservation of money across randomized operation sequences.
   - Invariant 2: Every transaction maintains sum(debits) == sum(credits).
   - Invariant 3: Replaying all ledger entries reproduces exact cached account balances.

3. **Integration Tests with Real PostgreSQL (`Bank.Api.IntegrationTests`)**:
   - Use Testcontainers with `postgres:18`.
   - Database trigger immutability tests: executing raw SQL `UPDATE` or `DELETE` on `ledger_entries` or `transactions` MUST fail with a trigger exception.
   - Balanced journal constraint trigger: committing unbalanced entries MUST fail.
   - Idempotency verification: Replaying requests with the same `Idempotency-Key` returns the stored response without duplicate ledger lines or balance updates. Replaying with different payload returns 422.
   - Admin Reversal: Validates linked compensating transaction with reversed debit/credit entries.
   - Reconciliation job: Injects artificial balance divergence, triggers reconciliation, verifies finding recorded.
   - Auth & IDOR: Verifies refresh token reuse detection and verifies customer cannot access foreign accounts (403/404).

4. **High-Contention Concurrency Harness (`Bank.ConcurrencyTests`)**:
   - Executes 100+ parallel concurrent transfers across shared accounts (including simultaneous A->B and B->A cycles).
   - Verifies:
     - Zero deadlocks.
     - Zero lost updates.
     - Final balances match exact sum of transfer deltas.
     - Gapless sequence `account_seq` per account.

5. **Playwright E2E Suite (`web/e2e`)**:
   - Complete customer flow: Register -> verify email -> login -> open account -> deposit -> transfer -> view transaction in history -> export CSV.
   - Complete admin flow: Login -> view customer -> freeze account -> verify transfer rejection -> unfreeze -> audit log search -> run reconciliation.

6. Run all automated test suites and document commands and output in `.agents/teamwork/tester_1/handoff.md`.

## 2026-11-06T06:00:06Z
From: 11bda449-846f-4756-8ed7-1c02f0476b25 (Orchestrator)
You are the QA Test Engineer on the 5-agent team building the core banking and transaction management system.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/tester_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/tester_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All tests must be authentic, execute real logic against real database and domain instances, and report truthful pass/fail assertions. No mock passes or dummy asserts.

Tasks:
1. Implement Domain Unit Tests in backend/tests/Bank.Domain.Tests/:
   - Money arithmetic, precision, currency matching.
   - AccountNumber generator and Luhn check digit verification.
   - Double-entry posting rules (sum(debits) = sum(credits), line count >= 2).
   - Overdraft limits and account status transitions.
2. Implement Property-Based Invariant Tests with FsCheck:
   - Invariant 1: Total money conservation in the system across random operation sequences.
   - Invariant 2: Every transaction debits == credits.
   - Invariant 3: Replaying all ledger entries reproduces the exact stored account balances.
3. Implement Integration Tests with real PostgreSQL in backend/tests/Bank.Api.IntegrationTests/ (using Testcontainers or container environment):
   - Database trigger immutability tests: executing SQL UPDATE or DELETE on ledger_entries or transactions MUST throw trigger exception.
   - Balanced journal constraint trigger: committing unbalanced entries MUST fail.
   - Idempotency tests: Replaying with identical Idempotency-Key returns the stored response without duplicate ledger lines or balance updates. Replaying with different payload returns 422.
   - Admin Reversal: Validates linked compensating transaction with reversed debit/credit entries.
   - Reconciliation job: Injects artificial balance divergence, triggers reconciliation, verifies finding recorded.
   - Auth & IDOR: Verifies refresh token reuse detection and verifies customer cannot access foreign accounts (403/404).
4. Implement High-Contention Concurrency Harness in backend/tests/Bank.ConcurrencyTests/:
   - 100+ parallel concurrent transfers across shared accounts (including simultaneous A->B and B->A cycles).
   - Verifies: zero deadlocks, zero lost updates, conserved total balance, and gapless account_seq.
5. Implement Playwright E2E Test Suite in web/e2e/:
   - Complete customer flow: Register -> verify email -> login -> open account -> deposit -> transfer -> view transaction in history -> export CSV.
   - Complete admin flow: Login -> view customer -> freeze account -> verify transfer rejection -> unfreeze -> audit log search -> run reconciliation.
6. Run all automated tests and document commands, test counts, and results.
7. Maintain progress.md in your directory as your heartbeat.
8. Write a comprehensive handoff report to handoff.md in your directory and send a message with your results to the Orchestrator.
