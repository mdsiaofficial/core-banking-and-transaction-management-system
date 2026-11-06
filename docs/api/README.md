# API Specification & Conventions

- Base path: `/api/v1`
- Content-Type: `application/json`
- Error format: RFC 9457 `application/problem+json`
- Idempotency Header: `Idempotency-Key: <UUID>` required for mutating financial endpoints (`/deposits`, `/withdrawals`, `/transfers`, `/reversals`).
- Documentation: Swagger UI available at `/swagger` and OpenAPI JSON at `/openapi/v1.json`.
