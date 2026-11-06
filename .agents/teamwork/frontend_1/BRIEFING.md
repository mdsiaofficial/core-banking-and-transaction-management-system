# BRIEFING — 2026-11-06T06:35:30Z

## Mission
Build a production-grade, responsive Angular 22 web client for the core banking and transaction management system, covering customer and administrative back-office workflows with financial-transaction integrity.

## 🔒 My Identity
- Archetype: frontend_1
- Roles: implementer, qa, specialist
- Working directory: /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/frontend_1
- Original parent: 11bda449-846f-4756-8ed7-1c02f0476b25
- Milestone: M4 (Banking Web App)

## 🔒 Key Constraints
- Exclusive write ownership over `web/`.
- No mock or facade logic; must be fully connected to real backend API contracts (/api/v1).
- Money values handled without floating-point math; BDT formatted via Intl.NumberFormat.
- Idempotency key (UUIDv4) per user submission action on money-moving forms.
- RFC 9457 ProblemDetails handling with user-friendly notifications.
- Single-flight 401 refresh token flow.
- Standalone components, signals-first state, zoneless change detection where applicable, Tailwind CSS v4.

## Current Parent
- Conversation ID: 11bda449-846f-4756-8ed7-1c02f0476b25
- Updated: 2026-11-06T06:35:30Z

## Task Summary
- **What to build**: Angular 22 web client in `web/` supporting Customer and Admin workflows, interceptors, services, and tests.
- **Success criteria**: Clean compilation with `npm run build`, full adherence to backend contracts, robust error handling and UX.
- **Interface contracts**: `/home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md`
- **Code layout**: `web/src/app` with `core/`, `shared/`, `features/`

## Key Decisions Made
- Angular 22 standalone components with `@if/@for/@switch` control flows.
- Signals-first reactive state management throughout services and components.
- Tailwind CSS v4 styling with banking-grade color scheme and dark/light mode toggle.
- Safe financial arithmetic using scaled BigInt in `MoneyUtil` and BDT currency formatting via `Intl.NumberFormat`.
- Functional interceptors for JWT Bearer attachment, single-flight 401 token refresh, client UUID idempotency keys on money endpoints, and RFC 9457 ProblemDetails mapping.
- Role-guarded customer and admin portals with responsive sidebar and header shells.

## Artifact Index
- `.agents/teamwork/frontend_1/BRIEFING.md` — persistent memory
- `.agents/teamwork/frontend_1/progress.md` — heartbeat and task status
- `.agents/teamwork/frontend_1/handoff.md` — completion report

## Change Tracker
- **Files modified**: Angular 22 client in `web/` complete (services, models, interceptors, guards, pipes, components, routes, styles, and unit tests)
- **Build status**: PASS (Clean build in 5.7s, 33/33 unit tests pass)
- **Pending issues**: None

## Quality Status
- **Build/test result**: PASS (0 errors, 0 warnings, 33 tests passing)
- **Lint status**: Clean
- **Tests added/modified**: 8 test suites covering MoneyUtil, MoneyPipe, IdempotencyService, NotificationService, authGuard, idempotencyInterceptor, authInterceptor, and App component.

## Loaded Skills
- None specified by orchestrator
