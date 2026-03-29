# Architecture

---

## Tenant Model

```
Platform
  └── Roofer (Tenant)
        └── Location
              ├── Vapi Phone Number + Assistant
              ├── Calendar (Google Calendar → Outlook → Calendly)
              ├── CRM Connection (GoHighLevel → JobNimbus → AccuLynx → HubSpot)
              └── Customers & Jobs
```

- **Multi-tenant** — each roofer's data is fully isolated via EF Core global query filters
- **Multi-location** — a single tenant can have many locations; each gets its own assistant and number
- **CRM agnostic** — integrations built as adapters, not hardcoded to one CRM
- **Calendar agnostic** — same adapter pattern for scheduling
- **Scalable call handling** — Vapi runs in the cloud; call volume spikes are handled automatically

---

## Auth Flows

Three distinct auth concerns — each handled differently.

### 1. Mobile App Users → JWT

Roofer logs in with email + password. The API issues a JWT containing a `tenant_id` claim. Finbuckle.MultiTenant reads that claim on every request and scopes all queries to that tenant automatically.

```
POST /v1/auth/login
  → validate credentials
  → issue JWT { sub, tenant_id, role, exp }
  → Finbuckle resolves tenant from tenant_id claim on all subsequent requests
```

### 2. Vapi Webhooks → Secret Verification

Vapi is not a user. It's a service sending HTTP calls to your API. Each Vapi assistant is configured with a per-location webhook secret. Your API verifies the `X-Vapi-Secret` header on every incoming webhook — requests without a valid secret are rejected with 401. The `locationId` in the URL then resolves the tenant.

```
POST /v1/webhooks/vapi/{locationId}
  → VapiWebhookAuthFilter validates X-Vapi-Secret header against Location.WebhookSecret
  → locationId resolves tenant + location — no JWT needed
  → call event handler runs in correct tenant context
```

### 3. CRM / Calendar OAuth → Token Storage

When a tenant connects their CRM or calendar, your API runs an OAuth consent flow (GoHighLevel, HubSpot, Google Calendar) or accepts a manually pasted API key (JobNimbus, AccuLynx). Credentials are stored encrypted in `TenantCrmConfig` / `TenantCalendarConfig` and retrieved at runtime by the adapter.

```
GET /v1/integrations/crm/gohighlevel/connect
  → redirect to GHL OAuth consent screen
  → callback: exchange code for access + refresh tokens
  → store encrypted in TenantCrmConfig
  → adapter retrieves + auto-refreshes tokens on every use
```

---

## Inbound Call Flow

```
Inbound Call
     │
     ▼
[ Vapi Assistant ]  (per location)
     │
     ▼
POST /v1/webhooks/vapi/{locationId}
     │
     ▼
[ VapiWebhookAuthFilter: verify X-Vapi-Secret ]
     │
     ▼
[ Middleware: resolve tenant from locationId ]
     │
     ▼
[ Call Event Handler ]
     │
     ├──► CRM Adapter       →  CreateLeadAsync() / UpdateJobAsync()
     ├──► Calendar Adapter  →  GetAvailableSlotsAsync() / BookAppointmentAsync()
     └──► Dispatch Queue    →  enqueue SMS / email / outbound call
```

The `locationId` in the webhook URL identifies both the tenant and the location — no ambiguity across tenants.

---

## Warm Outbound Flow

Only triggered for existing leads or customers who have already made contact.

```
[ Hangfire Scheduled Job ]
     │
     ▼
[ Dispatch Service ]
     │
     ▼
[ Vapi Outbound Call API ]
     │
     ▼
[ Customer (existing lead/customer only) ]
```

---

## Multi-Tenancy Design

Every entity carries a `TenantId`. EF Core global query filters automatically scope all queries — no chance of cross-tenant data leaks, and developers never manually filter.

```csharp
// Defined once per entity in DbContext
modelBuilder.Entity<Lead>()
    .HasQueryFilter(o => o.TenantId == _tenantProvider.CurrentTenantId);
```

Finbuckle.MultiTenant resolves the current tenant from the incoming request:
- **JWT claim** (`tenant_id`) — mobile app requests
- **`locationId` URL parameter** — Vapi webhook requests (resolved via `Location` lookup)

---

## CRM / Calendar Adapter Pattern

New integrations are added by implementing an interface — core platform logic never changes.

```csharp
public interface ICrmAdapter
{
    // Capability flags — adapters declare what they support
    bool SupportsCalendarBooking { get; }
    bool SupportsInsuranceFields { get; }

    Task<Lead> CreateLeadAsync(CreateLeadRequest request);
    Task<Job> CreateJobAsync(CreateJobRequest request);
    Task UpdateJobAsync(string jobId, UpdateJobRequest request);
    Task<JobStatus> GetJobStatusAsync(string jobId);

    // Returns null if SupportsCalendarBooking is false
    Task<Appointment?> ScheduleAppointmentAsync(ScheduleAppointmentRequest request);
}

public interface ICalendarAdapter
{
    Task<IEnumerable<TimeSlot>> GetAvailableSlotsAsync(string locationId, DateOnly date);
    Task<Appointment> BookAppointmentAsync(BookAppointmentRequest request);
    Task CancelAppointmentAsync(string appointmentId);
}
```

**Capability flags** exist because adapters differ in what they can support:
- AccuLynx appointments require an existing job record — `SupportsCalendarBooking` returns `false` until a job exists
- JobNimbus has no calendar API — `SupportsCalendarBooking` always returns `false`
- AccuLynx has native insurance endpoints — `SupportsInsuranceFields` returns `true`

When booking is not supported, the dispatch queue sends an SMS/email to the customer with contact details instead.

### Connection Flows

Adapters share an interface but have two distinct connection setup flows. Your `ICrmAdapterFactory` handles both:

| CRM | Connection Type | Notes |
|---|---|---|
| GoHighLevel | OAuth 2.0 | Rotating refresh token — must persist new token on every refresh |
| HubSpot | OAuth 2.0 | Stable refresh token — standard flow |
| JobNimbus | API Key | Tenant pastes key from their JN account settings |
| AccuLynx | API Key | Tenant pastes key from their AccuLynx account settings |

### Pipeline / Stage ID Caching

All four CRMs use user-configured workflow stages — there is no fixed status enum. `TenantCrmConfig` caches stage name → ID mappings per tenant, populated at connection time and refreshed when a tenant reconnects.

```csharp
// TenantCrmConfig entity
public class TenantCrmConfig
{
    public Guid TenantId { get; set; }
    public CrmType CrmType { get; set; }             // GoHighLevel, JobNimbus, AccuLynx, HubSpot
    public string? EncryptedApiKey { get; set; }     // API key CRMs
    public string? AccessToken { get; set; }         // OAuth CRMs
    public string? RefreshToken { get; set; }        // OAuth CRMs
    public DateTime? TokenExpiresAt { get; set; }
    public Dictionary<string, string> StageIdMap { get; set; }  // "Lead" → "abc123"
}
```

---

## Mobile App — Phase 1 Scope

The mobile app is a dashboard for roofers to manage the AI receptionist. It authenticates via JWT (same auth system as the API) and is scoped to the tenant from the token.

**Phase 1 screens:**
1. **Login** — email + password → JWT stored in secure storage
2. **Dashboard** — total calls today/week, leads captured, appointments booked (sourced from `CallLog`)
3. **Assistant Toggle** — enable/disable the Vapi assistant per location (calls Vapi API)
4. **Location Switcher** — switch between locations if tenant has multiple

All analytics in Phase 1 come from existing `CallLog` data — no separate analytics infrastructure needed.
