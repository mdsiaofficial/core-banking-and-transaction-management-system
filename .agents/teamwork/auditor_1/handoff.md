# Forensic Audit Report & Handoff

**Work Product**: Core Banking and Transaction Management System (`backend/`, `web/`, `deploy/`)
**Profile**: General Project (Financial & Security Integrity)
**Verdict**: **CLEAN**

---

### Phase Results
- **Financial & Ledger Integrity**: **PASS** — Genuine double-entry ledger posting with `sum(debits) = sum(credits)` validated both in domain logic (`PostingService.cs:111-115`) and in PostgreSQL via deferred constraint trigger `trg_ledger_entries_balanced` (`002_triggers.sql:32-67`). Immutability triggers raise SQLSTATE `55000` on UPDATE/DELETE for `transactions`, `ledger_entries`, and `audit_logs`. Overdraft constraint (`ck_accounts_overdraft`) and unique index on `(account_id, account_seq)` prevent negative balance breaches and sequence race conditions.
- **Concurrency & Idempotency**: **PASS** — Deterministic row locking (`SELECT * FROM accounts WHERE id = ... FOR UPDATE` in ascending GUID order in `PaymentService.cs:525-541`) prevents deadlocks under high-contention transfers (verified empirically with 120 parallel transfers across 4 accounts: 0 deadlocks, 0 lost updates). Idempotency keys calculate genuine SHA-256 request payload hashes (`IdempotencyService.cs:101-105`), returning 422 Unprocessable Entity on payload alteration and replaying cached responses with `Idempotent-Replayed: true` on identical duplicate submissions.
- **Security & Authorization**: **PASS** — JWT issuance and single-use refresh token rotation implemented with RFC 6819 token family tracking (`RefreshTokenService.cs:94-117`). Replaying a rotated refresh token immediately triggers critical alert and revokes the entire token family. Resource authorization (`AccountOwnershipAuthorizationHandler.cs:26-37` and `ApplicationDbContextLocator.cs:42-43`) enforces strict customer ownership matching `account.CustomerId == user.CustomerId` to prevent IDOR/BOLA attacks. Structured audit trail logs all security and financial events to append-only `audit_logs`.
- **Frontend Authenticity**: **PASS** — Angular 22 standalone application with signals-first state management and Tailwind CSS v4. Forms directly interact with backend Minimal API endpoints via HTTP services (`account.service.ts`, `transaction.service.ts`, `admin.service.ts`) without dummy facades or mocked shortcuts. Interceptors automatically manage JWT bearer tokens, refresh rotation (`auth.interceptor.ts`), and UUIDv4 `Idempotency-Key` headers (`idempotency.interceptor.ts`).
- **Test Realism & Suite Integrity**: **PASS** — 156 automated tests executed empirically (123 backend xUnit/FsCheck/Testcontainers + 33 web vitest/Angular tests). All assertions verify real business rules, database invariants, and exception codes (no trivial `Assert.True(true)` or empty tests). Zero pre-populated log or result files detected in the workspace.

---

## 1. Observation

1. **Database Schema & Constraints**:
   - In `backend/src/Bank.Infrastructure/Data/Scripts/001_schema.sql:143`:
     ```sql
     CONSTRAINT uq_ledger_entries_account_seq UNIQUE (account_id, account_seq)
     ```
   - In `backend/src/Bank.Infrastructure/Data/Scripts/002_triggers.sql:4-29`:
     ```sql
     CREATE OR REPLACE FUNCTION prevent_immutable_modification()
     RETURNS TRIGGER AS $$
     BEGIN
         RAISE EXCEPTION 'UPDATE and DELETE operations are not permitted on table % (append-only ledger/audit).', TG_TABLE_NAME
         USING ERRCODE = '55000';
     END;
     $$ LANGUAGE plpgsql;
     ```
     Attached to `transactions`, `ledger_entries`, and `audit_logs` as `BEFORE UPDATE OR DELETE FOR EACH ROW`.
   - In `backend/src/Bank.Infrastructure/Data/Scripts/002_triggers.sql:62-67`:
     ```sql
     CREATE CONSTRAINT TRIGGER trg_ledger_entries_balanced
     AFTER INSERT ON ledger_entries
     DEFERRABLE INITIALLY DEFERRED
     FOR EACH ROW
     EXECUTE FUNCTION check_transaction_balanced();
     ```
   - In `backend/src/Bank.Infrastructure/Data/Scripts/002_triggers.sql:75-77`:
     ```sql
     ALTER TABLE accounts ADD CONSTRAINT ck_accounts_overdraft
     CHECK (kind <> 'Customer' OR balance + overdraft_limit >= 0);
     ```

2. **Posting Service & Account Ledger Arithmetic**:
   - In `backend/src/Bank.Domain/Services/PostingService.cs:30-34, 111-115`:
     Requires `instructions.Count >= 2`, asserts `debits == credits`, and verifies identical currency across all entries.
   - In `backend/src/Bank.Domain/Entities/Account.cs:128-164`:
     Enforces `expectedSeq == LastEntrySeq + 1` and applies directional arithmetic (`Balance += amount` or `Balance -= amount` based on `LedgerClass`). Throws `InsufficientFundsException` if `Kind == Customer` and `Balance + OverdraftLimit < 0`.

3. **Deterministic Row Locking & Deadlock Freedom**:
   - In `backend/src/Bank.Infrastructure/Services/PaymentService.cs:268-270, 525-541`:
     ```csharp
     var orderedIds = new[] { request.SourceAccountId, request.DestinationAccountId }.OrderBy(id => id).ToList();
     var lockedAccounts = await LockAccountsInOrderAsync(orderedIds, ct);
     ```
     Lock acquisition:
     ```csharp
     var acc = await _db.Accounts
         .FromSqlInterpolated($"SELECT * FROM accounts WHERE id = {id} FOR UPDATE")
         .FirstOrDefaultAsync(ct);
     ```

4. **Idempotency Hash & Replay Enforcement**:
   - In `backend/src/Bank.Infrastructure/Services/IdempotencyService.cs:30, 39-62, 101-105`:
     Computes SHA-256 hash of `endpoint:requestPayload`. Returns `IdempotencyStatus.PayloadMismatch` (mapped to HTTP 422) if hash differs. Returns `IdempotencyStatus.Conflict` (mapped to HTTP 409) if still `IN_PROGRESS`. Returns `IdempotencyStatus.Replay` with cached JSON response if `COMPLETED`.

5. **Security, Token Family Reuse Detection & IDOR Protection**:
   - In `backend/src/Bank.Infrastructure/Auth/RefreshTokenService.cs:94-117`:
     ```csharp
     if (token.RevokedAt != null || token.ReplacedById != null)
     {
         _logger.LogCritical("Security Alert: Refresh token reuse detected for user {UserId}, family {FamilyId}. Revoking entire token family immediately.", token.UserId, token.FamilyId);
         await _repository.RevokeFamilyAsync(token.FamilyId, ipAddress, ct);
         return new RefreshTokenResult(Success: false, Error: "Refresh token reuse detected...", ReuseDetected: true);
     }
     ```
   - In `backend/src/Bank.Api/Security/AccountOwnershipAuthorizationHandler.cs:26-36`:
     Denies access (`context.Fail()`) unless user is Admin/Auditor or `user.GetCustomerId() == resource.CustomerId`.

6. **Empirical Test Suite Execution Results**:
   - `dotnet test backend/tests/Bank.Domain.Tests/`:
     `Passed! - Failed: 0, Passed: 70, Skipped: 0, Total: 70`
   - `dotnet test backend/tests/Bank.Application.Tests/`:
     `Passed! - Failed: 0, Passed: 12, Skipped: 0, Total: 12` (Includes 3 FsCheck property test suites running 100 randomized executions each for total money conservation, debits=credits, and full ledger replaying)
   - `dotnet test backend/tests/Bank.Architecture.Tests/`:
     `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3`
   - `dotnet test backend/tests/Bank.Api.IntegrationTests/`:
     `Passed! - Failed: 0, Passed: 36, Skipped: 0, Total: 36` (Includes raw PostgreSQL trigger verification for SQLSTATE 55000 and 23514)
   - `dotnet test backend/tests/Bank.ConcurrencyTests/`:
     `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1` (120 parallel transfers across 4 accounts: zero deadlocks, zero lost updates, conserved total balance)
   - `web/` Angular Vitest Test Suite (`ng test --no-watch`):
     `Test Files: 8 passed (8), Tests: 33 passed (33)`
   - Repository scan for pre-populated test artifacts: 0 files found.
   - Search for fake assertions (`Assert.True(true)`): 0 matches found.

---

## 2. Logic Chain

1. **Integrity Rule 1 (No hardcoded test results / no facades)**:
   Observations 1, 2, 3, 4, and 5 confirm that all business logic, validation rules, state transitions, and database queries are implemented with genuine code. All balances are computed dynamically from ledger rows; row locks are issued dynamically through raw SQL `FOR UPDATE`; SHA-256 hashes are computed from request bodies; and refresh token families are tracked via persistent entity relations.

2. **Integrity Rule 2 (No fabricated outputs)**:
   Observation 6 confirms that no static log files or mock result files were planted in the workspace. All 156 tests ran dynamically on live processes and containers during the audit session.

3. **Integrity Rule 3 (Financial & Concurrency Invariants)**:
   Observation 1 and 6 prove that PostgreSQL triggers and database constraints independently enforce balance equilibrium (`check_transaction_balanced()`) and immutability (`prevent_immutable_modification()`). Even if application code were bypassed, direct SQL modification triggers SQLSTATE 55000 and 23514. The concurrency harness empirically proved zero lost updates and zero deadlocks under heavy contention.

4. **Integrity Rule 4 (Security & BOLA/IDOR)**:
   Observations 5 and 6 confirm that authorization and token management adhere to defense-in-depth principles: single-use refresh token rotation with immediate whole-family invalidation on reuse, and strict resource ownership checks before servicing account queries.

---

## 3. Caveats

- **External Mail Service**: Mailpit is configured as a local development SMTP container for email verification tokens; in production, an enterprise SMTP relay or transactional email provider would be substituted.
- **Integrity Mode**: The project was evaluated under **Development Mode** per `ORIGINAL_REQUEST.md`, but it also fully complies with **Demo Mode** standards (no external mock delegation for core banking logic).

---

## 4. Conclusion

The work product demonstrates **rigorous architectural authenticity, financial consistency, concurrency safety, and security controls**. All mandatory forensic audit checks have passed without reservation.

**Verdict**: **CLEAN**

---

## 5. Verification Method

To independently re-verify the forensic audit findings:

1. **Verify Backend Build and Unit Tests**:
   ```bash
   dotnet build backend/Bank.sln
   dotnet test backend/tests/Bank.Domain.Tests/
   dotnet test backend/tests/Bank.Application.Tests/
   dotnet test backend/tests/Bank.Architecture.Tests/
   ```

2. **Verify Database Trigger Immutability & Constraint Triggers against PostgreSQL**:
   ```bash
   dotnet test backend/tests/Bank.Api.IntegrationTests/ --filter "FullyQualifiedName~DatabaseTriggerImmutabilityTests|FullyQualifiedName~BalancedJournalConstraintTriggerTests"
   ```

3. **Verify High-Contention Concurrency Harness (120 parallel transfers, zero deadlocks)**:
   ```bash
   dotnet test backend/tests/Bank.ConcurrencyTests/ --filter "FullyQualifiedName~HighContentionTransferTests"
   ```

4. **Verify Frontend Build and Unit Tests**:
   ```bash
   cd web && npm run build
   cd web && npm test -- --no-watch
   ```

5. **Verify Workspace Cleanliness & Absence of Mock/Cheating Artifacts**:
   ```bash
   find . -not -path '*/.*' -not -path '*/node_modules/*' -not -path '*/bin/*' -not -path '*/obj/*' \( -name '*.log' -o -name '*result*' -o -name '*output*' \)
   ```
