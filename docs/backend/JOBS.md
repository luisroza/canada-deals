# Background jobs

The worker host uses Hangfire with PostgreSQL storage. The API and worker share the approved modular-monolith database boundary; the worker runtime is separately deployable/scalable later.

Current implementation:

- Hangfire server starts with one worker.
- A fixture-safe sample job can be enqueued only when `Worker:EnqueueSampleJob=true`.
- The sample job performs no merchant fetch and creates no recurring load.
- No Hangfire dashboard is exposed anonymously.

Future ingestion, freshness, matching, alert, and reconciliation jobs must be idempotent, rate-aware, policy-controlled, observable, and added only when their slice is approved.
