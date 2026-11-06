# Dispatch Log

## 2026-11-06T05:52:38Z
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
