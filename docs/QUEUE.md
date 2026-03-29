# Communication Dispatch Queue

---

## Purpose

All outgoing communication (SMS, email, outbound calls) is dispatched asynchronously through a queue rather than inline during a webhook handler. This ensures:

- Webhook responses return to Vapi immediately — no timeouts
- Failed deliveries retry automatically with backoff
- Jobs are durable — survive server restarts
- Full visibility into what was sent, when, and whether it succeeded
- Per-tenant, per-CRM rate limits are respected (AccuLynx is 10 req/sec — the binding constraint)

---

## Technology — Hangfire + PostgreSQL

Hangfire runs inside the ASP.NET Core process and uses the existing PostgreSQL database (separate schema). No additional infrastructure needed.

**Key capabilities used:**
- **Enqueue** — fire a job as soon as possible (e.g. send confirmation SMS right after booking)
- **Schedule** — fire a job at a specific time (e.g. estimate follow-up call 48 hours after estimate sent)
- **Recurring** — run on a cron schedule (e.g. check for unanswered callbacks every 10 minutes)
- **Automatic retry** — if Twilio or a CRM API fails, Hangfire retries with exponential backoff
- **Dashboard** — web UI at `/hangfire` for monitoring and manual retriggers

---

## Job Types

### Immediate Jobs
Enqueued by the webhook handler after a call ends.

| Job | Trigger | Action |
|---|---|---|
| SendBookingConfirmation | Appointment booked | SMS + email to customer |
| CreateCrmLead | New inbound call | Write lead to CRM via adapter |
| NotifyOnCall | After-hours emergency keyword | SMS to on-call staff |
| EnqueueMissedCallCallback | Inbound call unanswered | Schedule callback job |

### Scheduled Jobs
Enqueued with a delay based on business logic.

| Job | Delay | Action |
|---|---|---|
| EstimateFollowUpCall | 48h after estimate sent | Vapi warm outbound call to lead |
| ReviewCollectionCall | 7 days after job closed | Vapi warm outbound call to customer |
| MissedCallCallback | 5 min after missed call | Vapi calls the number back |

### Recurring Jobs
Run on a timer regardless of call activity.

| Job | Schedule | Action |
|---|---|---|
| ProcessStormResponseQueue | Every 10 min | Drains storm lead queue as estimators free up |
| SyncCrmJobStatuses | Every 30 min | Pulls updated job statuses from CRM for status lookup feature |
| RefreshExpiredOAuthTokens | Every hour | Proactively refreshes CRM/calendar OAuth tokens before they expire |

---

## Tenant Isolation in Jobs

Every job carries a `tenantId` and `locationId` as parameters. When the job executes, Finbuckle resolves the tenant context from these parameters — the same isolation guarantees apply as on inbound requests.

```csharp
BackgroundJob.Enqueue<IDispatchService>(
    o => o.SendBookingConfirmationAsync(tenantId, locationId, appointmentId)
);
```

---

## CRM Rate Limiting

CRM API calls enqueued as background jobs must respect per-CRM rate limits. The binding constraint is AccuLynx at 10 req/sec per API key.

Each CRM sync job checks a per-tenant rate limiter before executing. If the limit is reached, the job re-queues itself with a short delay rather than failing.

| CRM | Rate Limit |
|---|---|
| GoHighLevel | 100 req / 10 sec per location |
| HubSpot | 190 req / 10 sec |
| AccuLynx | **10 req / sec** (binding constraint) |
| JobNimbus | Undocumented — implement exponential backoff on 429 |

---

## Retry Policy

Hangfire retries failed jobs automatically. Default strategy:

| Attempt | Delay |
|---|---|
| 1st retry | 1 min |
| 2nd retry | 10 min |
| 3rd retry | 1 hour |
| 4th+ | Dead letter queue — flagged in dashboard |

Failed jobs in the dead letter queue are visible in the Hangfire dashboard and can be manually retriggered after the underlying issue is resolved (e.g. CRM API was down).
