# Vertical Slice 4 Test Report

**Slice:** Target-Price Alert Persistence + Evaluation Boundary
**Status:** IMPLEMENTED AND VALIDATED
**Validation date:** 2026-08-11

## Environment

- Windows 11; .NET SDK 10.0.300; Release build.
- PostgreSQL 17 in the repository Docker Compose service.
- Node.js 24.14.0; repository `pnpm@10.15.0`; Chromium Playwright.
- Real ASP.NET Core API, Next.js, PostgreSQL, Hangfire PostgreSQL storage, and separately hosted Worker.
- Synthetic fixture data only; no retailer connector, scraping, affiliate credential, or external email send.

## Migration and database

A separate empty `canadadeals_slice4_validation` database applied, in order:

1. `20260811180731_InitialCreate`
2. `20260811185543_AddListingIssueReports`
3. `20260811192055_AddIdentityAndSavedProducts`
4. `20260811202709_AddPriceAlertsAndNotificationDeliveries`

A second `database update` reported no pending migrations. `pg_trgm`, `PriceAlerts`, `NotificationDeliveries`, and 12 Hangfire schema tables were verified. The alert uniqueness/range/version constraints and delivery deduplication index are exercised through real Npgsql/EF Core integration tests.

## Automated results

| Suite | Result |
|---|---:|
| Domain | 38 passed, 0 failed, 0 skipped |
| PostgreSQL API integration | 39 passed, 0 failed, 0 skipped |
| Frontend component/library | 29 passed, 0 failed, 0 skipped |
| Playwright full stack | 13 passed, 0 failed, 0 skipped |
| Release backend build | passed, 0 warnings, 0 errors |
| Next.js production build | passed |

## Coverage evidence

- Domain: valid/invalid target, ownership relationship, consent/version lifecycle, target update/reactivation, equality, above target, stale/future-dated, policy denied, unsafe match, unavailable offer, history independence, commission/save neutrality, continuous-condition reset/deduplication.
- API/persistence: anonymous rejection, CSRF, confirmed-email gate, idempotent PUT, unique user/Product configuration, list/update/disable, invalid Product/target/consent, auto-Save, current-principal ownership, cross-user isolation.
- Evaluation: real current observations; equality eligible; above/stale/unsafe/policy/no-current-price skipped; history-unavailable eligible; target version may re-trigger; repeated condition creates one delivery.
- Worker: real Hangfire server started with PostgreSQL storage and health endpoint; queued evaluation executed; advisory locks and database uniqueness make retries safe; job performed no retailer fetch.
- Delivery: Development/Test intent reached `DevelopmentCaptured` with `CONTROLLED_DEVELOPMENT_TEST_SINK`; Production integration reached `Suppressed` with `PRODUCTION_EMAIL_PROVIDER_NOT_CONFIGURED`, never fake `Sent`.
- Frontend/accessibility: signed-out return context, unconfirmed state, labeled CAD entry, validation, explicit alert-only consent, pending/error/success, active/edit/remove, `/saved`, keyboard focus, mobile regression.
- Playwright: target below current -> controlled fresh price crossing -> worker evaluation -> exactly one captured notification after duplicate evaluation; cheaper unsafe related variant produced no delivery.

## Security and abuse controls

- User ID comes only from the authenticated cookie principal; IDOR/BOLA cross-user read/delete is tested.
- Every cookie-authenticated alert mutation requires the established anti-forgery token.
- ACTIVE requires confirmed email; one configuration per user/Product; mutations are rate-limited; repeated evaluation is durably deduplicated.
- Logs use opaque user/alert/Product IDs and outcomes/statuses, not CSRF/auth/confirmation tokens, cookies, provider keys, or message bodies.
- Broader provider send retry limits and email-bombing monitoring remain part of the future provider/security review.

## Regression and defects

All Slice 1-3 discovery, price truth, evidence/freshness/history, safe comparison/handoff, reporting, Identity, CSRF, session isolation, Save, `/saved`, and open-redirect journeys remain green.

Meaningful defects found and fixed:

- Account session endpoint used sync-over-async while adding confirmation state; changed to an async action and covered by integration/UI flows.
- Alert mutation rate limiting initially ran before authentication, preventing user partitioning; middleware order was corrected so the server principal is available.
- Worker had no independent readiness signal for real E2E orchestration; the Worker host now exposes `/health`, and Playwright starts/observes the real process.
- Existing Save E2E selector became ambiguous after a second account boundary appeared; selector was scoped to the Save region.
- A five-second polling window was shorter than a legitimate pre-existing Hangfire backlog; the test now uses a bounded 30-second worker window and still verifies exact one-delivery state/reason.
- A reusable Development fixture price can be changed by the controlled E2E crossing scenario; an integration assertion incorrectly hard-coded the seed amount. It now asserts the qualifying threshold invariant, while clean-database tests retain deterministic seed validation.
- The unsafe-variant E2E inferred completion from an alert timestamp and could observe the automatic create job instead of the explicitly requested scenario job. A Development/Test-only authenticated job-state read now lets the test await the exact Hangfire job ID before asserting zero deliveries.

## Production limitations

`PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED`.

Production account-confirmation delivery is unchanged and remains unavailable. Domain authentication/from-address setup, privacy/legal review, provider retry/complaint/bounce handling, password recovery, MFA, production operator tooling, approved merchant integrations, and live affiliate relationships remain release gates. Weekly Digest and all other alert types remain unimplemented.
