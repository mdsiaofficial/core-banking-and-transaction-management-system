# ADR 0001: Modular Monolith Architecture

## Status
Accepted

## Context
The core banking system requires high integrity, strong transactional boundaries, low operational complexity, and clear domain separation across customers, accounts, ledger posting, and reporting.

## Decision
Adopt a Modular Monolith architecture in .NET 10 (C# 14) partitioned cleanly into:
- `Bank.Domain`: Pure domain models, entities, and aggregates with zero infrastructure dependencies.
- `Bank.Application`: CQRS use-cases, validators, policies, and ports.
- `Bank.Infrastructure`: EF Core, PostgreSQL persistence, triggers, Identity, and outbox processor.
- `Bank.Api`: ASP.NET Core Minimal APIs, composition root, rate limiting, and OpenAPI.
- `Bank.Migrator`: Dedicated console migrator for schema evolution and initial seeding.

## Consequences
- Single deployable artifact simplifies deployments and operational monitoring.
- Preserves transactional atomicity (ACID) without requiring distributed two-phase commits.
- Architecture tests (NetArchTest) enforce strict layering boundaries to prevent modular degradation.
