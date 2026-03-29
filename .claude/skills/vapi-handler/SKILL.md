---
name: vapi-handler
description: Scaffold a new Vapi webhook event handler. Use when adding handling for a new Vapi event type (e.g. end-of-call-report, status-update).
argument-hint: "[event-type] [description of what to do]"
---

Read `.claude/tech/aspnetcore9.md` and `.claude/tech/efcore9.md` before writing any code.

Scaffold a Vapi webhook handler for: $ARGUMENTS

**Controller Setup**
- Route: `POST /v1/webhooks/vapi/{locationId}`
- Apply `[ServiceFilter<VapiSecretAuthFilter>]` — this handles auth before the action runs
- Apply `[AllowAnonymous]` — the secret filter IS the auth, there's no JWT on webhook routes
- Return `Ok()` immediately after queuing/processing — Vapi is fire-and-forget, don't make it wait

**Event Routing**
- Deserialize the payload to check `event` field (e.g. `"end-of-call-report"`)
- Use a `switch` expression on the event type to route to the correct handler logic
- Unknown event types: return `Ok()` silently — don't error on unrecognized events

**Tenant Resolution**
- Finbuckle resolves tenant from `locationId` route parameter via `LocationTenantStore`
- Never manually look up `TenantId` from `locationId` — it's already in the context

**Idempotency**
- Use the `VapiCallId` unique index to guard against duplicate webhook deliveries
- Catch `DbUpdateException` with `PostgresException.SqlState == "23505"` → return `Ok()`

**Data Persistence**
- Create a `CallLog` row for every `end-of-call-report` event
- Create a `Lead` row if the transcript contains lead information
- Never set `Id`, `CreatedAt`, or `UpdatedAt` — let EF/PostgreSQL handle them

**After writing**
- Confirm `VapiSecretAuthFilter` is applied
- Confirm `[AllowAnonymous]` is present
- Confirm idempotency guard is in place
- Confirm `Ok()` is returned before any slow operations
