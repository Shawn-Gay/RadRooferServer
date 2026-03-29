# Tech Stack

---

## Overview

| Layer | Choice | Notes |
|---|---|---|
| AI Receptionist | Vapi | Per-location assistant + phone number |
| API Server | ASP.NET Core (C#) | Handles Vapi webhooks, mobile app API, CRM/calendar integrations |
| ORM | Entity Framework Core | Global query filters for automatic tenant scoping |
| Multi-tenancy | Finbuckle.MultiTenant | JWT claim (`tenant_id`) for mobile app; locationId lookup for Vapi webhooks |
| Database | PostgreSQL | EF Core support, RLS available as extra safety net |
| Background Jobs | Hangfire | Dispatch queue, recurring jobs, per-tenant rate-limited sync |
| Mobile App | React Native + Expo (TypeScript) | iOS + Android, push notifications, OTA updates |
| CRM Integrations | C# adapter interfaces | Phase 1: GoHighLevel. Phase 2: JobNimbus, AccuLynx. Phase 3: HubSpot |
| Calendar Integrations | C# adapter interfaces | Phase 1: Google Calendar. Phase 2: Outlook. Phase 3: Calendly |
| Notifications | Twilio (SMS) + SendGrid (email) | Simple SDKs, industry standard |
| Server Hosting | Railway | Best DX for SaaS, PostgreSQL addon built in |
| App Distribution | EAS (Expo Application Services) | Builds, OTA updates, App Store + Play Store |

---

## API Server — ASP.NET Core (C#)

Single server handles everything:
- Vapi webhook endpoints (with `X-Vapi-Secret` verification per location)
- Mobile app REST API (JWT-authenticated)
- CRM / calendar OAuth callback endpoints
- Tenant and location management
- Background job processing (Hangfire)

No need for a separate server — one deployed service covers all of it.

---

## Solution Structure

```
RadRoofer.Api            → Controllers, middleware, auth filters, Hangfire setup
RadRoofer.Core           → Domain models, interfaces (ICrmAdapter, ICalendarAdapter), business logic
RadRoofer.Infrastructure → EF Core DbContext, adapter implementations, external service clients
RadRoofer.Tests          → Unit and integration tests
```

`Core` has zero infrastructure dependencies. Everything external lives in `Infrastructure` and is injected via interfaces defined in `Core`.

---

## Database — PostgreSQL

- Hosted on Railway alongside the API server
- EF Core handles all queries via adapters and global query filters
- Row Level Security (RLS) available as an optional extra safety net
- Hangfire uses a separate schema in the same database
- `TenantCrmConfig` and `TenantCalendarConfig` store encrypted credentials and cached pipeline/stage ID mappings

---

## Mobile App — React Native + Expo (TypeScript)

Roofers are field workers — they need a native feel, home screen icon, and real-time push notifications for new leads and calls. PWAs are still second-class on iOS for this use case.

**Phase 1 screens:** Login, Dashboard (analytics), Assistant Toggle, Location Switcher

**Expo toolchain:**
- **Expo Go** — local development
- **EAS Build** — compiles iOS + Android binaries in the cloud, no Mac required for Android
- **EAS Update** — pushes JS/UI changes over the air, no app store resubmission needed for most updates

---

## Hosting — Railway

- Deploy via Docker or GitHub push
- PostgreSQL addon on the same platform — easy connection strings, no separate DB hosting to manage
- Scales vertically and horizontally when needed
- Cheap to start (~$5–20/mo), easy to migrate to Azure if enterprise clients require it

---

## Why Not Azure (Yet)

Azure is a natural fit for ASP.NET Core and makes sense at scale or for enterprise clients. Railway is simpler and cheaper to start with, and migration is straightforward when the time comes.
