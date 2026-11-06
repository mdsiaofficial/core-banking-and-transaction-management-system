# Original User Request

## Initial Request — 2026-11-06T05:52:38Z

invoke a team of 5 subagent: frontend, backend, devops, tester, security. Read PLAN.md (/home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md) and delegate works between those agents to collaboratively work together and build the complete core banking and transaction management system project as a showcase demonstrating financial-transaction integrity.

Working directory: /home/ashiq/work/core-banking-and-transaction-management-system
Integrity mode: development

Reference material: /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md

## Requirements

### R1. Core Banking & Ledger Engine
Implement the backend modular monolith and database per PLAN.md covering MVP (§3.1) and Should-have (§3.2) capabilities: customer identity and accounts, idempotent deposits, withdrawals, and intra-bank transfers, immutable double-entry ledger posting, administrative transaction reversals, automated balance reconciliation, daily limits, and outbox event dispatch.

### R2. Banking Web Application
Implement the responsive web client per PLAN.md covering customer workflows (self-registration, authentication, account overview, transfer wizard with idempotency key generation, transaction history with keyset/cursor pagination, and CSV statement download) and administrative back-office workflows (customer management, account freezing/unfreezing, reversals, audit log viewer, and reconciliation dashboard).

### R3. Automated Test Suites
Deliver comprehensive automated test suites including domain unit tests, property-based tests verifying conservation of money and debits-equal-credits invariants, integration tests with real PostgreSQL, a concurrency test harness verifying zero lost updates and deadlock freedom under high-contention concurrent transfers, and Playwright end-to-end tests for customer and admin flows.

### R4. Containerized DevOps & Security
Provide a complete Docker Compose environment (database, migration runner, backend API, frontend web, and dev mail catcher), automated migrations and seed data, JWT authentication with rotating refresh tokens, role-based and resource-based authorization, rate limiting, and structured audit logging.

## Acceptance Criteria

### Functional & Ledger Integrity
- [ ] Customer account opening, deposit, withdrawal, and intra-bank transfer operations succeed through both API and web interface, enforcing balance checks and overdraft limits.
- [ ] Every financial posting creates an immutable transaction record with at least two ledger entries where sum of debits equals sum of credits in the database.
- [ ] Database-level protections (triggers or permissions) prevent any UPDATE or DELETE operations on ledger entries and transaction records.
- [ ] Admin transaction reversal creates a linked compensating transaction without modifying or deleting original entries.
- [ ] Automated reconciliation job validates that stored account balances match the sum of their ledger entries and reports any discrepancies.
- [ ] Repeated requests with the same Idempotency-Key return the original response without executing duplicate transactions or balance updates.

### Security & Access Control
- [ ] Authentication endpoints issue JWT access tokens and implement rotating refresh tokens with reuse detection.
- [ ] RBAC and resource-ownership authorization checks prevent customers from accessing other customers' accounts or accessing administrative endpoints.
- [ ] All financial transactions and security-sensitive administrative operations generate structured audit log entries.

### Automated Testing & Verification
- [ ] Automated unit test suites execute and pass with zero failures.
- [ ] Property-based tests pass verifying ledger conservation across randomized transaction sequences.
- [ ] Integration tests using real PostgreSQL in containers pass for all major endpoints and workflows.
- [ ] Concurrency test harness with parallel concurrent transfers across shared accounts completes with zero lost updates, zero deadlocks, and verified balance integrity.
- [ ] Playwright end-to-end test suite passes for customer and administrative journeys.

### Deployment & Environments
- [ ] `docker compose up` starts all required services (database, migrator, backend API, frontend web, and mail service) and reports all containers healthy.
- [ ] Transaction history supports cursor-based pagination and CSV statement export.
