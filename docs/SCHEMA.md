# Database Schema

Full entity relationship design for the Roofers Tech platform. All tables are created in the initial migration. Phase 0 actively writes to a subset; the rest exist with correct relationships and constraints, ready for later phases to populate.

---

## Design Principles

1. **Every entity has `TenantId`** — EF Core global query filter scopes all queries. No exceptions.
2. **`LocationId` where applicable** — Leads, CallLogs, Jobs, Appointments, DispatchMessages are per-location.
3. **Soft deletes** on `Lead`, `Customer`, `Job` — `IsDeleted` + `DeletedAt`, filtered out by default via global query filter.
4. **`CreatedAt` + `UpdatedAt`** on every entity — set automatically in `SaveChanges` override.
5. **GUIDs for all PKs** — avoids sequential ID leaks across tenants, safe for distributed systems later.
6. **`jsonb` for flexible maps** — `StageIdMap`, `BusinessHoursJson` stored as PostgreSQL native JSON.
7. **No cascade deletes across tenant boundaries** — only within parent-child (e.g., Tenant → Location).

---

## Entity Marker Interfaces

Two marker interfaces live in `RadRoofer.Core/Entities/` alongside the entities.

### `IOrganizationIsolated`
Implemented by every entity that carries a `TenantId`. Exposes `Guid TenantId { get; set; }` so
`AppDbContext.OnModelCreating` can identify tenant-scoped entities via type-checking rather than property
reflection. Multi-tenancy is enforced by **Finbuckle.MultiTenant** — do not manually filter on TenantId.

Implementors: `Location`, `AppUser`, `CallLog`, `Lead`, `Customer`, `Job`, `Appointment`,
`DispatchMessage`, `TenantCrmConfig`, `TenantCalendarConfig`

### `IServiceLocationIsolated`
Pure marker (no properties). Implemented by entities that are scoped to a `Location`. Not wired into
any tenant logic — reserved for future use (e.g., generic location-scoped admin queries, reporting).
LocationId is a **shadow FK** on most implementors; `DispatchMessage` is the exception (explicit property).

Implementors: `CallLog`, `Lead`, `Job`, `Appointment`, `DispatchMessage`

### `INearRealTime`
For entities that cache data pulled from an external source (CRM, calendar, etc.) and need freshness
tracking. Exposes `DateTime? LastSynced` and a static `DefaultSyncWaitDuration` (default: 10 minutes).
Callers use the `IsStale()` extension to decide whether to return the cached value or trigger a fresh fetch.

```csharp
// Per-entity threshold override: define a static on the class and pass it in
if (crmConfig.IsStale())                              // uses 10-minute default
if (crmConfig.IsStale(TimeSpan.FromMinutes(30)))      // custom threshold for this call
```

To permanently change the threshold for one entity type, set the constant on the class:
```csharp
public class TenantCrmConfig : BaseEntity, IOrganizationIsolated, INearRealTime
{
    public static readonly TimeSpan SyncWaitDuration = TimeSpan.FromMinutes(30);
    // ... then callers use: crmConfig.IsStale(TenantCrmConfig.SyncWaitDuration)
}
```

**Phase roadmap for `INearRealTime`:**
- Phase 0 (now): `TenantCrmConfig`, `TenantCalendarConfig` — connection/token state
- Phase 1: `Lead` — sync status from GoHighLevel
- Phase 2: `Job` — sync status from JobNimbus/AccuLynx

---

## Entity Definitions

### Tenant

The roofing company. Top-level isolation boundary.

```
Tenant
  Id              Guid        PK
  Name            string      required, max 200
  Phone           string?     max 20
  Email           string?     max 200
  CreatedAt       DateTime    auto-set
  UpdatedAt       DateTime    auto-set

  -- Navigation --
  Locations           → List<Location>
  Users               → List<AppUser>
  CrmConfig           → TenantCrmConfig?
  CalendarConfig      → TenantCalendarConfig?
```

**Indexes:** none beyond PK (tenants are few, always looked up by Id)

---

### Location

A physical office/branch. Each location gets its own Vapi number and assistant.

```
Location
  Id                  Guid        PK
  TenantId            Guid        FK → Tenant.Id, required
  Name                string      required, max 200
  Address             string?     max 500
  Phone               string?     max 20
  Timezone            string      required, default "America/New_York", max 50
  VapiAssistantId     string?     max 100
  VapiPhoneNumber     string?     max 20
  VapiSecret          string?     max 200  (per-location webhook secret)
  BusinessHoursJson   string?     jsonb — see structure below
  IsActive            bool        default true
  CreatedAt           DateTime    auto-set
  UpdatedAt           DateTime    auto-set

  -- Navigation --
  Tenant              → Tenant
  CallLogs            → List<CallLog>
  Leads               → List<Lead>
  Jobs                → List<Job>
  Appointments        → List<Appointment>
```

**Indexes:**
- `IX_Location_TenantId` on `TenantId`
- `IX_Location_VapiSecret` unique on `VapiSecret` (webhook lookup)

**BusinessHoursJson structure:**
```json
{
  "monday":    { "open": "08:00", "close": "17:00" },
  "tuesday":   { "open": "08:00", "close": "17:00" },
  "wednesday": { "open": "08:00", "close": "17:00" },
  "thursday":  { "open": "08:00", "close": "17:00" },
  "friday":    { "open": "08:00", "close": "16:00" },
  "saturday":  null,
  "sunday":    null
}
```

---

### AppUser

A human who logs into the platform. Belongs to one tenant.

```
AppUser
  Id              Guid        PK
  TenantId        Guid        FK → Tenant.Id, required
  Email           string      required, max 200
  PasswordHash    string      required, max 500
  FullName        string      required, max 200
  Role            UserRole    enum: Owner, Manager, Technician
  IsActive        bool        default true
  CreatedAt       DateTime    auto-set
  UpdatedAt       DateTime    auto-set

  -- Navigation --
  Tenant          → Tenant
```

**Indexes:**
- `IX_AppUser_TenantId` on `TenantId`
- `IX_AppUser_Email` unique on `Email` (login lookup — globally unique)

**Enum: UserRole**
```
Owner       = 0    // full access, billing, settings
Manager     = 1    // location management, reporting
Technician  = 2    // read-only dashboard, own schedule
```

---

### CallLog

Every Vapi call recorded — inbound and outbound. Immutable after creation (no UpdatedAt).

```
CallLog
  Id              Guid            PK
  TenantId        Guid            FK → Tenant.Id, required
  LocationId      Guid            FK → Location.Id, required
  LeadId          Guid?           FK → Lead.Id, nullable (not every call creates a lead)
  VapiCallId      string          required, max 200 (Vapi's external ID for dedup)
  CallerPhone     string          required, max 20
  CallerName      string?         max 200
  Summary         string?         max 2000 (AI-generated call summary)
  Transcript      string?         text (full transcript from Vapi)
  DurationSeconds int             default 0
  Direction       CallDirection   enum: Inbound, Outbound
  IsAfterHours    bool            default false
  CreatedAt       DateTime        auto-set

  -- Navigation --
  Tenant          → Tenant
  Location        → Location
  Lead            → Lead?
```

**Indexes:**
- `IX_CallLog_TenantId` on `TenantId`
- `IX_CallLog_LocationId` on `LocationId`
- `IX_CallLog_VapiCallId` unique on `VapiCallId` (idempotency)
- `IX_CallLog_CreatedAt` on `CreatedAt` desc (dashboard queries)

**Enum: CallDirection**
```
Inbound     = 0
Outbound    = 1
```

---

### Lead

A potential customer captured from an inbound call. Can be promoted to Customer.

```
Lead
  Id              Guid        PK
  TenantId        Guid        FK → Tenant.Id, required
  LocationId      Guid        FK → Location.Id, required
  CustomerId      Guid?       FK → Customer.Id, nullable (set when lead converts)
  CallerName      string      required, max 200
  CallerPhone     string      required, max 20
  Email           string?     max 200
  Address         string?     max 500
  ReasonForCall   string?     max 1000
  Source          string      required, max 50, default "inbound-call"
  ReferralSource  string?     max 200 (Phase 2: "How did you hear about us?")
  Status          LeadStatus  enum, default New
  IsDeleted       bool        default false
  DeletedAt       DateTime?   null until soft-deleted
  CreatedAt       DateTime    auto-set
  UpdatedAt       DateTime    auto-set

  -- Navigation --
  Tenant          → Tenant
  Location        → Location
  Customer        → Customer?
  CallLogs        → List<CallLog>
  Appointments    → List<Appointment>
```

**Indexes:**
- `IX_Lead_TenantId` on `TenantId`
- `IX_Lead_LocationId` on `LocationId`
- `IX_Lead_CallerPhone_TenantId` on (`CallerPhone`, `TenantId`) (phone lookup for returning callers)
- `IX_Lead_Status` on `Status` (filtered queries)
- `IX_Lead_CreatedAt` on `CreatedAt` desc

**Enum: LeadStatus**
```
New             = 0    // just captured
Contacted       = 1    // roofer has followed up
EstimateSent    = 2    // estimate delivered
Won             = 3    // converted to customer/job
Lost            = 4    // didn't close
```

---

### Customer

A lead that converted. Owns jobs and appointments. Separate from Lead to model the lifecycle.

```
Customer
  Id              Guid        PK
  TenantId        Guid        FK → Tenant.Id, required
  Name            string      required, max 200
  Phone           string      required, max 20
  Email           string?     max 200
  Address         string?     max 500
  IsDeleted       bool        default false
  DeletedAt       DateTime?
  CreatedAt       DateTime    auto-set
  UpdatedAt       DateTime    auto-set

  -- Navigation --
  Tenant          → Tenant
  Leads           → List<Lead>  (a customer may have been a lead multiple times)
  Jobs            → List<Job>
  Appointments    → List<Appointment>
```

**Indexes:**
- `IX_Customer_TenantId` on `TenantId`
- `IX_Customer_Phone_TenantId` on (`Phone`, `TenantId`) (phone lookup)

---

### Job

A roofing work order. Tied to a customer at a specific location.

```
Job
  Id                  Guid        PK
  TenantId            Guid        FK → Tenant.Id, required
  LocationId          Guid        FK → Location.Id, required
  CustomerId          Guid        FK → Customer.Id, required
  Title               string      required, max 300
  Description         string?     max 2000
  Status              JobStatus   enum, default New
  ExternalCrmJobId    string?     max 200 (ID in the connected CRM)

  -- Insurance fields (Phase 2, schema now) --
  IsInsuranceJob      bool        default false
  InsuranceCarrier    string?     max 200
  ClaimNumber         string?     max 100
  AdjusterName        string?     max 200
  AdjusterPhone       string?     max 20
  AdjusterEmail       string?     max 200
  DeductibleAmount    decimal?

  -- Soft delete --
  IsDeleted           bool        default false
  DeletedAt           DateTime?
  CreatedAt           DateTime    auto-set
  UpdatedAt           DateTime    auto-set

  -- Navigation --
  Tenant              → Tenant
  Location            → Location
  Customer            → Customer
  Appointments        → List<Appointment>
```

**Indexes:**
- `IX_Job_TenantId` on `TenantId`
- `IX_Job_LocationId` on `LocationId`
- `IX_Job_CustomerId` on `CustomerId`
- `IX_Job_Status` on `Status`
- `IX_Job_ExternalCrmJobId` on `ExternalCrmJobId` (CRM sync lookup)

**Enum: JobStatus**
```
New             = 0
InspectionScheduled = 1
EstimateSent    = 2
Approved        = 3
InProgress      = 4
Completed       = 5
Cancelled       = 6
```

---

### Appointment

Scheduled inspections, estimates, or follow-ups. Can belong to a lead (pre-conversion) or a job (post-conversion).

```
Appointment
  Id                          Guid                PK
  TenantId                    Guid                FK → Tenant.Id, required
  LocationId                  Guid                FK → Location.Id, required
  LeadId                      Guid?               FK → Lead.Id, nullable
  JobId                       Guid?               FK → Job.Id, nullable
  CustomerId                  Guid?               FK → Customer.Id, nullable
  StartTime                   DateTime            required
  EndTime                     DateTime            required
  Type                        AppointmentType     enum
  Status                      AppointmentStatus   enum, default Scheduled
  ExternalCalendarEventId     string?             max 200 (Google Calendar event ID)
  Notes                       string?             max 1000
  CreatedAt                   DateTime            auto-set
  UpdatedAt                   DateTime            auto-set

  -- Navigation --
  Tenant        → Tenant
  Location      → Location
  Lead          → Lead?
  Job           → Job?
  Customer      → Customer?
```

**Indexes:**
- `IX_Appointment_TenantId` on `TenantId`
- `IX_Appointment_LocationId` on `LocationId`
- `IX_Appointment_StartTime` on `StartTime` (availability queries)
- `IX_Appointment_ExternalCalendarEventId` on `ExternalCalendarEventId` (calendar sync)

**Enum: AppointmentType**
```
Inspection      = 0
Estimate        = 1
FollowUp        = 2
AdjusterVisit   = 3    // Phase 3
```

**Enum: AppointmentStatus**
```
Scheduled       = 0
Confirmed       = 1
Completed       = 2
Cancelled       = 3
NoShow          = 4
```

---

### DispatchMessage

Outgoing SMS/email queue. Rows are inserted immediately; a background processor sends them at `SendAt` time.

```
DispatchMessage
  Id                  Guid                PK
  TenantId            Guid                FK → Tenant.Id, required
  LocationId          Guid?               FK → Location.Id, nullable (some messages are tenant-wide)
  Channel             DispatchChannel     enum: Sms, Email
  To                  string              required, max 200 (phone or email)
  Subject             string?             max 300 (email only)
  Body                string              required, max 4000
  SendAt              DateTime            required (when to send — can be now or future)
  SentAt              DateTime?           null until sent
  Error               string?             max 1000 (last error if send failed)
  RetryCount          int                 default 0
  RelatedEntityType   string?             max 50 (e.g. "Lead", "Job", "Appointment")
  RelatedEntityId     Guid?               (FK to the related entity for traceability)
  CreatedAt           DateTime            auto-set

  -- Navigation --
  Tenant              → Tenant
  Location            → Location?
```

**Indexes:**
- `IX_DispatchMessage_TenantId` on `TenantId`
- `IX_DispatchMessage_Pending` on (`SendAt`, `SentAt`) where `SentAt IS NULL` (queue processor query)
- `IX_DispatchMessage_RelatedEntity` on (`RelatedEntityType`, `RelatedEntityId`)

**Enum: DispatchChannel**
```
Sms     = 0
Email   = 1
```

---

### TenantCrmConfig

CRM connection credentials and cached stage mappings. One per tenant.

```
TenantCrmConfig
  Id                  Guid        PK
  TenantId            Guid        FK → Tenant.Id, required, unique
  CrmType             CrmType     enum
  EncryptedApiKey     string?     max 1000 (JobNimbus, AccuLynx)
  AccessToken         string?     max 2000 (GHL, HubSpot)
  RefreshToken        string?     max 2000 (GHL, HubSpot)
  TokenExpiresAt      DateTime?
  StageIdMap          string?     jsonb — { "Lead": "abc123", "Won": "def456" }
  CustomFieldMap      string?     jsonb — { "InsuranceCarrier": "cf_12345" }
  IsConnected         bool        default false
  CreatedAt           DateTime    auto-set
  UpdatedAt           DateTime    auto-set

  -- Navigation --
  Tenant              → Tenant
```

**Indexes:**
- `IX_TenantCrmConfig_TenantId` unique on `TenantId` (one CRM config per tenant)

**Enum: CrmType**
```
GoHighLevel     = 0
JobNimbus       = 1
AccuLynx        = 2
HubSpot         = 3
```

**StageIdMap example (GoHighLevel):**
```json
{
  "New":              "pipeline_stage_abc",
  "Contacted":        "pipeline_stage_def",
  "EstimateSent":     "pipeline_stage_ghi",
  "Won":              "pipeline_stage_jkl",
  "Lost":             "pipeline_stage_mno"
}
```

**CustomFieldMap example:**
```json
{
  "InsuranceCarrier":   "cf_insurance_company",
  "ClaimNumber":        "cf_claim_no",
  "AdjusterName":       "cf_adjuster",
  "DeductibleAmount":   "cf_deductible"
}
```

---

### TenantCalendarConfig

Calendar connection credentials. One per tenant.

```
TenantCalendarConfig
  Id                  Guid            PK
  TenantId            Guid            FK → Tenant.Id, required, unique
  CalendarType        CalendarType    enum
  CalendarId          string?         max 200 (e.g. Google Calendar ID)
  AccessToken         string?         max 2000
  RefreshToken        string?         max 2000
  TokenExpiresAt      DateTime?
  IsConnected         bool            default false
  CreatedAt           DateTime        auto-set
  UpdatedAt           DateTime        auto-set

  -- Navigation --
  Tenant              → Tenant
```

**Indexes:**
- `IX_TenantCalendarConfig_TenantId` unique on `TenantId`

**Enum: CalendarType**
```
GoogleCalendar  = 0
Outlook         = 1
Calendly        = 2
```

---

## Relationship Map

```
Tenant ──1:N──► Location
Tenant ──1:N──► AppUser
Tenant ──1:1──► TenantCrmConfig
Tenant ──1:1──► TenantCalendarConfig
Tenant ──1:N──► Lead
Tenant ──1:N──► Customer
Tenant ──1:N──► Job
Tenant ──1:N──► Appointment
Tenant ──1:N──► CallLog
Tenant ──1:N──► DispatchMessage

Location ──1:N──► CallLog
Location ──1:N──► Lead
Location ──1:N──► Job
Location ──1:N──► Appointment
Location ──1:N──► DispatchMessage

Lead ───N:1──► Customer?        (nullable — set on conversion)
Lead ───1:N──► CallLog          (calls that produced this lead)
Lead ───1:N──► Appointment      (pre-conversion appointments)

Customer ──1:N──► Job
Customer ──1:N──► Appointment
Customer ──1:N──► Lead          (may have been a lead multiple times)

Job ────1:N──► Appointment
```

---

## EF Core Pseudo Code

### Global Query Filters

```csharp
// Applied to EVERY entity in OnModelCreating
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (entityType has TenantId property)
        add filter: o => o.TenantId == currentTenantId

    if (entityType has IsDeleted property)
        add filter: o => !o.IsDeleted
}
```

### SaveChanges Override

```csharp
override SaveChangesAsync:
    foreach entry in ChangeTracker.Entries:
        if entry.State == Added:
            set CreatedAt = UtcNow
            set UpdatedAt = UtcNow
            if TenantId is empty:
                set TenantId = currentTenantId  // safety net
        if entry.State == Modified:
            set UpdatedAt = UtcNow
```

### Soft Delete Override

```csharp
// Instead of DELETE, set IsDeleted = true + DeletedAt = UtcNow
// Can be done via SaveChanges interceptor or explicit service method
override SaveChangesAsync:
    foreach entry where State == Deleted and entity has IsDeleted:
        entry.State = Modified
        entry.Entity.IsDeleted = true
        entry.Entity.DeletedAt = UtcNow
```

### Entity Configuration Example (Lead)

```csharp
class LeadConfiguration : IEntityTypeConfiguration<Lead>
    Configure(builder):
        builder.ToTable("Leads")
        builder.HasKey(o => o.Id)
        builder.Property(o => o.CallerName).IsRequired().HasMaxLength(200)
        builder.Property(o => o.CallerPhone).IsRequired().HasMaxLength(20)
        builder.Property(o => o.Email).HasMaxLength(200)
        builder.Property(o => o.Address).HasMaxLength(500)
        builder.Property(o => o.ReasonForCall).HasMaxLength(1000)
        builder.Property(o => o.Source).IsRequired().HasMaxLength(50).HasDefaultValue("inbound-call")
        builder.Property(o => o.ReferralSource).HasMaxLength(200)
        builder.Property(o => o.Status).HasConversion<int>()

        // Relationships
        builder.HasOne(o => o.Tenant).WithMany().HasForeignKey(o => o.TenantId).OnDelete(Restrict)
        builder.HasOne(o => o.Location).WithMany(o => o.Leads).HasForeignKey(o => o.LocationId).OnDelete(Restrict)
        builder.HasOne(o => o.Customer).WithMany(o => o.Leads).HasForeignKey(o => o.CustomerId).OnDelete(SetNull)

        // Indexes
        builder.HasIndex(o => o.TenantId)
        builder.HasIndex(o => new { o.CallerPhone, o.TenantId })
        builder.HasIndex(o => o.Status)
        builder.HasIndex(o => o.CreatedAt).IsDescending()
```

---

## Phase Activation Map

Shows which entities are actively written to in each phase.

| Entity              | Phase 0 | Phase 1 | Phase 2 | Phase 3 |
|---------------------|---------|---------|---------|---------|
| Tenant              | Write   | Write   | Write   | Write   |
| Location            | Write   | Write   | Write   | Write   |
| AppUser             | Write   | Write   | Write   | Write   |
| CallLog             | Write   | Write   | Write   | Write   |
| Lead                | Write   | Write   | Write   | Write   |
| Customer            | --      | Write   | Write   | Write   |
| Job                 | --      | --      | Write   | Write   |
| Appointment         | --      | Write   | Write   | Write   |
| DispatchMessage     | --      | Write   | Write   | Write   |
| TenantCrmConfig     | --      | Write   | Write   | Write   |
| TenantCalendarConfig| --      | Write   | Write   | Write   |

---

## Migration Strategy

- **Initial migration** creates ALL tables, ALL indexes, ALL constraints
- Future phases add columns (e.g., new fields on Job) or new tables — never restructure existing ones
- Schema-only tables cost nothing: empty tables with indexes have zero runtime overhead
- If a new enum value is needed, it's just an `int` in the DB — add the C# enum member, no migration required
