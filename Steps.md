# Steps — MVP Build Order

Everything needed to go from zero to a deployed, working MVP.
Each step is independently testable before moving to the next.

---

## Pre-Development

### 1. Local Dev Environment
- [X] .NET 9 SDK installed - Restart PC
- [X] PostgreSQL running locally on railway (dev db)
 - database_public_url: postgresql://postgres:ArElpQFgPILaYpPJQIshRZyybqBEdKWB@gondola.proxy.rlwy.net:38990/railway
 - database_url: postgresql://postgres:ArElpQFgPILaYpPJQIshRZyybqBEdKWB@postgres.railway.internal:5432/railway
- [X] Temp webhook endpoint: http://webhook-server-production-2687.up.railway.app/webhook-1
- [x] Vapi account created assistant(50706042-0dc5-44ac-9978-ed92c3b5f01b) - phone number (8155858426)
- [X] Node 20+ installed (`node -v`)

---

## Backend — .NET API

### 2. Scaffold Solution
- [x] `RadRoofer.sln` with 3 projects: `Api`, `Core`, `Infrastructure`
- [x] Project references: Api → Core + Infrastructure, Infrastructure → Core
- [x] `dotnet build` succeeds

### 3. Core Entities
Keep only what the MVP needs:
- [x] `Organization` — tenant root
- [x] `AppUser` — login
- [x] `ServiceLocation` — location per tenant
- [x] `CallLog` — Vapi call audit trail
- [x] `Appointment` — calendar booking created from each call

### 4. DbContext + EF Config
- [x] `AppDbContext` with DbSets for the 5 entities above
- [x] Global query filters on all entities (`WHERE OrganizationId = current_tenant`)
- [x] `SaveChangesAsync` override sets `CreatedAt`/`UpdatedAt`
- [x] `IEntityTypeConfiguration<T>` for each entity
- [x] `ServiceLocation` includes `VapiEnabled` (bool, default true) and `CalendarId` (string?)
- [x] Run initial migration:
  ```bash
  dotnet ef migrations add InitialCreate --project RadRoofer.Infrastructure --startup-project RadRoofer.Api
  ```
- [x] Verify: migration SQL looks correct for 5 tables, `ServiceLocations` has `VapiEnabled` + `CalendarId` columns

> **If you already have an InitialCreate migration and need to add the new columns**, run:
> ```bash
> dotnet ef migrations add AddLocationVapiAndCalendar --project RadRoofer.Infrastructure --startup-project RadRoofer.Api
> dotnet ef database update --project RadRoofer.Infrastructure --startup-project RadRoofer.Api
> ```

### 5. Multi-Tenancy + Auth
- [x] Finbuckle registered with `WithClaimStrategy("tenant_id")`
- [x] JWT Bearer for `/v1/*` routes, Cookie for Razor pages (SmartScheme)
- [x] Seed: 1 Organization + 1 ServiceLocation + 1 AppUser
- [x] `POST /v1/auth/login` → returns JWT with `tenant_id` claim
- [x] Verify: login returns JWT, wrong password → 401

### 6. Vapi Webhook → Google Calendar
- [x] `POST /v1/webhooks/vapi/{locationId}` with `VapiSecretAuthFilter`
- [x] Parses `end-of-call-report`: callerName, callerPhone, address, reason
- [x] Calls `GoogleCalendarService.CreateEventAsync()` → creates event on configured calendar
- [x] Saves `Appointment` to DB (`Notes` = caller summary, `ExternalId` = Google Calendar event ID)
- [x] Idempotency: duplicate webhook → no duplicate rows
- [ ] Verify: correct secret → 200 + appointment in DB + event in Google Calendar. Wrong secret → 401.

### 7. Read Endpoints
- [x] `GET /v1/appointments` — paginated, tenant-scoped, optional `?locationId=` filter
- [x] `GET /v1/appointments/{id}` — single appointment
- [x] `GET /v1/appointments/stats` — `{ today, thisWeek }` counts
- [x] `GET /v1/locations` — list tenant's service locations
- [ ] Verify: with JWT → returns only that tenant's data. Without JWT → 401.

### 8. Google Calendar Setup
- [x] Create a Google Cloud project
- [x] Enable the Google Calendar API
- [x] Create a Service Account → download JSON key
- [x] Share a Google Calendar with the service account email (Editor role)
  - Service account email: `radroofer-calendar@radroofer.iam.gserviceaccount.com`
- [x] Copy the Calendar ID from Google Calendar settings
- [x] Set env vars locally:
  - `GoogleCalendar__ServiceAccountJsonPath` → path to downloaded JSON key
  - `GoogleCalendar__CalendarId` → set in appsettings.Development.json
- [ ] Verify: fire a test webhook → event appears in Google Calendar

---

## Mobile App — Expo

### 9. Install + Configure
```bash
cd mobile
cp .env.example .env
# Set EXPO_PUBLIC_API_URL to your local API (use LAN IP, not localhost, for physical device)
npm install
```
- [x] `.env` created with `EXPO_PUBLIC_API_URL=http://192.168.1.189:5070`
- [x] `npm install` complete

### 10. Run in Development
```bash
npm start   # scan QR with Expo Go app on your phone
```

### 11. Verify Each Screen
- [ ] Login → enter seeded credentials → lands on Dashboard
- [ ] Dashboard shows "Appts Today" and "Appts This Week" (zeros until webhook fires)
- [ ] Appointments tab loads with empty state
- [ ] Locations tab lists seeded location, tap to filter dashboard
- [ ] Settings shows user info — Sign Out returns to Login
- [ ] Fire a test webhook → appointment appears in app after pull-to-refresh

---

## Deployment

### 12. Deploy API to Railway
- [ ] Create Railway project → Add service (from GitHub repo) + PostgreSQL addon
- [ ] Set environment variables:
  ```
  DATABASE_URL=<Railway provides this>
  Jwt__Secret=<long random string>
  ASPNETCORE_ENVIRONMENT=Production
  GoogleCalendar__ServiceAccountJson=<service account JSON>
  GoogleCalendar__CalendarId=<calendar ID>
  ```
- [ ] Run migrations on Railway shell:
  ```bash
  dotnet ef database update --project RadRoofer.Infrastructure --startup-project RadRoofer.Api
  ```
- [ ] Configure Vapi assistant:
  - Webhook URL: `https://<railway-url>/v1/webhooks/vapi/{locationId}`
  - Header: `X-Vapi-Secret: <seeded location's VapiSecret value>`

### 13. Deploy Mobile to TestFlight
```bash
cd mobile
eas login
eas build:configure        # one-time — links to App Store Connect
eas build --profile preview --platform ios   # ~10 min, builds in cloud
eas submit --platform ios --latest           # uploads to TestFlight
```
- Open App Store Connect → TestFlight → add yourself as internal tester
- Install via TestFlight app on iPhone

---

## End-to-End Validation

### 14. Full Flow Test
- [ ] Call the Vapi phone number from a real phone
- [ ] AI collects name, phone, address, reason for call
- [ ] Webhook fires → `Appointment` row in DB, event in Google Calendar
- [ ] Log into mobile app → appointment appears in the Appointments tab
- [ ] Stats increment on Dashboard
- [ ] Fire same webhook again → no duplicate (idempotency)

---

## Done — MVP Complete

Ship to one real roofer. Collect feedback before building Phase 2.

**Phase 2 candidates** (after real-world validation):
- Add `CallerName` / `CallerPhone` columns to `Appointment` (migration)
- Per-location Google Calendar ID (add `CalendarId` to `ServiceLocation`)
- GoHighLevel CRM sync
- Lead status management in mobile app
- Push notifications via Expo

---

# App Store (when ready for public launch)

```bash
eas build --profile production --platform ios
eas submit --platform ios --latest
```
Submit for App Store review in App Store Connect. No code changes needed.
