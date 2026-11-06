# Project: Core Banking & Transaction Management System

## Architecture
- Modular monolith in .NET 10 (C# 14) + PostgreSQL 18 + Angular 22
- Backend projects:
  - `Bank.Domain`: Pure domain model, aggregates (`Customer`, `Account`, `LedgerTransaction`), value objects (`Money`, `AccountNumber`, `IdempotencyKey`, `TransactionReference`), domain services (`PostingService`, `TransferPolicy`, `AccountNumberGenerator`).
  - `Bank.Application`: CQRS feature slices for Auth, Customers, Accounts, Deposits, Withdrawals, Transfers, Reversals, Statements, Reconciliation.
  - `Bank.Infrastructure`: EF Core 10, Npgsql, snake_case mapping, PostgreSQL triggers & constraint triggers, outbox processor, email service.
  - `Bank.Api`: ASP.NET Core Minimal APIs, Scalar OpenAPI, RFC 9457 ProblemDetails, rate limiting, Serilog, OpenTelemetry.
  - `Bank.Migrator`: Database migrations runner, initial seeding (chart of accounts, roles, initial admin, cash pools).
- Frontend application:
  - Standalone Angular 22 components, signals-first state, Tailwind CSS v4, CDK, client interceptors for JWT auth/refresh, idempotency key UUIDs, ProblemDetails error mapping.
- Test projects:
  - `Bank.Domain.Tests` (xUnit v3)
  - `Bank.Application.Tests` / `Bank.PropertyTests` (FsCheck)
  - `Bank.Api.IntegrationTests` (Testcontainers + real PostgreSQL 18)
  - `Bank.ConcurrencyTests` (Parallel transfers across shared accounts)
  - `web/e2e` (Playwright customer and admin test suite)

## Code Layout & Ownership Boundaries
- Root configuration: `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig` -> Owned by DevOps
- Deployments & Containers: `Dockerfile`s, `docker-compose.yml`, `deploy/`, `.github/workflows/` -> Owned by DevOps
- Backend core: `backend/src/Bank.Domain`, `backend/src/Bank.Application`, `backend/src/Bank.Infrastructure`, `backend/src/Bank.Api`, `backend/src/Bank.Migrator` -> Owned by Backend Engineer
- Security modules & configs: JWT configuration, refresh token rotation/reuse detection, RBAC policies, resource ownership handlers, security headers, audit logging schema & middleware -> Owned by Security Specialist in coordination with Backend
- Frontend client: `web/` (Angular 22, Tailwind v4, services, components) -> Owned by Frontend Engineer
- Test suites: `backend/tests/` and `web/e2e/` -> Owned by QA Test Engineer

## Feature Inventory
| # | Feature | Description | Milestone | Specialist | Source |
|---|---------|-------------|-----------|------------|--------|
| 1 | Scaffolding & DevOps Stack | Build files, Dockerfiles, compose, postgres, mailpit, nginx, CI | DONE | DevOps | PLAN §15 |
| 2 | Identity & Authentication | Register, verify email, login, refresh token rotation/reuse detection | DONE | Security/Backend | PLAN §4.1, §10 |
| 3 | Accounts Management | Open, view, freeze/unfreeze, close, account number generator (Luhn) | DONE | Backend | PLAN §4.2 |
| 4 | Double-Entry Ledger Core | Transactions, ledger_entries, balanced journal trigger, immutability triggers | DONE | Backend | PLAN §5, §6 |
| 5 | Money Movement (Idempotent) | Deposits, withdrawals, transfers, Idempotency-Key handling, ordered row locking | DONE | Backend | PLAN §4.3, §7 |
| 6 | Audit Logging & Security | Append-only audit_logs, resource authorization, rate limiting, threat model | DONE | Security | PLAN §10, §11 |
| 7 | Reversals & Reconciliation | Admin reversal (compensating entries), automated balance reconciliation | DONE | Backend | PLAN §4.4, §7.6, §7.7 |
| 8 | Banking Web App (Customer) | Auth, account cards, transfer wizard + idempotency, history, CSV statements | DONE | Frontend | PLAN §12 |
| 9 | Banking Web App (Admin) | Customer mgmt, account freezing, reversal dialog, audit logs, reconciliation | DONE | Frontend | PLAN §12 |
| 10 | Automated Test Suites | Domain unit, FsCheck property, Testcontainers integration, concurrency harness | DONE | Tester | PLAN §14 |
| 11 | Playwright E2E Tests | Customer & admin user journeys against web & api | DONE | Tester | PLAN §14 |
| 12 | End-to-End Verification & Audit | All services healthy in Docker, zero lost updates, clean forensic audit | DONE | All/Orchestrator | PLAN §17 |

## Interface Contracts & Shared Standards
### API Base & JSON
- Base URL: `/api/v1`
- JSON format: `camelCase` keys
- Money values: serialized as decimal strings (e.g. `"1000.0000"` or `"250.50"`) + `currency: "BDT"`
- Idempotency Header: `Idempotency-Key: <UUID>` required on all POST requests to `/deposits`, `/withdrawals`, `/transfers`, `/admin/transactions/{id}/reversal`.
- Error format: RFC 9457 `application/problem+json` with `error_code`, `title`, `status`, `detail`, `trace_id`.

### DB Schema Standards
- PostgreSQL 18
- `snake_case` table and column names
- Primary keys: UUIDv7
- Immutable tables (`transactions`, `ledger_entries`, `audit_logs`): `BEFORE UPDATE OR DELETE` triggers raise exception
- Double-entry trigger: `DEFERRABLE INITIALLY DEFERRED` constraint trigger validates line count >= 2 and `SUM(debits) = SUM(credits)` per currency on commit
