# Dispatch Instructions for Reviewer 2

## Role & Mission
You are **Reviewer 2** (`teamwork_reviewer`).
Your mission is to perform an independent re-review of the Core Banking & Transaction Management System following the Iteration 2 remediation of findings from Reviewer 1.

## Scope of Re-Review
1. **Verify Remediation of Previous Findings**:
   - Verify `backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs`:
     - Does `ProcessWithdrawalAsync` properly enforce ownership via `locator.CanAccessAccountAsync` and return 403 Forbidden on unauthorized access?
     - Does `ProcessTransferAsync` properly enforce ownership of the source account via `locator.CanAccessAccountAsync` and return 403 Forbidden?
     - Does `GetTransactionByIdAsync` properly restrict customer access to only transactions they participated in?
   - Verify `backend/src/Bank.Infrastructure/Services/PaymentService.cs`:
     - Does `ProcessReversalAsync` guard against reversing a reversal transaction (`TransactionType.Reversal`)?
   - Verify `backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs`:
     - Are the 3 new security tests present and asserting HTTP 403 Forbidden?
2. **Build & Test Verification**:
   - Verify `dotnet build backend/Bank.sln` (0 warnings, 0 errors).
   - Verify `dotnet test backend/Bank.sln` (100% pass rate, 126+ tests).
   - Verify `npm --prefix web test -- --watch=false` (100% pass rate, 33 tests).
3. **Verdict**:
   - Issue your verdict: `APPROVE` or `REQUEST_CHANGES`.
   - Write your report to `.agents/teamwork/reviewer_2/handoff.md` and message the Orchestrator.

## 2026-11-06T07:26:53Z
You are Reviewer 2 on the Core Banking project.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/reviewer_2/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/orchestrator/GATE_STATUS.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/reviewer_2/DISPATCH.md.

Re-review the codebase after Iteration 2 remediation:
1. Verify fixes for BOLA/IDOR on /withdrawals and /transfers, and access control on /transactions/{id} in backend/src/Bank.Api/Endpoints/PaymentEndpoints.cs.
2. Verify reversal of reversal guard in backend/src/Bank.Infrastructure/Services/PaymentService.cs.
3. Verify new security integration tests in backend/tests/Bank.Api.IntegrationTests/AuthAndIdorIntegrationTests.cs.
4. Verify dotnet build backend/Bank.sln (0 warnings, 0 errors).
5. Verify dotnet test backend/Bank.sln (all 126+ tests pass).
6. Issue your verdict (APPROVE or REQUEST_CHANGES) in handoff.md and send a message with your findings to the Orchestrator.
