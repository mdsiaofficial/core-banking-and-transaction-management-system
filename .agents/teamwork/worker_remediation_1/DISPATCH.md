# Dispatch Instructions for Remediation Worker

## Role & Mission
You are the **Remediation Worker** (`teamwork_worker`) for Iteration 2.
Your mission is to resolve the Broken Object Level Authorization (BOLA/IDOR) defect and access control findings identified by Code Quality Reviewer in `GATE_STATUS.md` and `reviewer_1/handoff.md`.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work.

## Tasks to Implement
1. **Fix BOLA on Withdrawals and Transfers in `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`**:
   - In `ProcessWithdrawalAsync`:
     - Fetch the account: `var account = await accountService.GetAccountByIdAsync(request.AccountId, ct);`
     - If `account is null`, return `Results.NotFound(ProblemDetailsFactory.CreateNotFound(...))`.
     - Check authorization: `if (!await locator.CanAccessAccountAsync(user, account, ct)) return Results.Forbid();`
   - In `ProcessTransferAsync`:
     - Fetch source account: `var sourceAcc = await accountService.GetAccountByIdAsync(request.SourceAccountId, ct);`
     - If `sourceAcc is null`, return `Results.NotFound(ProblemDetailsFactory.CreateNotFound(...))`.
     - Check authorization: `if (!await locator.CanAccessAccountAsync(user, sourceAcc, ct)) return Results.Forbid();`

2. **Fix Authorization on `GetTransactionByIdAsync` in `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`**:
   - Check if user is in role `Admin` or `Auditor`: if so, allow access.
   - If not, verify that user owns at least one account associated with the transaction:
     - For customer users, extract user's customerId via `user.GetCustomerId()`.
     - Ensure `transaction.Entries` contains an account whose `CustomerId == user.GetCustomerId()`.
     - If not, return `Results.Forbid()`.

3. **Guard Against Reversing a Reversal in `backend/src/Bank.Infrastructure/Services/PaymentService.cs`**:
   - In `ProcessReversalAsync`, check:
     `if (originalTxn.Type == TransactionType.Reversal) throw new DomainException("Cannot reverse a reversal transaction.", "CANNOT_REVERSE_REVERSAL");`

4. **Add Security Integration Tests in `backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs`**:
   - Add test `Customer_CannotWithdrawFromForeignAccount_IDOR_ShouldReturnForbidden_403`: Customer A tries to withdraw from Customer B's account -> asserts 403 Forbidden.
   - Add test `Customer_CannotTransferFromForeignAccount_IDOR_ShouldReturnForbidden_403`: Customer A tries to transfer out of Customer B's account -> asserts 403 Forbidden.
   - Add test `Customer_CannotViewForeignTransactionDetails_IDOR_ShouldReturnForbidden_403`: Customer A tries to call `GET /api/v1/transactions/{bobTxnId}` -> asserts 403 Forbidden.

5. **Build and Test**:
   - Run `dotnet build backend/Bank.sln` -> Ensure 0 warnings, 0 errors.
   - Run `dotnet test backend/Bank.sln` -> Ensure 100% tests pass (expect 126+ tests passing).

6. Document changes in `handoff.md` and message the Orchestrator.


## 2026-11-06T07:17:01Z
You are the Remediation Engineer on the Core Banking project.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/worker_remediation_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/orchestrator/GATE_STATUS.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/worker_remediation_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All fixes must be genuine and verified against real tests.

Tasks:
1. Fix BOLA/IDOR in backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs on ProcessWithdrawalAsync and ProcessTransferAsync by calling locator.CanAccessAccountAsync(user, account, ct).
2. Fix authorization in GetTransactionByIdAsync to prevent unauthorized customers from viewing foreign transactions.
3. Guard against reversing a reversal in backend/src/Bank.Infrastructure/Services/PaymentService.cs.
4. Add 3 integration tests in backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs proving that foreign withdrawal, foreign transfer, and foreign transaction viewing are rejected with 403 Forbidden.
5. Verify dotnet build backend/Bank.sln (0 warnings, 0 errors) and dotnet test backend/Bank.sln (all tests pass).
6. Deliver handoff.md and send a message to Orchestrator.
