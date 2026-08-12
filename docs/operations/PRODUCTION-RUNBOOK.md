# Production operational runbook

Use this only after the deployment inputs in `docs/operations/DEPLOYMENT.md` are available. Do not put secrets in shell history, source control, screenshots, or this document.

## Deploy and verify

1. Validate the rendered, secret-free App Spec structure and required environment presence.
2. Create or update the Toronto App Platform app using the approved account workflow. Confirm the `migrate` PRE_DEPLOY job succeeds before services receive traffic.
3. Confirm `web`, `api`, and `worker` are healthy and that PostgreSQL is a trusted source using TLS verification.
4. With `Email__EmergencyStop=true`, run read-only HTTP smoke checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy/smoke-production.ps1 -ProductionOrigin https://<production-host>
```

The smoke checks `/`, `/health`, `/api/v1/deals`, absent developer endpoints, and the required security headers. It performs no mutation unless `-AllowEmailMutation` is explicitly supplied.

## Controlled Resend validation

Before sending, verify the Resend domain/subdomain has current SPF and DKIM verification and configure the public HTTPS webhook at `/api/v1/webhooks/email/resend` for `email.sent`, `email.delivered`, `email.failed`, `email.bounced`, `email.complained`, and `email.suppressed`.

Temporarily set `Email__EmergencyStop=false`, deploy the environment change, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy/exercise-resend-events.ps1 -ProductionOrigin https://<production-host> -Confirm
```

The script creates controlled confirmation-mail requests only for Resend’s documented test addresses: delivered, bounced, complained, and suppressed. Inspect provider and application records for accepted provider IDs, signed webhook handling, terminal lifecycle state, normalized suppression, and no duplicate attempt for the same durable idempotency key. Then perform one controlled real confirmation and one controlled real target-price alert using an approved test mailbox. Record redacted evidence and restore the intended emergency-stop setting.

## Rollback and recovery

- If a deployment or migration fails, do not manually alter migration history. Review job/application logs, correct the source/configuration, and redeploy from a known healthy revision.
- If bad email behavior is suspected, immediately set `Email__EmergencyStop=true`. The application records suppression instead of sending through Resend.
- If a provider key or webhook secret is exposed, rotate it at Resend, update the deployment secret store, and deploy the replacement. Do not replay secrets in logs.
- Restore PostgreSQL only through the managed-database backup/PITR workflow. A restore creates a new target; re-establish the trusted source and re-run health/smoke checks before cutover.
- Treat the Data Protection PFX and PostgreSQL key-ring rows as a pair for recovery. Rotating or losing them can invalidate authenticated sessions and pending confirmation tokens.

## Cost guardrails

The prepared minimum footprint is approximately USD $40/month before jobs, tax, domain, and email: $10 web + $10 API + $5 worker + $15 managed PostgreSQL. Resend is $0 while its current free limits fit, or approximately $20/month for Pro. Set DigitalOcean budget alerts at USD $50, $75, and $100 and review them weekly during MVP.
