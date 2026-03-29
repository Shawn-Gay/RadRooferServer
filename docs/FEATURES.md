# Features

---

## Phase 0 — Proof of Core Loop

Prove the AI receptionist captures leads end-to-end before building integrations. See [PHASE0.md](PHASE0.md) for the full spec.

### Lead Capture (core)
- AI collects caller name, phone, address, and reason for call
- Webhook persists lead directly to database (no CRM sync yet)
- Tagged with source: `inbound-call` and location

### Vapi Webhook
- `POST /v1/webhooks/vapi/{locationId}` with `X-Vapi-Secret` verification
- Parses `end-of-call-report` payload
- Creates CallLog + Lead, idempotent on VapiCallId

### Auth & Dashboard
- Email + password login → JWT (API) / Cookie (Razor)
- Minimal Razor page: stats (calls today/week, leads today/week) + table of recent calls and leads

### Foundation (ships but not user-facing)
- Full database schema for all phases (11 entities, all indexes, all constraints)
- EF Core global query filters (TenantId + soft delete)
- Finbuckle multi-tenancy (JWT claim + locationId webhook resolution)

---

## Phase 1 — Integrations & Mobile

Build on Phase 0's foundation. Add CRM sync, calendar booking, SMS, and the mobile app.

### Mobile App Dashboard
- Login with email + password
- Simple analytics: calls today/week, leads captured, appointments booked
- Per-location AI assistant on/off toggle
- Location switcher for multi-location tenants

### GoHighLevel CRM Sync
- OAuth 2.0 connection flow
- `ICrmAdapter` interface extracted (Phase 0 was direct DB only)
- Lead creation in GHL on every inbound call
- Contact + opportunity sync with pipeline stage mapping

### Google Calendar Booking
- OAuth 2.0 connection flow
- `ICalendarAdapter` interface
- AI checks real-time availability during call
- Books inspection/estimate appointments directly
- Falls back to SMS contact details if booking unavailable

### SMS & Email Notifications
- Twilio SMS confirmation to caller after lead capture
- SendGrid email for appointment confirmations
- Dispatch queue (Hangfire) for async processing with retry

### After-Hours Handling
- AI handles all calls outside business hours
- Takes messages and schedules callbacks
- Urgent storm/emergency calls escalated via SMS to on-call staff

### Multi-Location Management
- Location CRUD endpoints
- Assistant on/off toggle per location
- Centralized reporting across all locations per tenant

---

## Phase 2 — Depth

Features that add operational value once Phase 1 is stable.

### Additional CRM Integrations
- **JobNimbus** — API key connection; lead + job sync; no calendar API (falls back to SMS)
- **AccuLynx** — API key connection; lead + job sync; insurance fields native; appointment requires existing job

### Estimate Requests
- Caller describes damage or roofing need
- AI captures details and flags for estimator follow-up
- Auto-assign to estimator based on zip code or region

### Job Status Lookup
- Existing customers call and ask for job status
- AI looks up by phone number or name and reads back current status
- Keeps homeowner informed without involving staff

### Insurance & Claims Intake
- Collect insurance carrier, claim number, adjuster name and contact
- Ask if customer has already filed or needs help starting a claim
- Flag claim jobs vs. retail/cash jobs (different sales process)
- Capture deductible amount to help estimator prep

### Estimate Follow-Up (Warm Outbound)
- Vapi calls leads who received an estimate but haven't signed
- "Just checking in — do you have any questions about your estimate?"
- Improves close rate; only contacts existing leads who opted in

### Review Collection (Warm Outbound)
- After job completion, AI calls the customer to ask about their experience
- If positive, sends a direct Google review link via SMS
- If negative, flags for a manager before it becomes a public review

### Missed Call Callback
- Detects unanswered inbound calls
- Vapi proactively calls the number back
- Only applies to numbers that called in first — not cold outbound

### Storm Response Mode
- Manually triggered by the roofer via the dashboard toggle
- Switches AI to fast intake mode — name, address, damage type, contact info only
- No scheduling during the surge; builds a prioritized callback queue
- Vapi handles unlimited simultaneous calls — no busy signals, no lost leads

### Referral Capture
- Asks every caller "How did you hear about us?"
- Tracks referral sources automatically in the CRM

### Financing Inquiry Handling
- AI identifies callers interested in payment plans
- Captures interest and passes to a financing coordinator

---

## Phase 3 — Advanced

Higher complexity, higher value. For tenants with more mature operations.

### Additional CRM Integrations
- **HubSpot** — OAuth 2.0 connection; uses Deals for jobs; custom properties for roofing fields; Enterprise tier required for custom objects

### Insurance Adjuster Communicator
- AI acts as middleman between the homeowner and their insurance adjuster
- Coordinates adjuster site visit scheduling on behalf of the roofing company
- Sends follow-up reminders to both parties
- Logs all adjuster communication to the job record

### Trip / Route Optimization
- Groups nearby inspection appointments together by postal code or geolocation
- Suggests optimized daily routes for estimators
- Reduces drive time and fuel cost across locations
- Only viable if CRM exposes geo/zip data via API — adapter must confirm support

### Warranty Lookup
- Existing customers call with warranty questions
- AI pulls up job record and reads back warranty details
- Logs the inquiry

### Permit & HOA Status Updates
- Customers call asking about permit approval or HOA status
- AI reads back current status from the job record

### Predictive Maintenance Outreach
- Analyzes roof age and material type from past jobs
- Proactively contacts existing customers approaching maintenance windows
- "Your roof is 10 years old heading into storm season — want a free inspection?"
