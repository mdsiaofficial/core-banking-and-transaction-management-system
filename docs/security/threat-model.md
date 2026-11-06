# Threat Model & Security Architecture

**System:** Core Banking & Transaction Management System  
**Standard:** OWASP ASVS Level 2, OWASP API Security Top 10 (2023), STRIDE Threat Modeling  
**Version:** 1.0.0  
**Last Updated:** 2026-11-06  

---

## 1. System Overview & Scope

The Core Banking and Transaction Management System is a modular monolith built on .NET 10, PostgreSQL 18, and Angular 22. It provides retail banking services (account opening, savings/current accounts, deposits, withdrawals, intra-bank transfers) and back-office management (account freezing, customer suspension, transaction reversals, audit review, and reconciliation).

The primary engineering goal is to demonstrate **financial transaction integrity, non-repudiation, strict authorization, and ACID double-entry accounting**.

### 1.1 Assets Under Protection
1. **Financial Ledger & Balances:** Immutable ledger entries, journal transactions, account balances, and cash pools.
2. **Customer Identity & PII:** Passwords (hashes), email addresses, phone numbers, KYC status, physical addresses.
3. **Session & Auth Credentials:** JWT access tokens, refresh tokens, signing keys, password reset tokens.
4. **Audit Logs:** Tamper-evident records of administrative actions, authentication attempts, and financial postings.
5. **Business Logic & Invariants:** Zero-sum journal invariant ($\sum \text{Debits} = \sum \text{Credits}$), non-negative balances subject to overdraft limits, single-reversal constraint.

### 1.2 Threat Actors & Personas
- **External Attacker (Unauthenticated):** Exploits public endpoints (register, login, refresh) to perform credential stuffing, brute force, token forgery, or DDoS.
- **Malicious Customer (Authenticated):** Attempts Broken Object Level Authorization (BOLA/IDOR) to view or transact on other customers' accounts, attempts race-condition double spends, or abuses transfer limits.
- **Compromised Auditor (Read-Only):** Possesses read permissions; attempts privilege escalation to initiate transfers or alter logs.
- **Malicious/Rogue Administrator:** Attempts unauthorized reversals, account freezing without justification, or deletion of incriminating audit trails.
- **Man-in-the-Middle (Network Attacker):** Attempts eavesdropping, token interception, or replay attacks.

---

## 2. Trust Boundaries & Data Flow Diagram (DFD)

```
[ Public Internet / Untrusted Network ]
                    │
                    ▼  (HTTPS / TLS 1.3, CSP, HSTS)
       ┌───────────────────────────┐
       │     Reverse Proxy / Nginx │
       └─────────────┬─────────────┘
                     │ (Trust Boundary 1: Perimeter)
                     ▼
       ┌───────────────────────────┐
       │ ASP.NET Core Web API      │
       │ - Security Headers        │
       │ - Rate Limiting (IP/User) │
       │ - JWT Bearer AuthN        │
       │ - RBAC + Resource AuthZ   │
       │ - RFC 9457 ProblemDetails │
       └─────────────┬─────────────┘
                     │ (Trust Boundary 2: Internal Application)
                     ▼
       ┌───────────────────────────┐
       │ Domain & Ledger Core      │
       │ - Double-Entry Engine     │
       │ - Row-Level Lock Ordering │
       │ - Idempotency Validation  │
       └─────────────┬─────────────┘
                     │ (Trust Boundary 3: Database & Persistence)
                     ▼
       ┌─────────────────────────────────────────────────────────┐
       │ PostgreSQL 18 Database                                  │
       │ - Immutability Triggers (transactions, ledger, audit)   │
       │ - Deferred Constraint Trigger (balanced journal)        │
       │ - Least Privilege Roles (bank_app vs bank_migrator)     │
       └─────────────────────────────────────────────────────────┘
```

---

## 3. STRIDE Threat Analysis & Mitigations

| Threat ID | STRIDE Category | Threat Description | Attack Vector / Scenario | Impact | Applied Mitigation | ASVS Ref |
|:---|:---|:---|:---|:---|:---|:---|
| **TH-01** | **Spoofing** | Credential stuffing & brute-force login attacks | Attacker submits automated login attempts against `/api/v1/auth/login`. | Account takeover, unauthorized access. | ASP.NET Core Identity account lockout policy (5 failed attempts locks for 15 minutes) + IP-based rate limiting (5 req/min). | V2.2, V2.3 |
| **TH-02** | **Spoofing** | JWT Access Token tampering / signature forgery | Attacker crafts a forged JWT with arbitrary `sub` or `roles: Admin` claims. | Full privilege escalation to Admin or victim customer. | Asymmetric/HMAC signing with strong secret keys, signature verification, strict issuer/audience validation, minimal claims (`sub`, `roles`, `customer_id`), short lifetime (15 mins). | V3.5 |
| **TH-03** | **Spoofing** | Refresh Token Theft and Replay | Stolen refresh token used to generate new access tokens indefinitely. | Persistent session hijacking without victim's knowledge. | Single-use rotating refresh tokens with **family-based reuse detection**. Stored securely as SHA-256 hash. If an already-used or revoked token is presented, the entire family is immediately invalidated. | V3.7 |
| **TH-04** | **Tampering** | Account Balance / Ledger Manipulation | SQL injection or unauthorized update directly modifying `accounts.balance` or `ledger_entries`. | Counterfeit money creation, financial loss. | Parameterized queries via EF Core / Npgsql. Database `BEFORE UPDATE OR DELETE` triggers raise exceptions on `ledger_entries` and `transactions`. Runtime user `bank_app` restricted to `INSERT, SELECT`. | V5.3, V10.2 |
| **TH-05** | **Tampering** | Unbalanced Journal Postings | Software bug or race condition creating single-legged postings where money vanishes or appears. | Ledger corruption, regulatory violation. | PostgreSQL `DEFERRABLE INITIALLY DEFERRED` constraint trigger validates line count $\ge 2$ and $\sum \text{Debits} = \sum \text{Credits}$ per currency before commit. | V10.3 |
| **TH-06** | **Tampering** | Race Condition / Double Spending | Attacker initiates simultaneous transfers on an account with funds for only one transfer. | Account overdrawn beyond overdraft limit. | Deterministic row-level pessimistic locking (`SELECT ... FOR UPDATE ORDER BY id`) inside a single database transaction. DB check constraint `CHECK (kind <> 'CUSTOMER' OR balance + overdraft_limit >= 0)`. | V13.1 |
| **TH-07** | **Tampering** | Idempotency Key Abuse & Payload Tampering | Attacker retries request with same `Idempotency-Key` but modified destination account or amount. | Fraudulent transfer executed under false idempotency guise. | SHA-256 fingerprint hash of normalized route + method + request body stored with key. Key reuse with different hash returns HTTP 422 Unprocessable Entity. | V13.2 |
| **TH-08** | **Repudiation** | Denied Transaction or Admin Misconduct | Customer or Admin denies initiating a transfer, freeze, or reversal. | Inability to establish legal liability or audit trail. | Structured, append-only `audit_logs` table recording `occurred_at`, `actor_user_id`, `actor_role`, `actor_ip`, `correlation_id`, `action`, `entity_id`, `before`, `after`, and ledger `transaction_id`. Immutability trigger prevents deletion or update. | V8.1, V8.2 |
| **TH-09** | **Information Disclosure** | BOLA / IDOR (Broken Object Level Authorization) | Customer accesses `/api/v1/accounts/{foreign_id}` to view foreign balances and transactions. | Privacy breach, financial data leakage. | Resource-based authorization handler verifying `account.CustomerId == currentUser.CustomerId`. Query-level filtering (`WHERE customer_id = @currentCustomerId`) ensuring foreign resources return 403 Forbidden or 404 Not Found. | V4.1 |
| **TH-10** | **Information Disclosure** | Sensitive Data Leakage in Logs / Errors | Stack traces, passwords, or PII dumped to console or client. | Exposure of system internals and secrets. | RFC 9457 `ProblemDetails` sanitizes client error payloads. Serilog structured logging enriches correlation IDs while masking PII and passwords. | V7.4, V14.4 |
| **TH-11** | **Information Disclosure** | Clickjacking & Browser-Side Leaks | Web app loaded in malicious iframe or assets executed cross-origin. | Session hijacking, credential theft. | HTTP Security Headers: Content-Security-Policy (CSP), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `Strict-Transport-Security` (HSTS). | V14.4 |
| **TH-12** | **Denial of Service** | Endpoint Flooding & Brute Force | Distributed botnet spams transfer or login endpoints. | Service degradation, CPU exhaustion, database pool starvation. | Built-in ASP.NET Core rate limiting: Partitioned per-IP on auth endpoints (5 req/min), partitioned per-user on financial endpoints (30 req/min). | V11.1 |
| **TH-13** | **Elevation of Privilege** | Missing Function Level Access Control | Customer calls Admin endpoints (`/api/v1/admin/customers/{id}/suspend`, `/admin/audit-logs`). | Unauthorized customer suspension, account freezing, or data tampering. | Declarative endpoint role authorization policies (`[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "Admin,Auditor")]`). Default deny fallback policy on all API routes. | V4.2 |
| **TH-14** | **Elevation of Privilege** | Password Policy Bypass | Weak passwords easily cracked by offline or online dictionaries. | Account compromise. | Identity password policy enforced: minimum 12 characters, requiring uppercase, lowercase, digit, and special character. | V2.1 |

---

## 4. In-Depth Security Implementations

### 4.1 Authentication & Identity
- **Password Storage:** ASP.NET Core Identity with PBKDF2 with HMAC-SHA256, 100,000+ iterations.
- **Account Lockout:** Lockout threshold of 5 failed attempts; lockout duration of 15 minutes.
- **Access Tokens:** Stateless JWT tokens containing minimal claims:
  - `sub`: User ID (UUID)
  - `jti`: Token identifier (UUID)
  - `roles`: Role names (`Customer`, `Admin`, `Auditor`)
  - `customer_id`: Linked Customer profile UUID (if role is Customer)
- **Rotating Refresh Tokens with Family Reuse Detection:**
  - Token value: 256-bit cryptographically secure random string (`RandomNumberGenerator`).
  - Stored: SHA-256 hash in `refresh_tokens` table.
  - Lifecycle: When `/api/v1/auth/refresh` is called:
    1. If the token hash is not found $\rightarrow$ Reject (401).
    2. If the token is revoked or already replaced (i.e. `revoked_at != null` or `replaced_by_id != null`) $\rightarrow$ **Active attack detected:** Invalidate all tokens in the `family_id` immediately; log security alert.
    3. If valid: Mark current token revoked and set `replaced_by_id`, issue a new token with the same `family_id` and new expiry.

### 4.2 Authorization & IDOR/BOLA Protection
- **Role Hierarchy:**
  - `Customer`: Limited to own profile and owned accounts.
  - `Auditor`: Read-only access to all accounts, ledger entries, audit logs, and reconciliation findings.
  - `Admin`: Full operational access (account freeze/unfreeze, customer suspension, reversals, audit view).
- **Resource Ownership Verification:**
  - Application logic requires `IAuthorizationService.AuthorizeAsync(user, account, "AccountOwnerPolicy")`.
  - The handler verifies `account.CustomerId == user.GetCustomerId()` or `user.IsInRole("Admin")` / `user.IsInRole("Auditor")`.
  - Customers attempting to view or operate on accounts belonging to other customers are blocked with HTTP 403/404.

### 4.3 Rate Limiting Strategy
- **Per-IP Partitioning (Auth):**
  - Applied to `/api/v1/auth/login`, `/register`, `/forgot-password`.
  - Fixed window: 10 requests per minute per IP address.
- **Per-User Partitioning (Financial Transactions):**
  - Applied to `/api/v1/deposits`, `/withdrawals`, `/transfers`.
  - Sliding window: 30 requests per minute per authenticated User ID.
- **Global Concurrency Limiter:**
  - Limits queue length and concurrent executions to protect database connection pools.

### 4.4 Immutable Audit Logging
- Every administrative action, authentication failure, account lifecycle change, and financial operation emits an audit log.
- Schema:
  - `id`: UUIDv7
  - `occurred_at`: UTC timestamp
  - `actor_user_id`: UUID of acting user (or `system`)
  - `actor_role`: Role string
  - `actor_ip`: IPv4/IPv6 address
  - `correlation_id`: Distributed trace / request correlation identifier
  - `action`: Semantic string (e.g., `AUTH_LOGIN_SUCCESS`, `TRANSFER_POSTED`, `ACCOUNT_FROZEN`)
  - `entity_type`: Aggregate name (e.g., `Account`, `Customer`, `Transaction`)
  - `entity_id`: Aggregate UUID
  - `outcome`: `SUCCESS`, `DENIED`, `FAILED`
  - `before`: JSON snapshot of prior state (sensitive data masked)
  - `after`: JSON snapshot of new state
  - `metadata`: Contextual JSON (reversal reasons, transaction references)
- Database Trigger: `prevent_audit_log_tampering` rejects any `UPDATE` or `DELETE` with SQLSTATE `55000`.

---

## 5. Security Verification & Compliance Checklist
- [x] Password policy enforces $\ge 12$ chars, mixed case, numbers, symbols.
- [x] Account lockout activated after 5 failed authentication attempts.
- [x] JWT access tokens signed, validated, short-lived (15 min).
- [x] Refresh tokens hashed, rotated, and family-based reuse detection implemented.
- [x] Resource-based authorization handler protects against IDOR/BOLA.
- [x] Rate limiting configured on auth and transaction endpoints.
- [x] Security headers middleware injects CSP, HSTS, XFO, XCTO, Referrer-Policy.
- [x] Append-only audit logs with PostgreSQL immutability trigger.
- [x] Financial postings protected by deferred double-entry constraint trigger and pessimistic row locks.
