# Dispatch Instructions for Reviewer

## Role & Mission
You are the **Code Quality Reviewer** (`teamwork_reviewer`).
Your mission is to perform an objective, rigorous review of the Core Banking & Transaction Management System deliverables per `PLAN.md` and `PROJECT.md`.

## Review Scope & Criteria
1. **Compilation & Build**:
   - Verify `dotnet build backend/Bank.sln` succeeds with 0 warnings, 0 errors.
   - Verify `npm run build` in `web/` succeeds with clean bundle generation.
2. **Automated Test Verification**:
   - Verify `dotnet test backend/Bank.sln` passes 100% of tests.
   - Verify `npm test -- --watch=false` in `web/` passes.
3. **Architecture & Contract Compliance**:
   - Review Clean Architecture / Modular Monolith layering per PLAN.md §8.
   - Verify REST API contracts, ProblemDetails RFC 9457 error formatting, cursor pagination, and CSV export.
   - Verify Angular 22 standalone signals-first architecture and Tailwind v4 styling.
4. **Deliverables Review**:
   - Report verdict: `APPROVE` or `REQUEST_CHANGES (with specific issues)`.
   - Write your review report to `.agents/teamwork/reviewer_1/handoff.md` and notify the Orchestrator.

## 2026-11-06T07:06:12Z
From: 11bda449-846f-4756-8ed7-1c02f0476b25
You are the Code Quality Reviewer on the Core Banking and Transaction Management System project.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/reviewer_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/reviewer_1/DISPATCH.md.

Perform a rigorous review of the solution:
1. Verify dotnet build backend/Bank.sln (0 warnings, 0 errors).
2. Verify dotnet test backend/Bank.sln (100% passing).
3. Verify web build (npm run build in web/).
4. Verify architectural conformance to PLAN.md and PROJECT.md.
5. Output your verdict (APPROVE or REQUEST_CHANGES) in handoff.md and send a message to the Orchestrator.
