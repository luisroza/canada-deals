# Project Status

## Current phase

Application Implementation — Vertical Slice 7 — IMPLEMENTED AND VALIDATED

## Completed

- Local workspace linked to `luisroza/canada-deals`
- Project and agent governance foundation
- Canadian market research and product definition
- Human Product Checkpoint
- UX / Product Design and Human UX Checkpoint
- Solution / Cloud / FinOps Architecture
- Data / Affiliate Integration Architecture
- Architecture / Data reconciliation
- Human Architecture / Data Integration Checkpoint approved with refinements
- Application monorepo foundation
- PostgreSQL EF Core model and first migration
- Synthetic fixture seed data
- Connector-neutral REST API
- Next.js discovery feed and Product Page
- Domain and frontend automated tests
- Hangfire worker foundation
- Vertical Slice 1 validation and stabilization
- Vertical Slice 2 persisted stale/wrong listing report workflow
- Vertical Slice 3 Save Product persistence with minimal account boundary
- Vertical Slice 4 Target-Price Alert persistence and evaluation boundary
- Vertical Slice 5 Search + Filters
- Vertical Slice 6 Product History Evidence View
- Vertical Slice 7 Production Email Delivery Boundary

## Approved product direction

- Positioning: Canadian price-truth layer for planned online purchases
- Initial wedge: electronics plus home improvement/tools
- English-first responsive web MVP
- Evidence before enthusiasm, visible freshness, conservative history, and safe same-product comparison
- Save Product and Target Price Alert remain P1; Weekly Digest remains P2
- Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI

## Current implementation

- Backend: .NET 10 modular monolith projects under `src/backend/` for Domain, Infrastructure, API, and Worker.
- Persistence: PostgreSQL/Npgsql through `20260812135446_AddEmailRetrySchedule`; local development uses Docker Compose.
- API: public discovery/product/history/health/report/handoff, PostgreSQL FTS + `pg_trgm` typo fallback, deterministic policy-safe P0 filtering/sorting/pagination, Identity accounts, current-user Saved Products, and idempotent current-user Target Price Alert list/create/update/disable.
- Fixtures: Products A-H cover reliable/partial/unavailable/policy-hidden Product history, stale current price with valid history, unsafe cheaper history, discovery states, and alerts without live retailer sources.
- Frontend: Next.js 16 + React 19 public URL-driven discovery search/filters and `/products/[slug]`, summary-first accessible 30/90-day Product history, account routes, Product Page Save and Target Price controls, and `/saved` alert management; public Product content remains server-rendered and SEO-visible.
- Reports: controlled reasons, optional bounded plain-text note, `OPEN` default status, non-cascading listing FK, no required PII, and no automatic mutation/suppression of listing truth.
- Identity: normalized email identifier, Identity hashing/token support, confirmed-email policy, explicit 24-hour email-confirmation token provider, base64url confirmation links from a configured canonical origin, secure same-site cookie session, anti-forgery on mutations, generic resend/login behavior, lockout, and register/login/resend rate limit.
- Saved Products: composite `(UserId, ProductId)` persistence, current-session ownership, canonical Product identity, user cascade/Product restrict delete behavior, and no influence on Price Truth, Deal Quality, evidence, freshness, affiliate economics, or organic ranking.
- Target Price Alerts: one canonical Product configuration per user, confirmed-email ACTIVE gate, explicit alert-only consent, CAD target/version lifecycle, fresh/policy-permitted/safely-matched evaluation, and continuous-condition deduplication.
- Delivery: provider-neutral transactional email with a production Resend HTTP adapter; durable account-confirmation and alert deliveries; exact Development/Test HTML/text capture; delivery-derived idempotency keys; bounded persisted retries; provider acceptance versus webhook-confirmed delivery; signed replay-safe lifecycle webhooks; and bounce/complaint/provider suppression.
- Product history: 30/90-day bounded server projection, daily lowest qualifying safe price, explicit `RELIABLE`/`PARTIAL`/`UNAVAILABLE`, truthful tracking/coverage, no interpolation, and current freshness kept independent.
- Tests: 64 domain tests, 87 PostgreSQL API integration tests, and 43 frontend tests pass with zero skips against PostgreSQL 17.
- Browser validation: 24 Playwright tests pass against real Next.js/API/PostgreSQL/Worker, including all prior regressions plus captured-email registration/confirmation/resend, alert email content, and provider-free deterministic delivery.
- Worker: Hangfire PostgreSQL storage, health endpoint, and retry-safe Price Alert evaluation; no merchant ingestion.

## Blocked external integrations

- Best Buy Canada: program exists, but feed/API rights, permitted fields, retention, and cadence are unresolved.
- Home Depot Canada: affiliate program exists, but catalog/API/feed rights are unresolved.
- Amazon.ca: gated; not on the critical implementation path.
- Walmart Canada: fallback / Phase 2 pending Rakuten partnership and data access validation.

No merchant-specific production connector may be added until the verified source evidence required by `docs/integrations/INTEGRATION-BACKLOG.md` is committed.

## Current checkpoint

Human Architecture / Data Integration Checkpoint: approved. Application implementation is authorized within the approved architecture and connector gate.

## Validation evidence

- Clean PostgreSQL 17 Compose instance started successfully.
- EF migration `20260811180731_InitialCreate` applied successfully; a second application reported the database already up to date.
- `pg_trgm` extension and relational schema were created successfully.
- Development API seeded six controlled synthetic products and their listings without live retailer data.
- Release backend restore/build/tests passed with 0 warnings and 0 errors.
- Worker startup created 12 Hangfire PostgreSQL tables and completed the opt-in fixture-safe sample job.
- Same-site Next.js rewrites for `/api/*` and `/go/*` were exercised by the Playwright path.
- A separate empty PostgreSQL 17 database successfully applied InitialCreate → AddListingIssueReports → AddIdentityAndSavedProducts → AddPriceAlertsAndNotificationDeliveries; reapplication reported current before all 39 integration tests passed.
- The report table, review index, `OPEN` lifecycle, anonymous persistence, retry-tolerant duplicate behavior, and Development-only operator review path were validated.
- Identity confirmation boundaries, production Secure/HttpOnly/SameSite cookie flags, CSRF rejection, rate limiting, normalized duplicates, generic login errors, logout, IDOR isolation, database uniqueness, and user/Product delete semantics were validated.
- Alert equality/above/stale/unsafe/policy/no-price/history-unavailable cases, target version re-trigger, CSRF, confirmed-email gate, IDOR, durable deduplication, Development capture, and Production suppression were validated.
- Release backend build passed with 0 warnings and 0 errors. Release frontend build and 13 real Playwright journeys passed without core API interception.
- Concurrent clean-database startup exposed and fixed a fixture-seed race; a PostgreSQL transaction advisory lock now serializes Development seed initialization.
- Worker regression created 12 Hangfire tables and the fixture-safe job reached `Succeeded`; no Save Product background job was added.
- A separate empty PostgreSQL 17 database applied all five migrations including `AddPostgresProductSearch`; a second update was current, `pg_trgm` and the generated FTS/trigram/identifier indexes were present, and all 69 integration tests passed without skips.
- `EXPLAIN (ANALYZE, BUFFERS)` verified bitmap index scans through the Product FTS GIN index and the `pg_trgm` word-similarity GIN index on the controlled fixture database.
- Release frontend build and all 18 real Playwright journeys passed against the isolated PostgreSQL validation database.
- A separate empty PostgreSQL 17 database applied the unchanged five-migration chain for Slice 6; a second startup was current and all 80 PostgreSQL integrations passed without skips.
- Slice 6 `EXPLAIN (ANALYZE, BUFFERS)` used `IX_RetailerListings_ProductId` and `IX_PriceObservations_RetailerListingId_ObservedAt_SourceHash` for the bounded Product/listing/time query, so no migration or speculative index was added.
- Release backend build passed with 0 warnings and 0 errors; 58 domain, 80 integration, 38 frontend, and 23 real full-stack Playwright tests passed with zero skips.
- A separate empty PostgreSQL 17 database applied the complete seven-migration chain through `AddProductionEmailDelivery` and `AddEmailRetrySchedule`; a second update reported current, all 87 integration tests passed, and the exact temporary database was removed.
- Release backend build passed with 0 warnings and 0 errors; 64 domain, 87 PostgreSQL integration, 43 frontend, and 24 real full-stack Playwright tests passed with zero skips.
- The E2E account journey read the exact persisted confirmation email, followed its real Identity token, confirmed, signed in, created an alert, ran Hangfire, and asserted the exact captured HTML/text alert content without API interception or external email.
- Signed webhook tests covered invalid signatures, provider-event replay, out-of-order delivery, provider acceptance versus delivery, and application suppression. Resend HTTP tests covered stable idempotency keys, provider IDs, and `429 Retry-After`.

## Production-readiness limitations

- `PRODUCTION EMAIL PROVIDER IMPLEMENTED — OPERATIONAL VALIDATION BLOCKED`: no production API key, verified sender domain/address, webhook signing secret, or canonical production origin was available, so DNS/provider acceptance and controlled real-mail smoke tests remain a release gate.
- Password recovery and MFA are not implemented.
- No merchant-specific production connector or live affiliate relationship is configured.

## Next vertical slice

Recommended next slice: Vertical Slice 8 — Production Deployment + Email Operational Validation. Provision the approved Canadian runtime/database boundary, configure the verified Resend sender and signed webhook secrets, run controlled real confirmation/alert lifecycle smoke tests, and capture operational evidence without adding password recovery, MFA, merchant connectors, or new Product functionality.
