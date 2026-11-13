# Core Banking & Transaction Management System — Backend

A portfolio-grade, simplified core banking platform built as a **modular monolith** in **.NET 10** using **Clean Architecture**.

Customers register, open savings/current accounts, deposit, withdraw, and transfer money. Every money movement is recorded in an **immutable double-entry ledger**, executed **atomically**, safe under **concurrency**, safe against **client retries (idempotency)**, and fully **auditable**.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Dependency Flow](#dependency-flow)
4. [Entry Point: `Program.cs`](#entry-point-programcs)
5. [How a Request Triggers an API](#how-a-request-triggers-an-api)
6. [All API Endpoints](#all-api-endpoints)
7. [Layers in Detail](#layers-in-detail)
8. [Security & Cross-Cutting Concerns](#security--cross-cutting-concerns)
9. [How to Run](#how-to-run)
10. [Request Lifecycle Summary](#request-lifecycle-summary)

---

## 1. Architecture Overview

The system follows **Clean Architecture** with a strict dependency rule: inner layers know nothing about outer layers.

```
Client (Angular / browser / curl)
        │
        ▼
┌─────────────────────────────────────┐
│  Bank.Api                           │
│  (ASP.NET Core — HTTP entry point)  │
│  Endpoints · Middleware · Filters   │
└──────────────┬──────────────────────┘
               │ references
               ▼
┌─────────────────────────────────────┐
│  Bank.Application                   │
│  (Use case contracts — interfaces   │
│   + DTOs)                           │
└──────────────┬──────────────────────┘
               │ references
               ▼
┌─────────────────────────────────────┐
│  Bank.Domain                        │
│  (Core model — entities, value      │
│   objects, enums, domain services)  │
└──────────────┬──────────────────────┘
               │ implemented by
               ▼
┌─────────────────────────────────────┐
│  Bank.Infrastructure                │
│  (Persistence — EF Core, PostgreSQL,│
│   Identity, JWT, service impls)     │
└─────────────────────────────────────┘

Bank.Migrator (separate console app)
  → uses Bank.Infrastructure to run EF Core migrations + seed demo data
```

**Design goals (from `PLAN.md`):**
- Money is never created or destroyed (debits = credits enforced by DB constraint + reconciliation)
- No lost updates / double spends (row locking with deterministic lock order)
- Retries are safe (`Idempotency-Key` handling)
- Everything is traceable (append-only audit log + immutable ledger + correlation IDs)
- Secure by default (ASP.NET Core Identity, JWT + refresh rotation, RBAC + resource-based authorization, rate limiting)

---

## 2. Project Structure

```
backend/
├── src/
│   ├── Bank.Api/                 # HTTP entry point (ASP.NET Core)
│   │   ├── Endpoints/            # Minimal API endpoint groups
│   │   ├── Filters/              # Endpoint filters (idempotency)
│   │   ├── Middleware/           # Exception handler, security headers
│   │   ├── Security/             # AuthZ policies, claims helpers, rate limiting
│   │   ├── Program.cs            # Application entry point
│   │   ├── appsettings.json
│   │   └── Bank.Api.csproj
│   ├── Bank.Application/         # Use case contracts (interfaces + DTOs)
│   │   ├── Accounts/             # IAccountService, AccountDtos
│   │   ├── Payments/             # IPaymentService, PaymentDtos
│   │   ├── Customers/            # ICustomerService
│   │   ├── Audit/                # IAuditService
│   │   ├── Reconciliation/       # IReconciliationService
│   │   ├── Idempotency/          # IIdempotencyService
│   │   ├── Common/
│   │   └── ApplicationMarker.cs
│   ├── Bank.Domain/              # Core business model (no external deps)
│   │   ├── Entities/             # Account, Customer, LedgerTransaction, LedgerEntry, ...
│   │   ├── ValueObjects/         # Money, AccountNumber, TransactionReference, IdempotencyKey
│   │   ├── Enums/                # AccountType, CustomerStatus, AuditOutcome, ...
│   │   ├── Services/             # IPostingService, ITransferPolicy, IAccountNumberGenerator
│   │   ├── Exceptions/           # DomainExceptions
│   │   ├── Common/               # SystemAccounts
│   │   └── DomainMarker.cs
│   ├── Bank.Infrastructure/      # Persistence & external concerns
│   │   ├── Data/                 # BankDbContext, DatabaseMigrator, Configurations, Scripts
│   │   ├── Auth/                 # ApplicationUser, JwtTokenService, RefreshTokenService
│   │   ├── Services/             # PaymentService, AccountService, CustomerService, ...
│   │   ├── DependencyInjection.cs
│   │   └── InfrastructureMarker.cs
│   └── Bank.Migrator/            # Standalone DB migration + seed console app
│       └── Program.cs
├── tests/
│   ├── Bank.Domain.Tests/
│   ├── Bank.Application.Tests/
│   ├── Bank.Api.IntegrationTests/
│   ├── Bank.ConcurrencyTests/
│   └── Bank.Architecture.Tests/
├── Bank.sln
├── Directory.Build.props
├── Directory.Packages.props
├── Dockerfile
├── Dockerfile.migrator
└── global.json
```

---

## 3. Dependency Flow

| Project | Responsibility | References |
|---------|---------------|------------|
| `Bank.Domain` | Entities, value objects, enums, domain services, exceptions | (none — innermost layer) |
| `Bank.Application` | Service interfaces + DTOs (use cases) | `Bank.Domain` |
| `Bank.Infrastructure` | EF Core DbContext, Identity, JWT, service implementations, migrations | `Bank.Domain`, `Bank.Application` |
| `Bank.Api` | HTTP entry point, endpoints, middleware, filters, security | `Bank.Domain` + `Bank.Application` + `Bank.Infrastructure` |
| `Bank.Migrator` | Standalone console app that runs DB migrations + seeds demo data | `Bank.Domain` + `Bank.Infrastructure` |

`Bank.Api` is the **only** project that knows about all the others. This keeps the domain pure and testable.

---

## 4. Entry Point: `Program.cs`

`Bank.Api/Program.cs` is where the application starts. It does three things:

1. **Configures services** (DI container)
2. **Configures the middleware pipeline** (request processing order)
3. **Maps endpoints** (URL → C# handler)

### Annotated `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);   // 1. Create host builder

// 2. Register services (DI container)
builder.Services.AddLogging();
builder.Services.AddInfrastructure(builder.Configuration);   // EF Core, DbContext, Identity, services
builder.Services.AddBankAuthorization();                      // JWT auth + policies
builder.Services.AddBankRateLimiting();                       // Rate limiting
builder.Services.AddProblemDetails();                         // RFC 9457 error format
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>(); // Global error handler
builder.Services.AddOpenApi();                                // Swagger/OpenAPI
builder.Services.AddCors(...);                                // CORS for Angular web client

var app = builder.Build();                                    // 3. Build the app

// 4. Configure the HTTP request pipeline (ORDER MATTERS)
app.UseExceptionHandler();                                    // Catch exceptions → ProblemDetails
app.UseSecurityHeaders();                                     // X-Frame-Options, CSP, HSTS, etc.
app.UseCors();
app.UseRateLimiter();                                         // Rate limit middleware
app.UseAuthentication();                                      // Validate JWT → populate User
app.UseAuthorization();                                       // Check policies/roles

// OpenAPI/Swagger in development
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Bank.Api v1");
    options.RoutePrefix = "swagger";
});

// 5. MAP ENDPOINTS (this is where routes are wired to handlers)
app.MapAuthEndpoints();      // /api/v1/auth/*
app.MapAccountEndpoints();   // /api/v1/accounts/*
app.MapPaymentEndpoints();   // /api/v1/deposits, /withdrawals, /transfers, /transactions/{id}
app.MapAdminEndpoints();     // /api/v1/admin/*
app.MapCustomerEndpoints();  // /api/v1/me/*
app.MapAuditEndpoints();     // /api/v1/admin/audit-logs/*

// Health check endpoints (no auth)
app.MapGet("/", ...);
app.MapGet("/health", ...);
app.MapGet("/health/live", ...);
app.MapGet("/health/ready", ...);

app.Run();   // 6. Start listening

public partial class Program;
```

---

## 5. How a Request Triggers an API

Here is the step-by-step journey of an HTTP request through the system.

### Example: `POST /api/v1/transfers` with a JWT Bearer token

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. HTTP REQUEST arrives at Kestrel (ASP.NET Core web server)   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. MIDDLEWARE PIPELINE (runs in order from Program.cs)         │
│    UseExceptionHandler → UseSecurityHeaders → UseCors →         │
│    UseRateLimiter → UseAuthentication → UseAuthorization       │
│                                                                 │
│    • Authentication reads the JWT Bearer token, validates it,  │
│      and creates a ClaimsPrincipal (the "user")                │
│    • Authorization checks: does this user have the required    │
│      role/policy for this endpoint? (e.g. [Authorize])         │
│    • If unauthorized → 401/403 returned immediately            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. ENDPOINT ROUTING matches POST /api/v1/transfers →           │
│    PaymentEndpoints.ProcessTransferAsync                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. ENDPOINT FILTERS run (e.g. IdempotencyEndpointFilter)       │
│    • Checks Idempotency-Key header                              │
│    • If this key was already processed → returns cached result  │
│    • Otherwise → continues to handler                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. ENDPOINT HANDLER runs (ProcessTransferAsync)                │
│    • Reads [FromBody] TransferRequest                           │
│    • Resolves customerId from ClaimsPrincipal via               │
│      ApplicationDbContextLocator                                │
│    • Checks ownership: CanAccessAccountAsync(user, sourceAcc)   │
│    • Calls IPaymentService.ProcessTransferAsync(...)            │
│      (crosses into Bank.Application → Bank.Infrastructure)      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. APPLICATION SERVICE (Bank.Infrastructure.Services.PaymentService)│
│    • Validates transfer rules (ITransferPolicy)                 │
│    • Opens a single EF Core transaction (ACID)                  │
│    • Posts double-entry ledger lines (debit source, credit dest)│
│    • Updates account balances                                   │
│    • Saves to BankDbContext (PostgreSQL)                        │
│    • Commits transaction                                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. RESPONSE flows back up:                                     │
│    Created(201) with Location: /api/v1/transactions/{id}       │
│    → serialized as JSON → sent to client                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. All API Endpoints

### Auth (`/api/v1/auth`) — `AuthEndpoints.cs` (no auth required)

| Method | Path | What it does |
|--------|------|--------------|
| POST | `/register` | Register a new customer (creates Customer + ApplicationUser) |
| POST | `/verify-email` | Verify email address |
| POST | `/login` | Login → returns access token + httpOnly refresh token cookie |
| POST | `/refresh` | Rotate refresh token, issue new access token |
| POST | `/logout` | Revoke refresh token |

### Accounts (`/api/v1/accounts`) — `AccountEndpoints.cs` (auth required)

| Method | Path | What it does |
|--------|------|--------------|
| POST | `/` | Open a new savings/current account |
| GET | `/` | List accounts (admin sees all; customer sees their own) |
| GET | `/{id}` | Get account details (with ownership check) |
| GET | `/{id}/balance` | Get account balance |
| GET | `/{id}/transactions` | Get ledger entries with keyset cursor pagination |
| GET | `/{id}/statement` | Export statement as CSV |
| POST | `/{id}/close` | Close account (zero balance required) |

### Payments (`/api/v1`) — `PaymentEndpoints.cs` (auth required + idempotency)

| Method | Path | What it does |
|--------|------|--------------|
| POST | `/deposits` | Deposit money (idempotent, single DB transaction) |
| POST | `/withdrawals` | Withdraw money (idempotent, ownership check) |
| POST | `/transfers` | Transfer between accounts (idempotent, deadlock-free row locking) |
| GET | `/transactions/{id}` | Get transaction + immutable double-entry ledger lines |

### Customer Profile (`/api/v1/me`) — `CustomerEndpoints.cs` (auth required)

| Method | Path | What it does |
|--------|------|--------------|
| GET | `/` | Get my profile |
| PATCH | `/` | Update my profile |

### Admin (`/api/v1/admin`) — `AdminEndpoints.cs` (admin/auditor policies)

| Method | Path | What it does |
|--------|------|--------------|
| GET | `/customers` | Search/filter customers |
| GET | `/customers/{id}` | Get customer by ID |
| POST | `/customers/{id}/suspend` | Suspend customer |
| POST | `/customers/{id}/reactivate` | Reactivate customer |
| POST | `/accounts/{id}/freeze` | Freeze account |
| POST | `/accounts/{id}/unfreeze` | Unfreeze account |
| POST | `/transactions/{id}/reverse` | Reverse a transaction |

### Audit (`/api/v1/admin/audit-logs`) — `AuditEndpoints.cs` (admin/auditor only)

| Method | Path | What it does |
|--------|------|--------------|
| GET | `/` | Search immutable audit trail records |

### Health (no auth)

| Method | Path |
|--------|------|
| GET | `/` |
| GET | `/health` |
| GET | `/health/live` |
| GET | `/health/ready` |

---

## 7. Layers in Detail

### Domain Layer (`Bank.Domain`) — the core, no dependencies

Contains pure business rules. Knows nothing about HTTP, EF Core, or databases.

- **Entities:** `Account`, `Customer`, `LedgerTransaction`, `LedgerEntry`, `AuditLog`, `RefreshToken`, `IdempotencyRecord`, `OutboxMessage`, `ReconciliationRun`, `ReconciliationFinding`
- **Value Objects:** `Money`, `AccountNumber`, `TransactionReference`, `IdempotencyKey`
- **Enums:** `AccountType`, `CustomerStatus`, `KycStatus`, `AuditOutcome`, `TransactionType`, `EntryType`
- **Domain Services (interfaces):** `IPostingService`, `ITransferPolicy`, `IAccountNumberGenerator`
- **Exceptions:** `DomainExceptions`

### Application Layer (`Bank.Application`) — use case contracts

Defines **interfaces + DTOs** that the API calls. Contains no implementation logic.

- `IAccountService` — `OpenAccountAsync`, `GetAccountByIdAsync`, `GetAccountBalanceAsync`, `CloseAccountAsync`, etc.
- `IPaymentService` — `ProcessDepositAsync`, `ProcessWithdrawalAsync`, `ProcessTransferAsync`, `GetTransactionByIdAsync`
- `ICustomerService`, `IReconciliationService`, `IAuditService`, `IIdempotencyService`
- DTOs: `OpenAccountDto`, `DepositRequest`, `WithdrawalRequest`, `TransferRequest`, `AccountDto`, etc.

### Infrastructure Layer (`Bank.Infrastructure`) — implementations

Implements the application interfaces using EF Core + PostgreSQL.

- `BankDbContext` — EF Core DbContext with all entity configurations (snake_case naming convention)
- `PaymentService` — posts double-entry ledger entries atomically
- `AccountService`, `CustomerService`, `ReconciliationService`, `IdempotencyService`
- **Auth:** `ApplicationUser` (Identity), `JwtTokenService`, `RefreshTokenService`, `RefreshTokenRepository`
- `DatabaseMigrator` — runs migrations + seeds demo data
- `DependencyInjection.cs` — central service registration

---

## 8. Security & Cross-Cutting Concerns

| Concern | Where | How |
|---------|-------|-----|
| **Authentication** | `Program.cs` + `Infrastructure/Auth` | JWT Bearer tokens; refresh tokens in httpOnly cookies |
| **Authorization** | `Security/AuthorizationPolicies.cs` | Role-based policies: `Customer`, `Admin`, `Auditor`, `AdminOrAuditor` |
| **Resource ownership** | `Security/ApplicationDbContextLocator.cs` + `AccountOwnershipAuthorizationHandler.cs` | Customers can only access their own accounts; admins can access all |
| **Idempotency** | `Filters/IdempotencyEndpointFilter.cs` | `Idempotency-Key` header → safe retries, no duplicate transactions |
| **Rate limiting** | `Security/RateLimitingExtensions.cs` | Per-endpoint/IP rate limits |
| **Security headers** | `Middleware/SecurityHeadersMiddleware.cs` | X-Frame-Options, CSP, HSTS, etc. |
| **Error handling** | `Middleware/ProblemDetailsExceptionHandler.cs` | RFC 9457 Problem Details for all errors |
| **Audit logging** | `IAuditService` | Every auth action + state change logged immutably |

---

## 9. How to Run

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose (for PostgreSQL)

### 1. Start the database

```bash
docker-compose up -d
```

This starts PostgreSQL and any other infrastructure defined in the compose files.

### 2. Run migrations + seed demo data

```bash
cd backend/src/Bank.Migrator
dotnet run
```

This applies EF Core migrations and seeds demo chart of accounts, users, and accounts.

### 3. Start the API

```bash
cd backend/src/Bank.Api
dotnet run
```

The API listens on the configured port (default HTTPS `https://localhost:5001`).

### 4. Explore the API

Open the Swagger UI:

```
https://localhost:5001/swagger
```

All endpoints are documented there with request/response schemas.

---

## 10. Request Lifecycle Summary

```
Browser / Angular / curl
        │
        ▼
[Kestrel] → [Middleware: ExceptionHandler → SecurityHeaders → CORS → RateLimiter → Authentication → Authorization]
        │
        ▼
[Routing] matches URL → [Endpoint Handler] in Bank.Api/Endpoints/*.cs
        │
        ▼
[Filter: Idempotency] → [Service Interface] in Bank.Application
        │
        ▼
[Service Implementation] in Bank.Infrastructure/Services/*.cs
        │
        ▼
[EF Core + PostgreSQL] — double-entry ledger, ACID transaction
        │
        ◀────────────────────────────────────────────────────────────
        JSON response (201 Created / 200 OK / 400 / 401 / 403 / 404)
```

---

## Related Documentation

- [`PLAN.md`](../PLAN.md) — Full project plan, requirements, and design decisions
- [`docker-compose.yml`](../docker-compose.yml) — Infrastructure service definitions
- [`web/`](../web) — Angular frontend client

---

*Backend architecture: Clean Architecture · Modular Monolith · .NET 10 · ASP.NET Core Minimal APIs · EF Core · PostgreSQL*