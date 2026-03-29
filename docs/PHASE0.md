# Phase 0 — True MVP

The simplest thing that proves the core bet: **an AI receptionist captures roofing leads without losing a single call.**

Phase 0 is NOT the full MVP described in the original plan. It is smaller, faster to ship, and deliberately defers CRM integration, SMS, calendar booking, and the mobile app. What it does ship is a foundation that every later phase builds on without restructuring.

---

## The Core Loop

```
Inbound call
    → Vapi answers (configured manually in Vapi dashboard)
    → AI collects: name, phone, address, reason for call
    → Call ends
    → Vapi fires POST /v1/webhooks/vapi/{locationId}
    → API verifies X-Vapi-Secret
    → API persists CallLog + Lead to PostgreSQL
    → Roofer logs into Razor dashboard and sees the lead
```

That's the whole loop. If this works end-to-end with a real roofer's phone number, the product has value.

---

## What Ships

| Component | Details |
|---|---|
| **Database** | Full schema — all 11 entities, all relationships, all indexes. See [SCHEMA.md](SCHEMA.md). Tables that Phase 0 doesn't write to still exist and are ready. |
| **EF Core** | Global query filters (TenantId + soft delete), SaveChanges timestamp override, all entity configurations. |
| **Multi-tenancy** | Finbuckle.MultiTenant with JWT claim strategy (Razor/API) and locationId lookup (webhooks). |
| **Auth** | `POST /v1/auth/login` → JWT. Cookie auth for Razor pages. Dual auth scheme in Program.cs. |
| **Vapi webhook** | `POST /v1/webhooks/vapi/{locationId}` — secret verification, payload parsing, CallLog + Lead creation. Idempotent via VapiCallId unique index. |
| **Read API** | `GET /v1/calls`, `GET /v1/calls/{id}`, `GET /v1/leads`, `GET /v1/leads/{id}`, `GET /v1/dashboard/stats`, `GET /v1/locations`. All tenant-scoped, paginated. |
| **Razor dashboard** | Login page + single dashboard page: stats at top, tabbed table (calls / leads). Server-rendered, no JS framework. |
| **Seed data** | 1 tenant, 1 location (with VapiSecret), 1 admin user. Applied via EF migration or startup seed. |

---

## What's Out (and why)

| Deferred | Phase | Reason |
|---|---|---|
| GoHighLevel CRM sync | 1 | OAuth flow adds complexity; validate lead capture first |
| Google Calendar booking | 1 | Requires calendar OAuth; leads are the priority |
| Twilio SMS confirmations | 1 | Nice-to-have; the roofer sees the lead in the dashboard already |
| SendGrid email | 1 | Same as SMS — deferred until dispatch queue exists |
| Dispatch queue (Hangfire) | 1 | No async jobs needed when there's no CRM/SMS to call |
| Adapter interfaces (ICrmAdapter) | 1 | Only one CRM; extract interface when second CRM arrives |
| React Native mobile app | 1 | Razor page validates the UX faster |
| User registration endpoint | 1 | Seed the first user; build registration in Phase 1 onboarding |
| Location CRUD endpoints | 1 | Seed one location; add management when multi-location matters |
| CI/CD pipeline | 1 | Manual deploy to Railway is fine for one service |

---

## Solution Structure

```
RadRoofer.sln
├── src/
│   ├── RadRoofer.Api/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── CallsController.cs
│   │   │   ├── LeadsController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── LocationsController.cs
│   │   │   └── VapiWebhookController.cs
│   │   ├── Filters/
│   │   │   └── VapiSecretAuthFilter.cs
│   │   ├── Pages/                      ← Razor Pages
│   │   │   ├── Login.cshtml
│   │   │   ├── Login.cshtml.cs
│   │   │   ├── Dashboard.cshtml
│   │   │   ├── Dashboard.cshtml.cs
│   │   │   └── Shared/
│   │   │       └── _Layout.cshtml
│   │   ├── DTOs/
│   │   │   ├── LoginRequest.cs
│   │   │   ├── LoginResponse.cs
│   │   │   ├── CallLogDto.cs
│   │   │   ├── LeadDto.cs
│   │   │   ├── DashboardStatsDto.cs
│   │   │   ├── LocationDto.cs
│   │   │   └── PagedResult.cs
│   │   └── Program.cs
│   │
│   ├── RadRoofer.Core/
│   │   ├── Entities/
│   │   │   ├── Tenant.cs
│   │   │   ├── Location.cs
│   │   │   ├── AppUser.cs
│   │   │   ├── CallLog.cs
│   │   │   ├── Lead.cs
│   │   │   ├── Customer.cs
│   │   │   ├── Job.cs
│   │   │   ├── Appointment.cs
│   │   │   ├── DispatchMessage.cs
│   │   │   ├── TenantCrmConfig.cs
│   │   │   └── TenantCalendarConfig.cs
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   ├── LeadStatus.cs
│   │   │   ├── JobStatus.cs
│   │   │   ├── CallDirection.cs
│   │   │   ├── DispatchChannel.cs
│   │   │   ├── AppointmentType.cs
│   │   │   ├── AppointmentStatus.cs
│   │   │   ├── CrmType.cs
│   │   │   └── CalendarType.cs
│   │   └── Interfaces/                 ← empty for Phase 0, ready for adapters
│   │
│   └── RadRoofer.Infrastructure/
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Configurations/         ← one IEntityTypeConfiguration per entity
│       │   │   ├── TenantConfiguration.cs
│       │   │   ├── LocationConfiguration.cs
│       │   │   ├── AppUserConfiguration.cs
│       │   │   ├── CallLogConfiguration.cs
│       │   │   ├── LeadConfiguration.cs
│       │   │   ├── CustomerConfiguration.cs
│       │   │   ├── JobConfiguration.cs
│       │   │   ├── AppointmentConfiguration.cs
│       │   │   ├── DispatchMessageConfiguration.cs
│       │   │   ├── TenantCrmConfigConfiguration.cs
│       │   │   └── TenantCalendarConfigConfiguration.cs
│       │   └── Migrations/
│       └── Services/
│           └── (empty for Phase 0 — CRM/calendar adapters go here in Phase 1)
│
└── tests/
    └── RadRoofer.Tests/
        ├── WebhookTests.cs
        ├── AuthTests.cs
        └── QueryFilterTests.cs
```

---

## Build Order

Each step is independently testable before moving to the next.

### Step 1: Scaffold Solution
- Create solution + 4 projects
- Add project references: Api → Core, Api → Infrastructure, Infrastructure → Core, Tests → Api
- Install NuGet packages:
  - **Api:** `Microsoft.AspNetCore.Authentication.JwtBearer`, `Finbuckle.MultiTenant`, `Finbuckle.MultiTenant.AspNetCore`
  - **Core:** (no dependencies — pure domain)
  - **Infrastructure:** `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `BCrypt.Net-Next`
  - **Tests:** `Microsoft.AspNetCore.Mvc.Testing`, `xunit`, `FluentAssertions`

### Step 2: Core Entities + Enums
- Write all 11 entity classes with properties and navigation properties
- Write all 9 enums
- **Verify:** project compiles with zero infrastructure dependencies

### Step 3: DbContext + EF Configurations
- Write `AppDbContext` with:
  - `DbSet<T>` for every entity
  - `OnModelCreating` applying all configurations
  - Global query filters for `TenantId` and `IsDeleted`
  - `SaveChangesAsync` override for `CreatedAt`/`UpdatedAt` timestamps
  - `SaveChangesAsync` override converting `Deleted` state to soft delete
- Write one `IEntityTypeConfiguration<T>` per entity (property constraints, relationships, indexes)
- **Verify:** `dotnet ef migrations add InitialCreate` succeeds and generates correct SQL

### Step 4: Finbuckle Multi-Tenancy
- Register Finbuckle in `Program.cs`
- Configure claim strategy: read `tenant_id` from JWT/cookie claims
- For webhooks: custom `ITenantResolverStrategy` that reads `locationId` from route and resolves tenant
- Inject `IMultiTenantContextAccessor` into `AppDbContext` for query filters
- **Verify:** tenant context resolves correctly from both JWT and locationId routes

### Step 5: Auth
- `AuthController` with login endpoint
- Password hashing with BCrypt
- JWT generation with `tenant_id`, `sub`, `email`, `role` claims
- Cookie authentication for Razor pages
- Dual auth policy scheme (JWT for `/v1/*`, Cookie for Razor)
- **Verify:** login returns valid JWT; JWT decodes with correct claims

### Step 6: Seed Data
- Seed via `DbContext.OnModelCreating` HasData or a startup `IHostedService`
- Seed: 1 Tenant ("Smith Roofing"), 1 Location ("Dallas Office" with VapiSecret), 1 AppUser (owner@smithroofing.com, hashed password)
- **Verify:** `SELECT * FROM "Tenants"` returns one row after migration

### Step 7: Vapi Webhook
- `VapiWebhookController` with `POST /v1/webhooks/vapi/{locationId}`
- `VapiSecretAuthFilter` — action filter that verifies `X-Vapi-Secret`
- Parse Vapi `end-of-call-report` payload
- Create Lead + CallLog
- Idempotency guard on `VapiCallId`
- **Verify:** POST with correct secret → 200 + rows in DB. POST with wrong secret → 401. POST with same VapiCallId → 200 + no duplicate.

### Step 8: Read Endpoints
- `CallsController` — `GET /v1/calls`, `GET /v1/calls/{id}`
- `LeadsController` — `GET /v1/leads`, `GET /v1/leads/{id}`
- `DashboardController` — `GET /v1/dashboard/stats`
- `LocationsController` — `GET /v1/locations`
- `PagedResult<T>` DTO for consistent pagination
- **Verify:** with JWT, GET returns only data for that tenant. Without JWT → 401.

### Step 9: Razor Dashboard
- `Login.cshtml` — email + password form, posts to page handler, sets auth cookie
- `Dashboard.cshtml` — calls `/v1/dashboard/stats` (server-side HttpClient or direct DB query), renders stats + table of recent calls/leads
- `_Layout.cshtml` — minimal layout with nav (just logo + logout)
- No JavaScript framework, no npm — pure Razor with basic CSS
- **Verify:** log in at `/login`, see dashboard with seeded/test data

### Step 10: Deploy to Railway
- Dockerfile or Railway nixpacks auto-detect
- Environment variables: `DATABASE_URL`, `JWT_SECRET`, `ASPNETCORE_ENVIRONMENT`
- Run migrations on startup (or manually via Railway shell)
- Set Vapi webhook URL to Railway URL
- **Verify:** real phone call → lead appears in dashboard

---

## Upgrade Path to Phase 1

When Phase 0 is validated with a real roofer:

| Addition | What changes |
|---|---|
| GoHighLevel CRM | Add `ICrmAdapter` interface in Core, GHL implementation in Infrastructure. Webhook handler calls `adapter.CreateLeadAsync()` after saving to DB. No schema change. |
| Google Calendar | Add `ICalendarAdapter` interface. Webhook handler calls `adapter.BookAppointmentAsync()`. Writes to existing Appointment table. |
| Twilio SMS | Add Twilio client in Infrastructure. Webhook handler inserts into existing DispatchMessage table. Add Hangfire to process the queue. |
| Mobile app | React Native app consumes the same `/v1/*` API endpoints. JWT auth works as-is. |
| Registration | Add `POST /v1/auth/register`. Writes to existing AppUser table. |
| Location CRUD | Add `POST/PUT /v1/locations`. Writes to existing Location table. |

Zero schema migrations needed for any of these. The tables, columns, and indexes are already there.

---

## Success Criteria

Phase 0 is done when:
- [ ] A real call comes in on the Vapi phone number
- [ ] The AI collects name, phone, address, and reason
- [ ] The webhook fires and the API persists a CallLog + Lead
- [ ] The roofer logs into the Razor dashboard and sees the lead
- [ ] Data is tenant-scoped (only their data visible)
- [ ] Duplicate webhooks are handled (idempotent)
