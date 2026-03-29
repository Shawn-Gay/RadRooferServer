---
description: Tenant isolation and security rules for all C# source files
paths:
  - "src/**/*.cs"
---

# Security Rules

## Tenant Isolation
- Every controller action touching tenant data must have `[Authorize]` and rely on Finbuckle global query filters
- Never call `IgnoreQueryFilters()` in normal application code — only in seed/migration/admin operations
- Never manually add `WHERE TenantId = ...` — EF global filters handle this; manual filters create false security

## API Design
- Never expose EF entity classes directly in API responses — always map to a DTO
- GUIDs only for IDs in API responses — never expose sequential int IDs
- Vapi webhook input is untrusted — validate `X-Vapi-Secret` before processing any payload data

## Credentials + Secrets
- Never hardcode connection strings, API keys, or secrets in source files
- Use `IConfiguration` / environment variables only
- `VapiSecret` is stored hashed or verified via `CryptographicOperations.FixedTimeEquals` — never plain string compare

## Auth
- JWT Bearer for all `/v1/*` routes — `[Authorize]` on every controller
- `[AllowAnonymous]` only on: `POST /v1/auth/login` and Vapi webhook (webhook uses `VapiSecretAuthFilter` instead)
- Cookie auth for Razor Pages — never mix JWT and cookie on the same route
