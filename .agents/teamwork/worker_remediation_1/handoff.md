# Handoff Report — Remediation Engineer (Iteration 2)

## 1. Observation

1. **Gate Review Findings**:
   - In `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`, `ProcessWithdrawalAsync` and `ProcessTransferAsync` accepted arbitrary account IDs without validating ownership against the authenticated caller, creating Broken Object Level Authorization (BOLA/IDOR) vulnerabilities.
   - `GetTransactionByIdAsync` returned transaction and double-entry ledger records to any authenticated user without verifying role or transaction participation.
   - In `backend/src/Bank.Infrastructure/Services/PaymentService.cs` (`ProcessReversalAsync`), there was no explicit guard checking if `originalTxn.Type == TransactionType.Reversal`.
   - `backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs` lacked integration tests specifically verifying that cross-account withdrawals, cross-account transfers, and foreign transaction viewing are denied with HTTP 403 Forbidden.

2. **Source Code Modifications**:
   - `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`:
     - Injected `IAccountService accountService` into `ProcessWithdrawalAsync` and `ProcessTransferAsync`.
     - In `ProcessWithdrawalAsync`:
       - Fetched target account via `accountService.GetAccountByIdAsync(request.AccountId, ct)`. If not found, returns `Results.NotFound(...)`.
       - Verified authorization via `await locator.CanAccessAccountAsync(user, account, ct)`. If unauthorized, returns `Results.Forbid()`.
     - In `ProcessTransferAsync`:
       - Fetched source debit account via `accountService.GetAccountByIdAsync(request.SourceAccountId, ct)`. If not found, returns `Results.NotFound(...)`.
       - Verified authorization via `await locator.CanAccessAccountAsync(user, sourceAcc, ct)`. If unauthorized, returns `Results.Forbid()`.
     - In `GetTransactionByIdAsync`:
       - Allowed access if `user.IsInRole(ApplicationRole.Admin) || user.IsInRole(ApplicationRole.Auditor)`.
       - Otherwise, resolved customer identity via `user.GetCustomerId() ?? await locator.ResolveCustomerIdAsync(user, ct)`. If null, returns `Results.Forbid()`.
       - Checked that caller's customer ID matches the owning customer of at least one distinct account associated with the transaction's entries. If no match, returns `Results.Forbid()`.
   - `backend/src/Bank.Infrastructure/Services/PaymentService.cs`:
     - Added guard in `ProcessReversalAsync`:
       ```csharp
       if (originalTxn.Type == TransactionType.Reversal)
           throw new DomainException("Cannot reverse a reversal transaction.", "CANNOT_REVERSE_REVERSAL");
       ```

3. **Integration Test Enhancements**:
   - `backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs`:
     - Added `Customer_CannotWithdrawFromForeignAccount_IDOR_ShouldReturnForbidden_403`: Verifies that customer Alice attempting to withdraw funds from Bob's account receives HTTP 403 Forbidden.
     - Added `Customer_CannotTransferFromForeignAccount_IDOR_ShouldReturnForbidden_403`: Verifies that customer Alice attempting to transfer funds out of Bob's account into her account receives HTTP 403 Forbidden.
     - Added `Customer_CannotViewForeignTransactionDetails_IDOR_ShouldReturnForbidden_403`: Verifies that customer Bob can view his private deposit transaction, Admin can view it, but unrelated customer Alice receives HTTP 403 Forbidden.
   - `backend/tests/Bank.Api.IntegrationTests/AdminReversalIntegrationTests.cs`:
     - Added assertion step 7 verifying that attempting to reverse a reversal transaction fails with HTTP 400 Bad Request and error code `CANNOT_REVERSE_REVERSAL`.

4. **Build & Test Execution Results**:
   - `dotnet build backend/Bank.sln`:
     ```text
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     Time Elapsed 00:00:10.48
     ```
   - `dotnet test backend/Bank.sln`:
     ```text
     Passed!  - Failed: 0, Passed: 70, Skipped: 0, Total: 70 - Bank.Domain.Tests.dll (net10.0)
     Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12 - Bank.Application.Tests.dll (net10.0)
     Passed!  - Failed: 0, Passed:  3, Skipped: 0, Total:  3 - Bank.Architecture.Tests.dll (net10.0)
     Passed!  - Failed: 0, Passed:  2, Skipped: 0, Total:  2 - Bank.ConcurrencyTests.dll (net10.0)
     Passed!  - Failed: 0, Passed: 39, Skipped: 0, Total: 39 - Bank.Api.IntegrationTests.dll (net10.0)
     Total: 126 passed, 0 failed, 0 skipped.
     ```
   - `npm --prefix web test -- --watch=false`:
     ```text
     Test Files  8 passed (8)
          Tests  33 passed (33)
     ```

---

## 2. Logic Chain

1. From Observation 1, the code quality review correctly identified that money debits via `/withdrawals` and `/transfers` lacked resource-level ownership validation in `PaymentEndpoints.cs`, and `GetTransactionByIdAsync` was open to all authenticated users.
2. From Observation 2, `locator.CanAccessAccountAsync(user, account, ct)` was integrated into `ProcessWithdrawalAsync` and `ProcessTransferAsync`. Because this method checks whether the user is Admin/Auditor or the account's `CustomerId` matches the caller's identity, any unauthorized customer debit attempt is intercepted before reaching the ledger posting engine and rejected with HTTP 403 Forbidden.
3. For transaction viewing, transactions touch multiple accounts (debit and credit entries). Verifying that a non-admin caller's `CustomerId` matches at least one participant account guarantees that customers can only view transactions they participated in.
4. Adding `if (originalTxn.Type == TransactionType.Reversal)` in `ProcessReversalAsync` guarantees that reversals cannot be reversed recursively, maintaining ledger auditability.
5. From Observation 3 and 4, three new integration tests were added in `AuthAndIdorIntegrationTests.cs` and executed against real PostgreSQL containers. All 3 tests plus the reversal-of-reversal check passed with 100% success rate, bringing total test count to 126 backend tests and 33 frontend tests with 0 warnings and 0 errors.

---

## 3. Caveats

- No caveats. The remediation directly addresses all findings from `reviewer_1` and `GATE_STATUS.md` without regressing any existing functionality or architectural boundaries.

---

## 4. Conclusion

- **Verdict**: **REMEDIATION_COMPLETE**
- All 3 reviewer findings have been resolved with genuine, production-grade security controls:
  1. BOLA/IDOR on `/withdrawals` and `/transfers` is prevented with `403 Forbidden`.
  2. BOLA on `/transactions/{id}` is prevented with `403 Forbidden` for non-participants.
  3. Reversal of reversals is rejected with `CANNOT_REVERSE_REVERSAL`.
  4. Comprehensive integration tests verify all scenarios against PostgreSQL.
- Build status is clean (0 warnings, 0 errors) and all 126 automated backend tests pass.

---

## 5. Verification Method

1. **Build Verification**:
   ```bash
   dotnet build backend/Bank.sln
   ```
   Confirm output: `0 Warning(s)`, `0 Error(s)`.

2. **Run Full Test Suite (126 Tests)**:
   ```bash
   dotnet test backend/Bank.sln
   ```
   Confirm all test projects pass, specifically verifying 39 tests passing in `Bank.Api.IntegrationTests`.

3. **Run Web Client Unit Tests (33 Tests)**:
   ```bash
   npm --prefix web test -- --watch=false
   ```
   Confirm 33 passed tests.

4. **Inspect Source Files**:
   - `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`
   - `backend/src/Bank.Infrastructure/Services/PaymentService.cs`
   - `backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs`
