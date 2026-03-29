# CRM Adapter Research

Research findings and design flags for the `ICrmAdapter` pattern. Updated before first line of code was written.

---

## Phase Rollout

| Phase | CRM | Auth Type | Notes |
|---|---|---|---|
| 1 | **GoHighLevel** | OAuth 2.0 | Reference implementation; richest API; best calendar support |
| 2 | **JobNimbus** | API Key | Roofing-native; no calendar API; no OAuth |
| 2 | **AccuLynx** | API Key | Roofing-native; insurance fields first-class; 10 req/sec limit |
| 3 | **HubSpot** | OAuth 2.0 | General-purpose; Enterprise required for custom objects |

---

## Auth & Onboarding

Two connection flows exist. Both are handled by `ICrmAdapterFactory` — the adapter interface itself is auth-agnostic.

### OAuth Flow (GoHighLevel, HubSpot)
- Tenant clicks "Connect CRM" → redirected to OAuth consent screen
- API receives callback → exchanges code for access + refresh tokens
- Tokens stored encrypted in `TenantCrmConfig`
- Adapter auto-refreshes tokens on every use

**GoHighLevel-specific:** Refresh token **rotates on every exchange**. Unlike HubSpot (stable refresh token), GHL invalidates the old refresh token immediately when a new access token is issued. The GHL adapter must write the new refresh token to the database atomically on every refresh. Failure to do so disconnects the tenant.

**HubSpot-specific:** Access tokens expire every **6 hours** (not 6 months). Auto-refresh must be handled in the adapter's `HttpClient` wrapper on every 401 response.

### API Key Flow (JobNimbus, AccuLynx)
- No self-service OAuth consent screen is possible — these CRMs do not support OAuth
- Tenant must manually generate an API key in their CRM settings and paste it into the platform
- Key stored encrypted in `TenantCrmConfig`
- Onboarding UX must include step-by-step instructions with screenshots for each CRM

---

## Known Adapter Design Issues

### 1. AccuLynx: Lead → Job is a mandatory two-step operation
AccuLynx requires a contact to exist before a job can be created. You cannot create a job for a new caller in a single API call. The AccuLynx adapter handles this internally — `CreateJobAsync` creates the contact first if no `ContactId` is provided, then creates the job. The interface contract stays clean.

### 2. Appointment scheduling is incompatible across CRMs
Each CRM has a fundamentally different appointment model:

| CRM | Appointment Model | Limitation |
|---|---|---|
| GoHighLevel | Standalone; linked to contact; rich API | None for Phase 1 |
| Google Calendar | Free-floating slot booking | None for Phase 1 |
| AccuLynx | Job-scoped; requires `jobId` | Cannot book before job record exists |
| HubSpot | Requires pre-configured meeting link slug | Not a raw time slot |
| JobNimbus | Tasks/activities only | No real calendar API |

`ICrmAdapter` uses a `SupportsCalendarBooking` capability flag. When `false`, the dispatch queue sends an SMS/email to the customer with the roofer's contact info instead of booking a slot.

### 3. Pipeline / stage IDs are always dynamic
All four CRMs use user-configured workflow stages — there is no fixed status enum. Every adapter must resolve stage names to internal IDs at runtime. `TenantCrmConfig` caches a `StageIdMap` dictionary (`"Lead" → "abc123"`) populated at connection time and refreshed when the tenant reconnects or explicitly re-syncs.

### 4. AccuLynx rate limit is the binding constraint
AccuLynx enforces 10 req/sec per API key. For a multi-tenant platform syncing many tenants simultaneously, this requires per-tenant rate-limited job queues. See `QUEUE.md` for the rate-limiting strategy.

### 5. Roofing-specific fields vary by CRM
Insurance fields (carrier, claim number, adjuster name, deductible) are first-class API objects in AccuLynx but custom fields in GHL, HubSpot, and JobNimbus.

`CreateLeadRequest` and `CreateJobRequest` include nullable insurance fields:

```csharp
public class CreateJobRequest
{
    public string ContactId { get; set; }
    public string? InsuranceCompany { get; set; }
    public string? ClaimNumber { get; set; }
    public string? AdjusterName { get; set; }
    public decimal? DeductibleAmount { get; set; }
    // ...
}
```

AccuLynx maps these to native insurance endpoints. GHL, JobNimbus, and HubSpot adapters map them to custom field key-value pairs using the field keys configured in `TenantCrmConfig`.

---

## CRM Reference Sheets

### GoHighLevel

| Property | Value |
|---|---|
| Auth | OAuth 2.0 (V2 required; V1 deprecated) |
| Rate limit | 100 req / 10 sec per location |
| Webhooks | Push-based; 50+ event types including contact, opportunity, appointment |
| Lead creation | `POST /contacts/` — upserts on email or phone match |
| Job creation | `POST /opportunities/` — requires `pipelineId` + `pipelineStageId` + `contactId` |
| Calendar API | Full — `POST /calendars/appointments`; first-class citizen |
| Sandbox | No dedicated sandbox; use Private app mode against internal sub-account |
| Roofing fields | Custom fields per location; no native roofing model |
| Key gotchas | Rotating refresh token; pipeline stage IDs are dynamic; white-label OAuth friction |

### JobNimbus

| Property | Value |
|---|---|
| Auth | Static API key (Bearer token) |
| Rate limit | Undocumented; implement exponential backoff on 429 |
| Webhooks | Limited push events (contact, job create/update/delete); registration via UI only |
| Lead creation | `POST /contacts` — no strictly required fields; practical min: name |
| Job creation | `POST /jobs` — requires `related` (parent contactId) and `status_name` |
| Calendar API | Tasks/activities only — no slot booking or availability check |
| Sandbox | None |
| Roofing fields | Native concepts (Contact vs. Job); custom fields for insurance; integrates with Eagleview/HOVER |
| Key gotchas | No OAuth (manual key onboarding); no programmatic webhook registration; status_name is string-matched to user-configured stages |

### AccuLynx

| Property | Value |
|---|---|
| Auth | Static API key (Bearer token) |
| Rate limit | **10 req/sec per API key** (IP: 30 req/sec) |
| Webhooks | Push-based; job-centric events only (job.created, job.updated, job-milestone-changed) |
| Lead creation | `POST /api/v2/leads/create` — required: `firstName`, `lastName` |
| Job creation | `POST /api/v2/jobs/CreateJob` — requires `contactId` from existing lead (two-step) |
| Calendar API | Job-scoped only: `PUT /api/v2/jobs/{jobId}/initial-appointment` |
| Insurance API | Native: assign insurer, claim info at job level — first-class endpoints |
| Sandbox | None; AccuLynx explicitly provides no developer support environment |
| Roofing fields | Most roofing-native API: milestones, trade types, work type (retail vs. insurance), job finances |
| Key gotchas | Lead → Job two-step creation; appointment requires jobId; 10 req/sec is tight for bulk sync; no OAuth |

### HubSpot

| Property | Value |
|---|---|
| Auth | OAuth 2.0 (stable refresh token) or Private App token |
| Rate limit | 190 req / 10 sec (Professional+); 250,000–1,000,000 req/day |
| Webhooks | Push-based; HMAC-signed payloads (must validate signature); up to 1,000 subscriptions |
| Lead creation | `POST /crm/v3/objects/contacts` — no required fields; deduplicates on email |
| Job creation | `POST /crm/v3/objects/deals` — required: `dealname`; stages are dynamic pipeline IDs |
| Calendar API | Meetings scheduler (`/scheduler/v3/meetings/`) requires pre-configured meeting link slug; cannot reschedule via API |
| Sandbox | Developer test accounts (free, 90-day); Standard Sandbox requires Enterprise |
| Roofing fields | None native; custom properties for all roofing fields; custom objects require Enterprise |
| Key gotchas | No roofing domain model; meetings API cannot reschedule; 6-hour access token expiry; associations API is verbose |
