# Dispatch Instructions for Security Specialist

## Role & Mission
You are the **Security Specialist** in the 5-agent core banking team.
Your mission is to implement and enforce the security architecture, authentication, authorization, rate limiting, and audit logging per `PLAN.md` (§2, §4.1, §10, §11) and `PROJECT.md`.

## Mandatory Integrity Warning
DO NOT CHEAT. All security implementations must be genuine, resilient, and verified. A teamwork_auditor will independently inspect auth, token handling, and audit mechanisms.

## Ownership Boundaries
You have ownership over:
- Security and Auth architecture in `backend/src/Bank.Infrastructure/Auth/` and `Bank.Api/Security/`
- Audit logging infrastructure in `backend/src/Bank.Infrastructure/Audit/` and `Bank.Application/Audit/`
- Documentation: `docs/security/threat-model.md` and security runbooks

## Key Deliverables
1. **Authentication & Identity**:
   - ASP.NET Core Identity integration with password policies (min 12 characters, lockout after failed attempts).
   - JWT access token generation and validation with minimal, necessary claims (`sub`, `roles`, `customer_id`, etc.).
   - Refresh token rotation with family-based **reuse detection**:
     - `refresh_tokens` table (`id`, `user_id`, `token_hash`, `family_id`, `expires_at`, `revoked_at`, `replaced_by_id`, `created_by_ip`).
     - Presenting an already-used or revoked token revokes the entire token family immediately.
   - Endpoints: `POST /api/v1/auth/register`, `/verify-email`, `/login`, `/refresh`, `/logout`.

2. **Authorization & IDOR / BOLA Prevention**:
   - Role policies: `Customer`, `Admin`, `Auditor`.
   - Resource-based authorization handler: verify `account.CustomerId == currentUser.CustomerId`.
   - Query-level scoping to guarantee customer cannot view, query, or transact on other customers' accounts.
   - Admin endpoints restricted to `Admin` and read-only to `Auditor`.

3. **Rate Limiting & Security Headers**:
   - Per-IP rate limiting on authentication routes (prevent brute force).
   - Per-user rate limiting on financial transaction endpoints.
   - Security headers: CSP, HSTS, X-Content-Type-Options, Referrer-Policy, X-Frame-Options.

4. **Structured Audit Logging**:
   - Append-only `audit_logs` table (id, occurred_at, actor_user_id, actor_role, actor_ip, correlation_id, action, entity_type, entity_id, outcome, before, after, metadata).
   - Database trigger preventing UPDATE or DELETE on `audit_logs`.
   - Auditing for login, lockout, account opening, freezing/unfreezing, customer suspension, and all money movements (including denied/failed operations).
   - Search endpoint: `GET /api/v1/admin/audit-logs`.

5. **Threat Model**:
   - Author `docs/security/threat-model.md` covering STRIDE analysis and mitigations.

6. Write your handoff report to `.agents/teamwork/security_1/handoff.md` and notify the Orchestrator.

## 2026-11-06T05:59:37Z
From: 11bda449-846f-4756-8ed7-1c02f0476b25 (Orchestrator)
You are the Security Specialist on the 5-agent team building the core banking and transaction management system.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/security_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/security_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All security implementations must be genuine, resilient, and verifiable. A teamwork_auditor will independently inspect your deliverables.

Tasks:
1. Coordinate with Backend Engineer on Auth and Identity in backend/src/Bank.Infrastructure/Auth/ and Bank.Api/Security/:
   - Configure ASP.NET Core Identity with password policies (min 12 chars, failed attempt lockout).
   - Implement JWT token generator & validator with minimal, necessary claims (sub, roles, customer_id).
   - Implement rotating refresh tokens with family-based reuse detection:
     - refresh_tokens table (id, user_id, token_hash, family_id, expires_at, revoked_at, replaced_by_id, created_by_ip).
     - Revoke the entire token family if an already-used or revoked refresh token is presented.
   - Auth endpoints: /api/v1/auth/register, /verify-email, /login, /refresh, /logout.
2. Implement Authorization & IDOR/BOLA Prevention:
   - Role-based authorization policies (Customer, Admin, Auditor).
   - Resource-based authorization handler: account.CustomerId == currentUser.CustomerId. Ensure customers can NEVER access foreign accounts (return 403/404).
3. Implement Rate Limiting & Security Headers:
   - Rate limiting per IP on auth endpoints, per user on money endpoints.
   - Security headers: CSP, HSTS, X-Content-Type-Options, Referrer-Policy, X-Frame-Options.
4. Implement Structured Audit Logging:
   - Append-only audit_logs table (occurred_at, actor_user_id, actor_role, actor_ip, correlation_id, action, entity_type, entity_id, outcome, before, after, metadata).
   - Immutability trigger on audit_logs preventing UPDATE and DELETE.
   - Audit logging for authentication events, account lifecycle, all money movements, and reversals.
   - Audit log search endpoint: GET /api/v1/admin/audit-logs.
5. Create docs/security/threat-model.md with STRIDE analysis.
6. Verify security components build and pass compilation.
7. Maintain progress.md in your directory as your heartbeat.
8. Write a comprehensive handoff report to handoff.md in your directory and send a message with your results to the Orchestrator.

## 2026-11-06T06:05:13Z
From: a2f106f7-ab79-4daf-930d-9b30d00c04af (Backend Engineer)
**Context**: Backend and Security interface coordination
**Content**: Hi Security Specialist! I am implementing the core domain model and BankDbContext. For authentication, Identity tables (users, roles, refresh_tokens) and audit_logs will be integrated into BankDbContext. I will ensure BankDbContext has the required entity configurations and hooks, and the database schema includes the immutability trigger for audit_logs. Let's align on entity definitions for RefreshToken and AuditLog so our EF Core models match seamlessly.
**Action**: Please review or share your preferred entity properties/interfaces for RefreshToken and AuditLog when ready.

## 2026-11-06T06:37:51Z
From: a2f106f7-ab79-4daf-930d-9b30d00c04af (Backend Engineer)
**Context**: Integration test compilation error in RefreshTokenReuseDetectionTests.cs
**Content**: In `backend/tests/Bank.Api.IntegrationTests/Security/RefreshTokenReuseDetectionTests.cs`, line 23 calls `.UseInMemoryDatabase(...)`, but `Microsoft.EntityFrameworkCore.InMemory` is not referenced in the project (the project uses Testcontainers.PostgreSql).
**Action**: Please either switch to Testcontainers / SQLite or coordinate with DevOps to add the InMemory package if unit-testing in-memory is desired.



