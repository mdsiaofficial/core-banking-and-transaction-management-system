# Security Architecture & Threat Model

- **Authentication**: ASP.NET Core Identity with JWT bearer tokens.
- **Refresh Tokens**: Cryptographically secure rotating tokens with automatic reuse detection and revocation family invalidation.
- **Authorization**: RBAC (Customer vs Administrator) and resource ownership validation (customers can only access their own accounts and statements).
- **Rate Limiting**: Partitioned IP/User sliding window rate limits on sensitive endpoints (`/auth/login`, `/transfers`).
- **Audit Logging**: Append-only tamper-resistant audit logs for all security and financial events.
