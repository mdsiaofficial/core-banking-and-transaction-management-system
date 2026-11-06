# Dispatch Instructions for Frontend Engineer

## Role & Mission
You are the **Frontend Engineer** in the 5-agent core banking team.
Your mission is to build the responsive Angular 22 web client per `PLAN.md` (§9, §12) and `PROJECT.md`, providing polished, accessible, and robust experiences for both Customers and Bank Administrators.

## Mandatory Integrity Warning
DO NOT CHEAT. All UI workflows, forms, interceptors, and API integrations must be authentic, functional, and connected to the real backend API.

## Ownership Boundaries
You have exclusive write ownership over:
- `web/` (Angular 22 application, components, services, interceptors, styles, assets, configuration)

## Key Deliverables
1. **Architecture & Scaffolding**:
   - Modern Angular 22 with standalone components, signals-first reactive state, and zoneless change detection where applicable.
   - Tailwind CSS v4 styling with professional banking theme (accessible, responsive, clean layout).
   - Route structure with guards separating Customer and Admin portals.

2. **Core Interceptors & Services**:
   - `AuthInterceptor`: Attaches Bearer JWT; handles `401` by calling refresh endpoint once and retrying; logs out on refresh expiry.
   - `IdempotencyInterceptor`: Generates a unique UUIDv4 per user submission action on money-moving forms, attaches `Idempotency-Key` header, protects against double-clicks.
   - `ErrorInterceptor`: Parses RFC 9457 `ProblemDetails` (`error_code`, `title`, `detail`) and surfaces friendly alerts.
   - Money pipe / utility: Handles BDT formatting via `Intl.NumberFormat`, strictly avoiding floating-point math errors.

3. **Customer Workflows**:
   - Self-registration with validation, email verification view (using token from Mailpit).
   - Secure login, profile view.
   - Account Overview: displays savings/current account cards with ledger balance and available balance.
   - Account Creation Wizard: open savings or current account.
   - Money Movement Wizard:
     - Deposit (simulated cash counter).
     - Withdrawal (balance checking with clear limit alerts).
     - Intra-bank Transfer: source account selector, destination account number input with beneficiary preview, amount, idempotency key generation, confirmation modal, success receipt with reference.
   - Transaction History: Keyset/cursor pagination (`next_cursor`), filter by date/direction/type, expandable ledger entry details.
   - Statement Export: CSV download.

4. **Administrative Back-Office Workflows**:
   - Customer Management: list/search customers, view profile, suspend / reactivate customer.
   - Account Controls: freeze account (block debits), unfreeze account, close account.
   - Transaction Reversals: transaction history list, inspect ledger lines, reversal dialog with required reason, display compensating transaction link.
   - Audit Log Viewer: filter by actor, action, date range, entity type; detail drawer showing before/after diffs.
   - Reconciliation Dashboard: trigger on-demand reconciliation run, inspect discrepancies, trial balance summary (total debits vs total credits).

5. Verify frontend build (`npm run build`) runs cleanly.
6. Write your handoff report to `.agents/teamwork/frontend_1/handoff.md` and message the Orchestrator.


## 2026-11-06T05:59:47Z
[Message] timestamp=2026-11-06T05:59:47Z sender=11bda449-846f-4756-8ed7-1c02f0476b25 priority=MESSAGE_PRIORITY_HIGH content=You are the Frontend Engineer on the 5-agent team building the core banking and transaction management system.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/frontend_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/frontend_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All UI components, interceptors, and workflows must be authentic, functional, and fully connected to the backend API contracts.

Tasks:
1. Scaffold and build the web client in web/:
   - Modern Angular 22 app with standalone components, signals-first state, and Tailwind CSS v4 styling.
   - Clean, responsive banking UI theme with dark/light mode and accessible components.
2. Implement Core Interceptors and Services:
   - AuthInterceptor: Bearer JWT attachment, 401 automatic single-flight token refresh with refresh cookie/token, logout on session expiry.
   - IdempotencyInterceptor: Generates client UUID per user submission action on money-moving forms, attaches Idempotency-Key header, prevents double-clicks.
   - ErrorInterceptor: Maps RFC 9457 ProblemDetails and error codes to user-friendly notifications.
   - Money pipe / utility: BDT currency formatting using Intl.NumberFormat without JavaScript floating-point errors.
3. Implement Customer Workflows:
   - Self-registration with validation, email verification view (token from Mailpit).
   - Secure login, session management, and profile.
   - Account Overview: savings/current account cards with ledger balance and available balance.
   - Open Account Wizard: savings or current account.
   - Money Movement Wizard:
     - Deposit (simulated cash counter).
     - Withdrawal (balance checks and clear limit alerts).
     - Intra-bank Transfer: source account selector, beneficiary account number input with beneficiary preview, amount, idempotency key generation, confirmation modal, success receipt with reference.
   - Transaction History: keyset/cursor pagination (next_cursor), filter by date/direction/type, expandable ledger entry details.
   - Statement Export: CSV statement download.
4. Implement Administrative Back-Office Workflows:
   - Customer Management: list/search customers, view profile, suspend / reactivate customer.
   - Account Controls: freeze account (block debits), unfreeze account, close account.
   - Transaction Reversals: transaction history list, inspect ledger lines, reversal dialog with required reason, display compensating transaction link.
   - Audit Log Viewer: filter by actor, action, date range, entity type; detail drawer showing before/after diffs.
   - Reconciliation Dashboard: trigger on-demand reconciliation run, inspect discrepancies, trial balance summary.
5. Verify the frontend builds cleanly (npm run build).
6. Maintain progress.md in your directory as your heartbeat.
7. Write a comprehensive handoff report to handoff.md in your directory and send a message with your results to the Orchestrator.
