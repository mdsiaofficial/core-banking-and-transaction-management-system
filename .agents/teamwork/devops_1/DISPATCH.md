# Dispatch Instructions for DevOps Specialist

## Role & Mission
You are the **DevOps Specialist** in the 5-agent core banking team.
Your mission is to establish the development, container, and CI/CD foundations for the entire core banking system per `PLAN.md` (§15, §16) and `PROJECT.md`.

## Ownership Boundaries
You have exclusive write ownership over:
- Root repository configurations: `.editorconfig`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.gitignore`
- Scaffolding the repository structure:
  - `backend/Bank.sln` and project directories (`Bank.Domain`, `Bank.Application`, `Bank.Infrastructure`, `Bank.Api`, `Bank.Migrator`, test projects)
  - `deploy/` directory: docker-compose files, nginx configurations, environment variables templates
  - `docker-compose.yml`, `docker-compose.override.yml`, `docker-compose.observability.yml`
  - Dockerfiles: `backend/Dockerfile` (multi-stage SDK -> publish -> aspnet runtime), `web/Dockerfile` (Node build -> nginx), `migrator`
  - Web client scaffolding if needed or nginx config for Angular SPA fallback and `/api` reverse proxy
  - `.github/workflows/ci.yml` (build, test, format, security scans)

## Key Deliverables
1. Check installed tools on the system (dotnet, node, npm, docker, etc.).
2. Setup `backend/` .NET 10 solution and projects per PLAN.md §8.2:
   - `Bank.Domain`, `Bank.Application`, `Bank.Infrastructure`, `Bank.Api`, `Bank.Migrator`
   - Test project skeletons in `backend/tests/`: `Bank.Domain.Tests`, `Bank.Application.Tests`, `Bank.Api.IntegrationTests`, `Bank.ConcurrencyTests`, `Bank.Architecture.Tests`
   - Setup project references and package configurations (`Npgsql.EntityFrameworkCore.PostgreSQL`, `FluentValidation`, `Serilog`, `OpenTelemetry`, `Scalar.AspNetCore`, etc.)
3. Setup `docker-compose.yml`:
   - `postgres`: PostgreSQL 18 with healthcheck
   - `migrator`: Runs database migrations & seeding
   - `api`: .NET 10 API with healthcheck
   - `web`: Angular web client served via Nginx with reverse proxy to `api`
   - `mailpit`: SMTP dev mail catcher with web UI on port 8025
4. Ensure `docker compose config` is valid and ready.
5. Create `docs/` structure per PLAN.md §16.
6. Verify everything builds cleanly with zero warnings/errors.
7. Write your handoff report to `.agents/teamwork/devops_1/handoff.md` and message the Orchestrator when done.

## 2026-11-06T05:59:12Z
From: 11bda449-846f-4756-8ed7-1c02f0476b25 (Orchestrator)
You are the DevOps Specialist on the 5-agent team building the core banking and transaction management system.
Your working directory is /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/devops_1/
Read /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/ORIGINAL_REQUEST.md, /home/ashiq/work/core-banking-and-transaction-management-system/PLAN.md, /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/PROJECT.md, and your specific instructions in /home/ashiq/work/core-banking-and-transaction-management-system/.agents/teamwork/devops_1/DISPATCH.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All configurations, Dockerfiles, and compose setups must be genuine and operational. A teamwork_auditor will independently inspect your deliverables.

Tasks:
1. Examine available tools in the environment (dotnet, node, docker, etc.).
2. Initialize and scaffold root configurations (.editorconfig, global.json, Directory.Build.props, Directory.Packages.props, .gitignore).
3. Scaffold backend solution at backend/Bank.sln with projects: Bank.Domain, Bank.Application, Bank.Infrastructure, Bank.Api, Bank.Migrator, and test projects in backend/tests/. Set up project references and common package dependencies.
4. Create deploy/ and root docker-compose.yml (Postgres 18, Migrator, API, Web, Mailpit) with healthchecks and proper dependencies.
5. Create multi-stage Dockerfiles for backend API, Migrator, and Web client (nginx).
6. Create .github/workflows/ci.yml covering build, test, and container checks.
7. Verify that dotnet build on the solution succeeds and docker compose config validates.
8. Maintain progress.md in your directory as your heartbeat.
9. Write a comprehensive handoff report to handoff.md in your directory and send a message with your results to the Orchestrator.
