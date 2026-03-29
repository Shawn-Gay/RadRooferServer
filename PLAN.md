# Roofers Tech — AI Receptionist Platform

A multi-tenant SaaS platform that provides a Vapi-powered AI receptionist to roofing companies. Each roofer is a tenant. Each tenant can have multiple locations. Each location has its own AI assistant, phone number, and calendar.

---

## Docs

| File | Contents |
|---|---|
| [PHASE0.md](docs/PHASE0.md) | Phase 0 true MVP — core loop spec, build order, pseudo code |
| [SCHEMA.md](docs/SCHEMA.md) | Full database schema — all entities, relationships, indexes, EF pseudo code |
| [API.md](docs/API.md) | API surface — all endpoints, request/response shapes, handler pseudo code |
| [FEATURES.md](docs/FEATURES.md) | All features organized by phase |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Platform architecture, tenant model, adapter pattern, auth flows, webhook flow |
| [STACK.md](docs/STACK.md) | Tech stack, hosting, mobile app |
| [QUEUE.md](docs/QUEUE.md) | Communication dispatch queue design |
| [CRM.md](docs/CRM.md) | CRM adapter research, design flags, connection flows, phase rollout |

---

## Phases

| Phase | Name | Goal |
|---|---|---|
| **0** | Proof of Core Loop | Vapi call → webhook → Lead in DB → roofer sees it in Razor dashboard. No CRM, no SMS, no mobile app. |
| **1** | Integrations & Mobile | GHL CRM sync, Google Calendar booking, Twilio SMS, React Native app, Hangfire queue |
| **2** | Depth | JobNimbus + AccuLynx adapters, estimate requests, job status lookup, insurance intake, warm outbound, storm mode |
| **3** | Advanced | HubSpot, insurance adjuster comms, route optimization, warranty lookup, permits, predictive maintenance |

---

## Resolved Decisions

| Question | Decision |
|---|---|
| Phase 0 scope | Lead capture + call logging only. No CRM, SMS, calendar, or mobile app. |
| Phase 0 dashboard | Minimal Razor page (server-rendered) — login + stats + calls/leads table |
| Phase 0 schema | Full schema for all phases deployed on day 1 (schema-only tables cost nothing) |
| Phase 1 CRM | **GoHighLevel** — richest API, full OAuth, best calendar support |
| Phase 2 CRMs | JobNimbus, AccuLynx |
| Phase 3 CRM | HubSpot |
| Phase 1 Calendar | **Google Calendar** — most common, well-documented OAuth |
| Tenant resolution | **JWT claim** (`tenant_id`) via Finbuckle.MultiTenant |
| Auth for mobile app | Email + password → JWT |
| Auth for Vapi webhooks | `X-Vapi-Secret` header verification (per-location secret) |
| Auth for Razor pages | Cookie authentication (same claims as JWT) |
| User roles | Single enum string: Owner, Manager, Technician |
| Delete strategy | Soft deletes (IsDeleted + DeletedAt) on Lead, Customer, Job |
| PKs | GUIDs on all entities |
| Insurance adjuster communicator | Phase 3 — coordination depth TBD |
| Trip optimization | Phase 3 — requires CRM to expose geo/zip via API |
| Storm response mode | Phase 2 — manually triggered via dashboard toggle |

---

## Open Questions

- [ ] Billing model — per tenant, per location, per call, or flat rate?
- [ ] Trip optimization — confirm GoHighLevel exposes geo/zip data via API before committing

---

## Out of Scope

- Cold outbound calling (illegal for AI)
- Payments or invoicing
- Full CRM replacement
- Aerial / satellite image integration
