# API Surface

All REST endpoints for the Roofers Tech platform. Phase 0 endpoints are implemented first; later phases add endpoints on top of the same controllers.

Base URL: `/v1/`

---

## Auth

### `POST /v1/auth/login`

Authenticate a user and return a JWT.

**Auth:** None (public)

**Request:**
```json
{
  "email": "owner@example.com",
  "password": "s3cret"
}
```

**Response 200:**
```json
{
  "token": "eyJhbG...",
  "expiresAt": "2026-03-25T12:00:00Z",
  "user": {
    "id": "guid",
    "email": "owner@example.com",
    "fullName": "Mike Smith",
    "role": "Owner",
    "tenantId": "guid",
    "tenantName": "Smith Roofing"
  }
}
```

**Response 401:**
```json
{ "error": "Invalid email or password." }
```

**Pseudo code:**
```
handler(request):
    user = db.AppUsers.FirstOrDefault(o => o.Email == request.Email && o.IsActive)
    if user is null → return 401

    if !verifyPassword(request.Password, user.PasswordHash) → return 401

    claims = {
        sub:        user.Id,
        email:      user.Email,
        tenant_id:  user.TenantId,
        role:       user.Role,
        exp:        now + 7 days
    }
    token = generateJwt(claims)

    return 200 { token, expiresAt, user: { ... } }
```

---

## Vapi Webhooks

### `POST /v1/webhooks/vapi/{locationId}`

Receives Vapi call events. This is the heart of Phase 0.

**Auth:** `X-Vapi-Secret` header verified against `Location.VapiSecret`

**Vapi sends multiple event types.** We care about `end-of-call-report` for Phase 0.

**Request (end-of-call-report from Vapi):**
```json
{
  "message": {
    "type": "end-of-call-report",
    "call": {
      "id": "vapi-call-uuid",
      "type": "inboundPhoneCall",
      "startedAt": "2026-03-24T14:30:00Z",
      "endedAt": "2026-03-24T14:35:00Z",
      "customer": {
        "number": "+15551234567"
      }
    },
    "summary": "Customer called about storm damage to their roof...",
    "transcript": "Agent: Hi, thanks for calling Smith Roofing...",
    "analysis": {
      "structuredData": {
        "callerName": "Jane Doe",
        "callerPhone": "+15551234567",
        "address": "123 Oak St, Dallas TX",
        "reasonForCall": "Storm damage - missing shingles after last night's hail"
      }
    }
  }
}
```

**Response 200:**
```json
{ "status": "ok", "callLogId": "guid", "leadId": "guid" }
```

**Response 401:**
```json
{ "error": "Invalid webhook secret." }
```

**Response 404:**
```json
{ "error": "Location not found." }
```

**Pseudo code:**
```
handler(locationId, request):
    // 1. Resolve location (bypasses tenant filter — needs raw query)
    location = db.Locations.IgnoreQueryFilters()
                 .FirstOrDefault(o => o.Id == locationId && o.IsActive)
    if location is null → return 404

    // 2. Verify webhook secret
    secret = request.Headers["X-Vapi-Secret"]
    if secret != location.VapiSecret → return 401

    // 3. Set tenant context for all downstream queries
    setCurrentTenant(location.TenantId)

    // 4. Parse the event
    message = request.Body.message
    if message.type != "end-of-call-report" → return 200 { status: "ignored" }

    // 5. Idempotency check — don't re-process the same call
    vapiCallId = message.call.id
    existing = db.CallLogs.FirstOrDefault(o => o.VapiCallId == vapiCallId)
    if existing is not null → return 200 { status: "duplicate", callLogId: existing.Id }

    // 6. Extract structured data from Vapi analysis
    data = message.analysis.structuredData
    callerPhone = data?.callerPhone ?? message.call.customer.number
    callerName  = data?.callerName
    address     = data?.address
    reason      = data?.reasonForCall
    summary     = message.summary
    transcript  = message.transcript

    // 7. Calculate duration
    duration = (message.call.endedAt - message.call.startedAt).TotalSeconds

    // 8. Determine if after-hours (Phase 0: simple check against location business hours)
    isAfterHours = checkAfterHours(location.BusinessHoursJson, location.Timezone, message.call.startedAt)

    // 9. Create Lead (if we have enough info)
    lead = null
    if callerPhone is not empty:
        lead = new Lead {
            TenantId     = location.TenantId,
            LocationId   = location.Id,
            CallerName   = callerName ?? "Unknown",
            CallerPhone  = callerPhone,
            Address      = address,
            ReasonForCall = reason,
            Source       = "inbound-call",
            Status       = LeadStatus.New
        }
        db.Leads.Add(lead)

    // 10. Create CallLog
    callLog = new CallLog {
        TenantId        = location.TenantId,
        LocationId      = location.Id,
        LeadId          = lead?.Id,
        VapiCallId      = vapiCallId,
        CallerPhone     = callerPhone ?? "unknown",
        CallerName      = callerName,
        Summary         = summary,
        Transcript      = transcript,
        DurationSeconds = (int)duration,
        Direction       = CallDirection.Inbound,
        IsAfterHours    = isAfterHours
    }
    db.CallLogs.Add(callLog)

    // 11. Save
    await db.SaveChangesAsync()

    return 200 { status: "ok", callLogId: callLog.Id, leadId: lead?.Id }
```

**Key design decisions in this handler:**
- **Idempotency via `VapiCallId`** — Vapi may retry webhooks. The unique index prevents duplicates.
- **Location lookup bypasses tenant filter** — we don't know the tenant yet; the location tells us.
- **Lead is optional** — if Vapi can't extract a phone number, we still log the call but skip lead creation.
- **No CRM sync, no SMS** — Phase 0 just persists. Phase 1 adds: enqueue CRM lead creation + SMS confirmation after save.

---

## Calls

### `GET /v1/calls`

List call logs for the current tenant. Paginated, newest first.

**Auth:** JWT (Bearer token)

**Query params:**
| Param       | Type     | Default | Description |
|-------------|----------|---------|-------------|
| `locationId`| Guid?    | null    | Filter by location |
| `page`      | int      | 1       | Page number |
| `pageSize`  | int      | 25      | Items per page (max 100) |

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "locationId": "guid",
      "locationName": "Dallas Office",
      "callerPhone": "+15551234567",
      "callerName": "Jane Doe",
      "summary": "Storm damage - missing shingles...",
      "durationSeconds": 300,
      "direction": "Inbound",
      "isAfterHours": false,
      "leadId": "guid",
      "createdAt": "2026-03-24T14:35:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 42
}
```

**Pseudo code:**
```
handler(query):
    // Tenant is already resolved from JWT by Finbuckle middleware
    calls = db.CallLogs
        .Include(o => o.Location)
        .OrderByDescending(o => o.CreatedAt)

    if query.locationId has value:
        calls = calls.Where(o => o.LocationId == query.locationId)

    totalCount = await calls.CountAsync()
    items = await calls
        .Skip((query.page - 1) * query.pageSize)
        .Take(min(query.pageSize, 100))
        .Select(o => new CallLogDto { ... })
        .ToListAsync()

    return 200 { items, page, pageSize, totalCount }
```

---

### `GET /v1/calls/{id}`

Get a single call log with full transcript.

**Auth:** JWT

**Response 200:**
```json
{
  "id": "guid",
  "locationId": "guid",
  "locationName": "Dallas Office",
  "callerPhone": "+15551234567",
  "callerName": "Jane Doe",
  "summary": "Storm damage - missing shingles...",
  "transcript": "Agent: Hi, thanks for calling Smith Roofing...",
  "durationSeconds": 300,
  "direction": "Inbound",
  "isAfterHours": false,
  "leadId": "guid",
  "createdAt": "2026-03-24T14:35:00Z"
}
```

---

## Leads

### `GET /v1/leads`

List leads for the current tenant. Paginated, newest first.

**Auth:** JWT

**Query params:**
| Param       | Type        | Default | Description |
|-------------|-------------|---------|-------------|
| `locationId`| Guid?       | null    | Filter by location |
| `status`    | LeadStatus? | null    | Filter by status |
| `page`      | int         | 1       | Page number |
| `pageSize`  | int         | 25      | Items per page (max 100) |

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "locationId": "guid",
      "locationName": "Dallas Office",
      "callerName": "Jane Doe",
      "callerPhone": "+15551234567",
      "address": "123 Oak St, Dallas TX",
      "reasonForCall": "Storm damage - missing shingles",
      "source": "inbound-call",
      "status": "New",
      "createdAt": "2026-03-24T14:35:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 18
}
```

**Pseudo code:**
```
handler(query):
    leads = db.Leads
        .Include(o => o.Location)
        .OrderByDescending(o => o.CreatedAt)

    if query.locationId has value:
        leads = leads.Where(o => o.LocationId == query.locationId)
    if query.status has value:
        leads = leads.Where(o => o.Status == query.status)

    totalCount = await leads.CountAsync()
    items = await leads
        .Skip((query.page - 1) * query.pageSize)
        .Take(min(query.pageSize, 100))
        .Select(o => new LeadDto { ... })
        .ToListAsync()

    return 200 { items, page, pageSize, totalCount }
```

---

### `GET /v1/leads/{id}`

Get a single lead with full details.

**Auth:** JWT

---

## Dashboard Stats

### `GET /v1/dashboard/stats`

Aggregated stats for the Razor dashboard page.

**Auth:** JWT

**Query params:**
| Param       | Type  | Default | Description |
|-------------|-------|---------|-------------|
| `locationId`| Guid? | null    | Filter by location (null = all locations) |

**Response 200:**
```json
{
  "callsToday": 12,
  "callsThisWeek": 47,
  "leadsToday": 8,
  "leadsThisWeek": 31,
  "afterHoursCallsThisWeek": 5
}
```

**Pseudo code:**
```
handler(query):
    today = utcNow.Date
    weekStart = today.AddDays(-(int)today.DayOfWeek)  // Sunday

    baseCallQuery = db.CallLogs.AsQueryable()
    baseLeadQuery = db.Leads.AsQueryable()

    if query.locationId has value:
        baseCallQuery = baseCallQuery.Where(o => o.LocationId == query.locationId)
        baseLeadQuery = baseLeadQuery.Where(o => o.LocationId == query.locationId)

    return 200 {
        callsToday         = await baseCallQuery.CountAsync(o => o.CreatedAt >= today),
        callsThisWeek      = await baseCallQuery.CountAsync(o => o.CreatedAt >= weekStart),
        leadsToday         = await baseLeadQuery.CountAsync(o => o.CreatedAt >= today),
        leadsThisWeek      = await baseLeadQuery.CountAsync(o => o.CreatedAt >= weekStart),
        afterHoursCallsThisWeek = await baseCallQuery.CountAsync(o => o.CreatedAt >= weekStart && o.IsAfterHours)
    }
```

---

## Locations (read-only for Phase 0)

### `GET /v1/locations`

List locations for the current tenant. Needed by the Razor page if we add a location dropdown later.

**Auth:** JWT

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Dallas Office",
      "address": "456 Main St, Dallas TX",
      "phone": "+15559876543",
      "vapiPhoneNumber": "+15551112222",
      "isActive": true
    }
  ]
}
```

---

## Endpoints Deferred to Phase 1+

These endpoints are NOT built in Phase 0 but are listed here for completeness.

### Phase 1
| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/v1/auth/register` | User registration (owner creates first user during onboarding) |
| `POST` | `/v1/locations` | Create a new location |
| `PUT`  | `/v1/locations/{id}` | Update location settings |
| `PUT`  | `/v1/locations/{id}/assistant` | Toggle Vapi assistant on/off |
| `GET`  | `/v1/integrations/crm/gohighlevel/connect` | Start GHL OAuth flow |
| `GET`  | `/v1/integrations/crm/gohighlevel/callback` | GHL OAuth callback |
| `GET`  | `/v1/integrations/calendar/google/connect` | Start Google Calendar OAuth flow |
| `GET`  | `/v1/integrations/calendar/google/callback` | Google Calendar OAuth callback |
| `GET`  | `/v1/appointments` | List appointments |
| `POST` | `/v1/appointments` | Book appointment |

### Phase 2
| Method | Route | Purpose |
|--------|-------|---------|
| `PUT`  | `/v1/leads/{id}/status` | Update lead status |
| `POST` | `/v1/leads/{id}/convert` | Convert lead to customer + job |
| `GET`  | `/v1/jobs` | List jobs |
| `GET`  | `/v1/jobs/{id}/status` | Job status (also used by AI for status lookup) |
| `PUT`  | `/v1/locations/{id}/storm-mode` | Toggle storm response mode |
| `POST` | `/v1/integrations/crm/jobnimbus/connect` | Paste JobNimbus API key |
| `POST` | `/v1/integrations/crm/acculynx/connect` | Paste AccuLynx API key |

---

## Auth Strategy

### JWT for API endpoints
- All `/v1/*` endpoints (except webhooks) require `Authorization: Bearer <token>`
- JWT contains `sub`, `email`, `tenant_id`, `role`, `exp`
- Finbuckle reads `tenant_id` claim and sets tenant context automatically
- Token lifetime: 7 days (Phase 0 — no refresh token flow yet)

### Cookie auth for Razor pages
- Razor login page posts to a server-side action that validates credentials
- On success, sets an auth cookie (ASP.NET Core cookie authentication)
- Cookie contains the same claims as the JWT
- Finbuckle reads `tenant_id` from the cookie claims — same resolution path

### Webhook auth
- `X-Vapi-Secret` header verified against `Location.VapiSecret`
- No JWT, no cookie — Vapi is a service, not a user
- Location lookup resolves the tenant — no Finbuckle middleware needed

### Dual auth setup (pseudo code)
```
Program.cs:
    services.AddAuthentication(defaultScheme: "MultiAuth")
        .AddJwtBearer("Jwt", options => {
            options.TokenValidationParameters = {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKey = signingKey
            }
        })
        .AddCookie("Cookie", options => {
            options.LoginPath = "/login"
            options.ExpireTimeSpan = 7 days
        })
        .AddPolicyScheme("MultiAuth", options => {
            // If request path starts with /v1/ → use JWT
            // Otherwise (Razor pages) → use Cookie
            options.ForwardDefaultSelector = context => {
                if context.Request.Path.StartsWithSegments("/v1"):
                    return "Jwt"
                return "Cookie"
            }
        })
```

---

## Error Response Format

All errors follow a consistent shape:

```json
{
  "error": "Human-readable error message.",
  "details": { }
}
```

HTTP status codes:
- `400` — Bad request / validation failure
- `401` — Not authenticated
- `403` — Authenticated but not authorized (wrong role/tenant)
- `404` — Entity not found (within tenant scope)
- `409` — Conflict (duplicate VapiCallId, etc.)
- `500` — Unhandled server error (log, don't expose internals)
