# MVP — Roofers Tech (Superseded)

> **This document has been superseded by [PHASE0.md](PHASE0.md).** Phase 0 is the true MVP — even leaner than what's described below. This file is preserved as context for how Phase 1 was originally scoped. What's described below now maps to **Phase 1** (after Phase 0 is validated).

The goal of the MVP is to prove the core loop works end-to-end with a real roofer before building a full platform. Every decision below optimizes for **speed to first real call handled**.

---

## The Core Loop

```
Inbound call → Vapi answers → collects lead info → webhook fires
→ API creates lead in GoHighLevel → sends SMS to caller → done
```

That's it. Everything else is Phase 2.

---

## What's In

### Vapi
- One Vapi assistant, one phone number (seeded/configured manually)
- Collects: caller name, phone, address, reason for call
- No calendar booking — just lead capture for MVP
- After-hours: same flow, no special handling needed yet
- System prompt kept simple and hardcoded

### API (ASP.NET Core)
- `POST /webhooks/vapi/{locationId}` — receives Vapi call-ended event
  - Verifies `X-Vapi-Secret` header
  - Parses transcript/tool-call payload
  - Creates lead in GoHighLevel (direct HTTP call, no adapter abstraction)
  - Sends SMS confirmation to caller via Twilio (inline, no queue)
  - Logs call to DB
- `POST /auth/login` — email + password → JWT
- `GET /dashboard` — returns simple stats for the web UI (calls today, leads captured)

### Database (PostgreSQL + EF Core)
Keep schema future-proof but implement only what MVP needs:

```
Tenants        (Id, Name, CreatedAt)
Locations      (Id, TenantId, Name, VapiAssistantId, VapiPhoneNumber, VapiSecret)
CallLogs       (Id, TenantId, LocationId, CallerPhone, CallerName, Summary, CreatedAt)
```

- `TenantId` and `LocationId` on every entity — non-negotiable, painful to add later
- No Finbuckle middleware yet — resolve tenant from JWT claim manually (one line)
- Seed one tenant + one location manually or via a simple seed endpoint

### Web Dashboard (not mobile)
Simple Razor page or a single-page React app — no React Native for MVP:
- Login screen
- Dashboard: calls today, leads captured this week
- No settings, no location switcher, no assistant toggle for MVP

### GoHighLevel Integration
- Direct HTTP calls to GHL API (no adapter interface yet)
- OAuth flow to connect a GHL account (required — no way around this)
- Create contact + opportunity on call end
- Tag with `inbound-call` source

### SMS (Twilio)
- Send inline after lead creation for MVP — no queue, no retry logic
- One message: "Hi [Name], [Company] received your call. Someone will be in touch shortly."
- Log failures, don't retry for MVP
- **Next step after MVP:** replace inline send with a `DispatchMessages` table (`Id`, `TenantId`, `To`, `Body`, `SendAt`, `SentAt`, `Error`) and a Railway cron job that runs every 5 minutes, processes any rows where `SendAt <= now` and `SentAt IS NULL`, and marks them sent. This enables scheduled sends (e.g. confirmation 2 minutes after call, follow-up next morning) with no additional dependencies — worst-case 5-minute delay is acceptable.

### Hosting
- Railway — one service (ASP.NET Core), one PostgreSQL addon
- Environment variables for secrets (Vapi key, GHL OAuth, Twilio, JWT secret, DB URL)
- No CI/CD yet — manual deploy to validate

---

## What's Out (explicitly deferred)

| Feature | Why deferred |
|---|---|
| Calendar booking | Adds GHL + Google OAuth complexity; validate lead capture first |
| React Native mobile app | Web dashboard is faster to build and iterate |
| Finbuckle.MultiTenant middleware | Manual tenant resolution is sufficient for one tenant |
| Adapter pattern (ICrmAdapter) | Only one CRM; extract interface after second CRM is needed |
| Hangfire / job queue | Not needed — Railway cron + DispatchMessages table is the upgrade path |
| Railway cron jobs | Nothing to schedule yet; becomes the dispatch processor post-MVP |
| After-hours special handling | AI handles the call; escalation SMS is Phase 2 |
| Multi-location management UI | Seed locations manually; UI is Phase 2 |
| Assistant on/off toggle | Configure directly in Vapi dashboard for now |
| Email confirmations (SendGrid) | SMS only for MVP |
| Analytics beyond basic counts | Phase 2 |
| CI/CD pipeline | Manual deploy is fine at this scale |

---

## Build Order

1. **Database** — create schema, run migrations, seed one tenant + location
2. **Vapi** — configure assistant + phone number in Vapi dashboard, set webhook URL + secret
3. **Webhook endpoint** — receive call event, verify secret, parse payload
4. **GoHighLevel** — OAuth flow + create contact/opportunity
5. **Twilio SMS** — send confirmation inline
6. **Auth + dashboard** — login → JWT → basic stats page
7. **Deploy to Railway** — wire up env vars, test end-to-end with a real call

Each step is independently testable before moving to the next.

---

## Upgrade Path (when MVP is validated)

These can be layered in without breaking the MVP foundation:

- **Finbuckle**: drop-in middleware; tenant JWT claim stays the same
- **Adapter pattern**: wrap the GoHighLevel HTTP calls in `ICrmAdapter`; same interface the second CRM needs
- **Hangfire or queue**: replace inline Twilio call with a dispatched job; DB schema already has `CallLog` to hang retry logic off
- **React Native**: the API shape doesn't change; mobile app consumes the same endpoints
- **Google Calendar**: add to the webhook handler after lead creation is solid
- **Multi-location UI**: locations table already exists; just add CRUD endpoints + UI

---

## Success Criteria

MVP is done when:
- A real call comes in on the Vapi number
- The AI collects name, phone, address, and reason
- A lead appears in GoHighLevel
- The caller gets an SMS confirmation
- The call is logged in the DB
- A human can see that call in the web dashboard

That's the whole thing. Ship that first.
