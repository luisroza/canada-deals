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
- `AffiliateLinkRefreshJob` uses the same PostgreSQL-backed Hangfire boundary with one concurrent execution and three bounded delayed retries. It finds ACTIVE programs/listings with missing or due links, invokes the provider adapter outside the shopper path, validates both URL trust boundaries, and persists a reusable link or bounded failure state. It never fetches Product/catalog/price data and is disabled by default (`Worker:EnqueueAffiliateLinkRefreshJob=false`).
- Impact `429` honors provider `Retry-After`; CJ rate limits and temporary errors are deferred. Existing valid links remain usable during temporary provider outages. Authoritative inactive relationship/deep-link responses suspend the program and immediately block handoff.
- `RakutenCatalogImportJob` is manual/opt-in, MID-scoped, non-concurrent, and limited to two Hangfire retries. Its service enforces page/page-size ceilings, provider pacing, dry-run versus live-write gates, transactional writes, persisted run counters, idempotent source mapping/observations, and safe failure reasons. `Worker:EnqueueRakutenCatalogImportJob` remains `false` by default.
- `CatalogDiscoveryJob`, `CatalogDryRunJob`, and `CatalogImportJob` provide one provider-neutral Hangfire orchestration boundary. Provider adapters own external contracts; jobs own bounded scheduling only. Discovery never activates a source, dry-run never mutates Product/listing/observation state, import is transactional, and all jobs are non-concurrent with one delayed retry. Global catalog persistence and every provider remain disabled by default.
- Read-only discovery and Product Search dry-run are also available as explicit Worker commands. They output redacted counts/status only and never print credentials, tokens, contact PII, or raw bodies.

Future ingestion, freshness, matching, and reconciliation jobs must be idempotent, rate-aware, policy-controlled, observable, and added only when their slice is approved. This job does not fetch merchants.
