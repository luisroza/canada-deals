# Background jobs

The worker host uses Hangfire with PostgreSQL storage. The API and worker share the approved modular-monolith database boundary; the worker runtime is separately deployable/scalable later.

Current implementation:

- Hangfire server starts with one worker.
- Worker exposes `/health` (local validation uses `http://localhost:5100/health`).
- A fixture-safe sample job can be enqueued only when `Worker:EnqueueSampleJob=true`.
- The sample job performs no merchant fetch and creates no recurring load.
- `PriceAlertEvaluationJob` selects ACTIVE alerts, takes a PostgreSQL advisory lock per alert, evaluates already-ingested current observations, commits a durable delivery intent, then processes pending deliveries under a per-delivery lock.
- Eligibility requires a matching CAD price, permitted observation/policy, confirmed/auto match, online availability, and source-policy freshness. History, Deal Quality, commission, and Save popularity are excluded.
- Deduplication uses the database unique key `(PriceAlertId, TargetVersion, PriceObservationId)` plus the alert's continuous-below-target cycle. Re-execution is retry-safe; price-above-target and target changes reset the cycle.
- API alert create/update enqueues evaluation. Controlled fixture paths can enqueue explicitly. No recurring high-frequency schedule exists before real ingestion.
- Development/Test persists exact HTML/text messages and changes delivery to `DevelopmentCaptured`. Production startup fails closed unless the complete Resend configuration is present.
- Alert attempts are committed before the provider call. A transient result schedules a delayed Hangfire retry using the same delivery-derived idempotency key and persisted `NextAttemptAt`; `429` honors `Retry-After`, other transient failures use bounded exponential delay, and maximum attempts are enforced. Permanent failures do not retry.
- Provider API success records `ProviderAccepted` plus provider message ID. Only a verified lifecycle webhook records `Delivered`; bounce, complaint, provider suppression, transient failure, and permanent failure remain distinct.
- No Hangfire dashboard is exposed anonymously.

Future ingestion, freshness, matching, and reconciliation jobs must be idempotent, rate-aware, policy-controlled, observable, and added only when their slice is approved. This job does not fetch merchants.
